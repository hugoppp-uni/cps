using System.Text;
using ConsoleApp1.pgs;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;
using MQTTnet;
using MQTTnet.Client;
using QuickGraph.Algorithms;

namespace ConsoleApp1.clients;

public record Kpi(string Topic, byte[] Payload);

public class CarData
{
    private int Id { get; }
    public PhysicalWorld World { get; }
    public StreetPosition Position { get; set; }
    public IEnumerable<StreetEdge> Path { get; set; }
    public StreetNode Destination { get; set; }

    public ParkingSpot OccupiedSpot { get; set; } = null!;
    public CarStatus Status { get; set; }
    public const int MaxParkTime = 500;
    public int ParkTime { get; set; }
    
    public double DistanceToDestination { get; set; }
    
    // for kpis
    private const int Co2EmissionRate = 131; // grams per km, average according to German Federal Environment Agency (UBA)
    public Dictionary<string, double> Kpis { get; set; } 
    public double SpeedReductionRunningAvg { get; set; }
    public double SpeedReductionSum { get; set; }
    public int SpeedReductionCount { get; set; }
    public double DistanceTravelled { get; set; }
    
    public void ResetKpiMetrics()
    {
        DistanceTravelled = 0;
        SpeedReductionRunningAvg = 0;
        SpeedReductionCount = 0;
        SpeedReductionSum = 0;
    }

    public override string ToString() => $"[CAR\t{Id},\t{Status}\t]";
    public CarData(int id, PhysicalWorld world, ParkingGuidanceSystem parkingGuidanceSystem)
    {
        Id = id;
        World = world;
        Path = Enumerable.Empty<StreetEdge>();
        Pgs = parkingGuidanceSystem;
        
        Kpis = new Dictionary<string, double>();
        
        Kpis.Add("kpi/travelDistanceToDestinationDistanceRatio", 0.0);
        Kpis.Add("kpi/distFromDest", 0.0);
        Kpis.Add("kpi/speedReduction", 0.0);
        Kpis.Add("kpi/parkingEmissions", 0.0);
        
        // init position
        Position = StreetPosition.WithRandomDistance(world.StreetEdges.RandomElement());
        Position.StreetEdge.IncrementCarCount();
    }

    public ParkingGuidanceSystem Pgs { get; set; }
    public ParkingSpot ReservedSpot { get; set; }

    public void Park(ParkingSpot spot)
    {
        OccupiedSpot = spot;
        OccupiedSpot.Occupied = true;
        Position = new StreetPosition(Position.StreetEdge, OccupiedSpot.DistanceFromSource);
        lock (Position.StreetEdge)
        {
            Position.StreetEdge.DecrementCarCount();
        }
        Random rand = new Random();
        ParkTime = rand.Next(0, MaxParkTime + 1);
        World.DecrementUnoccupiedSpotCount();
        World.IncrementParkEvents();
        DistanceTravelled += Position.DistanceFromSource;
        UpdateAllKpis();
    }

    public void UpdateAllKpis()
    {
        // travel distance to destination distance ratio
        double ratio = DistanceTravelled / DistanceToDestination;
        Kpis["kpi/travelDistanceToDestinationDistanceRatio"] = ratio;
        
        // distance from destination
        double distanceFromDestination = Position.StreetEdge.Length - Position.DistanceFromSource;
        var shortestPaths = World.Graph.ShortestPathsDijkstra(
            _ => 1.0,
            Position.StreetEdge.Source);

        if (shortestPaths.Invoke(Destination, out var path))
        {
            Path = path;
            distanceFromDestination += Path.Sum(streetEdge => streetEdge.Length);
        }
        Kpis["kpi/distFromDest"] = distanceFromDestination;
        
        // traffic induced speed reduction
        var speedReduction = SpeedReductionSum / SpeedReductionCount;
        Kpis["kpi/speedReduction"] = speedReduction;
        
        // co2 emissions
        double totalCo2EmissionsParking = (DistanceTravelled / 1000) * Co2EmissionRate;
        Kpis["kpi/parkingEmissions"] = totalCo2EmissionsParking;
    }
    
    private async Task Publish(IMqttClient client, Kpi kpi)
    {
        await client.PublishAsync(new MqttApplicationMessage { Topic = kpi.Topic, Payload = kpi.Payload });
    }
    
    public async Task PublishAll(IMqttClient client)
    {
        foreach (var kvp in Kpis)
        {
            await Publish(client, new Kpi(kvp.Key, Encoding.UTF8.GetBytes(kvp.Value.ToString())));
        }
    }
    
    public bool DestinationReached()
    {
        return !Path.Any() && Destination == Position.StreetEdge.Target &&
               Position.DistanceFromSource >= Position.StreetEdge.Length;
    }
    
    public bool TargetNodeReached()
    {
        return Position.DistanceFromSource >= Position.StreetEdge.Length;
    }
    
    public void RespawnAtRandom()
    {
        lock (Position.StreetEdge)
        {
            var previousPosition = Position;
            Position = StreetPosition.WithRandomDistance(World.StreetEdges.RandomElement());
            lock (Position.StreetEdge)
            {
                previousPosition.StreetEdge.DecrementCarCount();
                Position.StreetEdge.IncrementCarCount();
            }
        }
    }

    public void ResetAfterParking()
    {
        lock (Position.StreetEdge)
        {
            OccupiedSpot.Occupied = false;
            OccupiedSpot.Reserved = false;
            Position.StreetEdge.IncrementCarCount();
        }

        // kpi
        ResetKpiMetrics();

        // diagnostics
        World.IncrementUnoccupiedSpotCount();
    }

    public void Turn(StreetEdge next)
    {
        lock (Position.StreetEdge)
        {
            UpdateTrafficKpis();
            
            var overlap = Position.DistanceFromSource - Position.StreetEdge.Length;
            var previousPosition = Position;
            Position = new StreetPosition(next, overlap);
            lock (Position.StreetEdge)
            {
                previousPosition.StreetEdge.DecrementCarCount();
                Position.StreetEdge.IncrementCarCount();
            }
            Path = Path.Skip(1);
        }
    }

    public bool TryUpdatePath()
    {
        // update path
        var shortestPaths = World.Graph.ShortestPathsDijkstra(
            _ => 1,
            Position.StreetEdge.Source);
    
        if (shortestPaths.Invoke(Destination, out var path))
        {
            Path = path.ToList();
            
            // calculate distance to destination
            DistanceToDestination = Position.StreetEdge.Length - Position.DistanceFromSource;
            DistanceToDestination += Path.Sum(edge => edge.Length);
            return true;
        }
        
        Path = Enumerable.Empty<StreetEdge>();
        return false;
    }

    public void UpdateTrafficKpis()
    {
        double speedReductionP = 100 - ((Position.StreetEdge.CurrentMaxSpeedMs() / Position.StreetEdge.SpeedLimitMs) * 100);
        SpeedReductionSum += speedReductionP;
        SpeedReductionCount++;
        SpeedReductionRunningAvg = SpeedReductionSum / SpeedReductionCount;
    }
}