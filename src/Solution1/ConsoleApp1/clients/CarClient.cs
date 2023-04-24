using System.Drawing;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using QuickGraph;
using QuickGraph.Algorithms;

namespace ConsoleApp1;

struct StreetPosition
{
    public StreetPosition(StreetEdge streetEdge, double distanceFromSource)
    {
        StreetEdge = streetEdge;
        DistanceFromSource = distanceFromSource;
    }

    public static StreetPosition WithRandomDistance(StreetEdge streetEdge)
    {
        return new StreetPosition(streetEdge, Random.Shared.NextDouble() * streetEdge.Length);
    }

    public override string ToString() => $"{StreetEdge.StreetName} ({DistanceFromSource:F1}m / {StreetEdge.Length:F1}m) | [src: {StreetEdge.Source.Id}, tgt: {StreetEdge.Target.Id}])";

    public StreetEdge StreetEdge { get; }
    public double DistanceFromSource { get; set; }
}

enum CarClientStatus
{
    DRIVING,
    PARKING,
    PARKED,
    PATHING_FAILED
}

public class CarClient : BaseClient
{
    private int Id { get; }
    private PhysicalWorld PhysicalWorld { get; }
    private StreetPosition Position { get; set; }
    private IEnumerable<StreetEdge> Path { get; set; }
    private StreetNode Destination { get; set; }

    private CarClientStatus Status { get; set; }
    
    
    private ParkingSpot LastParkingSpotPassed { get; set; }

    // for calculating running avg time spent parking
    private int TicksSpentParking { get; set; }
    private int TimesParked { get; set; }
    private int TicksSpentParkingSum { get; set; }
    private int TicksSpentParkingRunningAvg { get; set; }

    public static int MaxParkTime { get; } = 100;
    
    private int ParkTime { get; set; }

    public override string ToString() => $"[CAR\t{Id},\t{Status}\t]";

    private CarClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id) : base(mqttClient)
    {
        // running avg time spent parking
        TicksSpentParkingSum = 0;
        TicksSpentParking = 0;
        TimesParked = 0;
        TicksSpentParkingRunningAvg = 0;
        
        Id = id;
        PhysicalWorld = physicalWorld;
        Position = StreetPosition.WithRandomDistance(physicalWorld.StreetEdges.RandomElement());
        Position.StreetEdge.CarCount++;
        // Console.WriteLine($"{this}\tInitial position: {Position.ToString()}");
        
        UpdateDestination();
    }
    
    /**
     * Main CarClient behaviour
     */
    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        switch (Status)
        {
            case CarClientStatus.PATHING_FAILED: // pathing failed, new dest needed
                Status = CarClientStatus.DRIVING;
                UpdateDestination();
                break;

            case CarClientStatus.PARKED: // car parked
                // TODO parked behaviour
                StayParked();
                break;

            case CarClientStatus.PARKING: // car looking for parking
                TicksSpentParking++;
                KeepDriving();
                if (Position.DistanceFromSource >= Position.StreetEdge.Length)
                {
                    NextStreetToLookForParking();
                }
                else
                {
                    TryParkingLocally();
                }
                break;

            case CarClientStatus.DRIVING:
                if (DestinationReached()) // car reached destination after driving
                {
                    InitLookingForParking();
                }
                else // car driving to destination
                {
                    KeepDriving();
                }
                break;
        }
    }

    private void StayParked()
    {
        Console.WriteLine($"{this}\ttick | Parked at {Position} | {ParkTime} ticks remaining");
        if (ParkTime == 0)
        {
            UpdateDestination();
        }
        else
        {
            ParkTime--;
        }
    }

    private void InitLookingForParking()
    {
        TicksSpentParking = 0;
        Status = CarClientStatus.PARKING;
        NextStreetToLookForParking();
    }


    private void UpdateDestination()
    {
        Status = CarClientStatus.DRIVING;
        Destination = PhysicalWorld.StreetNodes.RandomElement();
        // Console.WriteLine($"{this}\tupdated destination {Destination.Id}");
        UpdatePath();
    }
    
    private bool DestinationReached()
    {
        return !Path.Any() && Destination == Position.StreetEdge.Target && Position.DistanceFromSource >= Position.StreetEdge.Length;
    }

    private async void KeepDriving()
    {
        if (Position.DistanceFromSource < Position.StreetEdge.Length) // driving on street
        {
            // 1 tick = 1 second 
            Position = new StreetPosition(Position.StreetEdge, Position.DistanceFromSource + MathUtil.KmhToMs(Position.StreetEdge.CurrentMaxSpeed()));
            Console.WriteLine($"{this}\ttick | {Position.ToString()} | dest: {Destination.Id} | car count: {Position.StreetEdge.CarCount} | driving at {Position.StreetEdge.CurrentMaxSpeed():F2}kmh/{Position.StreetEdge.SpeedLimit:F2}kmh");
        }
        else // node reached
        {
            var overlap = Position.DistanceFromSource - Position.StreetEdge.Length;
            Position.StreetEdge.CarCount--;
            Position = new StreetPosition(Path.First(), overlap);
            Position.StreetEdge.CarCount++;
            Path = Path.Skip(1);
            Console.WriteLine($"{this}\ttick | {Position.ToString()} | dest: {Destination.Id} | car count: {Position.StreetEdge.CarCount} | driving at {Position.StreetEdge.CurrentMaxSpeed():F2}kmh/{Position.StreetEdge.SpeedLimit:F2}kmh");
        } 
        //await PublishPosition(); 
    }

    private async void TryParkingLocally()
    {
        if (Position.StreetEdge.ParkingSpots.Count == 0) return;
        int smallestIndexUncheckedSpot = LastParkingSpotPassed.Index;
        int lastPassedIndex = CalculateLastPassedIndex(smallestIndexUncheckedSpot);
        LastParkingSpotPassed = Position.StreetEdge.ParkingSpots[lastPassedIndex];

        var checkForOccupancy = Position.StreetEdge.ParkingSpots
            .Skip(smallestIndexUncheckedSpot)
            .Take(lastPassedIndex - smallestIndexUncheckedSpot + 1);

        ParkingSpot availableSpot = checkForOccupancy.LastOrDefault(ps => !ps.Occupied);
        if (availableSpot != null)
        {
            // Console.WriteLine($"{this}\tAvailable spot at {availableSpot.DistanceFromSource} on {Position.StreetEdge.StreetName}");
            Park(availableSpot);
        }
    }

    private async void Park(ParkingSpot parkingSpot)
    {
        Position = new StreetPosition(Position.StreetEdge, parkingSpot.DistanceFromSource);
        parkingSpot.Occupied = true;
        Position.StreetEdge.CarCount--;
        Status = CarClientStatus.PARKED;
        Random rand = new Random();
        ParkTime = rand.Next(0, MaxParkTime + 1);

        UpdateParkingTimeStats();
        await PublishAverageTimeSpentParking();
    }

    private int CalculateLastPassedIndex(int smallestIndexUncheckedSpot)
    {
        int lastPassedIndexFromDistance = (int)Math.Floor(Position.DistanceFromSource /
                                                          (LastParkingSpotPassed.Length + Position.StreetEdge.ParkingSpotSpacing));
        return Math.Min(lastPassedIndexFromDistance, Position.StreetEdge.ParkingSpots.Count - 1);
    }

    private void UpdateParkingTimeStats()
    {
        TicksSpentParkingSum += TicksSpentParking;
        TimesParked++;
        TicksSpentParkingRunningAvg = TicksSpentParkingSum / TimesParked;
    }


    private IEnumerable<StreetEdge> GetOutGoingStreets(StreetNode node)
    {
        IEnumerable<StreetEdge> outEdges;
        if (PhysicalWorld.Graph.TryGetOutEdges(Position.StreetEdge.Target, out outEdges))
        {
            return outEdges;
        }
        return null!;
    }

    public void NextStreetToLookForParking()
    {
        StreetEdge nextStreet;
        if (DestinationReached())
        {
            // look for parking at random outgoing street from destination
            var outGoingStreets = GetOutGoingStreets(Destination);
            nextStreet = outGoingStreets.ToList().RandomElement();
        }
        else
        {
            // TODO currently looking for parking while driving around randomly, this can be replaced with searching for parking algorithmically
            var outGoingStreets = GetOutGoingStreets(Position.StreetEdge.Target);
            nextStreet = outGoingStreets.ToList().RandomElement();
        }
        Path = new List<StreetEdge>{nextStreet} ;
        LastParkingSpotPassed = nextStreet.ParkingSpots[0];
        // Console.WriteLine($"{this}\tLooking for parking on {nextStreet.StreetName}");
    }

    private void UpdatePath()
    {
        var shortestPaths = PhysicalWorld.Graph.ShortestPathsDijkstra(
            edge => 100 - edge.SpeedLimit,
            Position.StreetEdge.Source);
    
        if (shortestPaths.Invoke(Destination, out var path))
        {
            Path = path;
            Console.WriteLine($"{this}\tpath: {string.Join(',', path.Select(p => p.StreetName))}");
        }
        else
        {
            Console.WriteLine($"{this}\tno path found [destination: {Destination.Id}, position: {Position}, node: {Position.StreetEdge.Source.Id}]");
            SetPathingFailedStatus();
        }
    }

    private void SetPathingFailedStatus()
    {
        Path = Enumerable.Empty<StreetEdge>();
        Status = CarClientStatus.PATHING_FAILED;
    }
    
    // TODO publish data 
    
    private async Task PublishPosition()
    {
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { CarId = Id, Position }));
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "position", Payload = payload });
    }

    private async Task PublishAverageTimeSpentParking()
    {
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { CarId = Id, TicksSpentParkingRunningAvg }));
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "avgTimeSpentParking", Payload = payload });
    }

    public static async Task<CarClient> Create(MqttClientFactory clientFactory, int id,
        PhysicalWorld physicalWorld)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tick"));
        return new CarClient(client, physicalWorld, id);
    }
}