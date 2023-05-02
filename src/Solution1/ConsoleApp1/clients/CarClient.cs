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

    private double FuelConsumptionRate { get; } = 6.5; // liters per 100 km, average according to German Federal Environment Agency (UBA)
    private int CO2EmissionRate { get; } = 131; // grams per km, average according to German Federal Environment Agency (UBA)
    private double DistanceTravelledParking { get; set; }

    private int LastParkingSpotPassedIndex { get; set; }

    // for calculating running avg time spent parking
    private int TicksSpentParking { get; set; }

    public static int MaxParkTime { get; } = 500;

    private int LastOccupiedIndex { get; set; }
    
    private int ParkTime { get; set; }

    public override string ToString() => $"[CAR\t{Id},\t{Status}\t]";

    private CarClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id) : base(mqttClient)
    {
        // running avg time spent parking
        TicksSpentParking = 0;
        
        Id = id;
        PhysicalWorld = physicalWorld;
        Position = StreetPosition.WithRandomDistance(physicalWorld.StreetEdges.RandomElement());
        Position.StreetEdge.IncrementCarCount();
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
                UpdateDestination();
                break;

            case CarClientStatus.PARKED: // car parked
                StayParked();
                break;

            case CarClientStatus.PARKING: // car looking for parking
                LookForParking();
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

    private async void LookForParking()
    {
        TicksSpentParking++;
        KeepDriving();
        if (Position.DistanceFromSource >= Position.StreetEdge.Length)
        {
            NextStreetToLookForParking();
        }
        else // try parking locally
        {
            (bool parkingFound, int newLastPassedOrFound) = Position.StreetEdge.TryParkingLocally(Position.DistanceFromSource, LastParkingSpotPassedIndex);
            LastParkingSpotPassedIndex = newLastPassedOrFound;
            if (parkingFound) // available spot found
            {
                LastOccupiedIndex = LastParkingSpotPassedIndex;
                Position = new StreetPosition(Position.StreetEdge, Position.StreetEdge.ParkingSpots[LastOccupiedIndex].DistanceFromSource);
                Status = CarClientStatus.PARKED;
                Random rand = new Random();
                ParkTime = rand.Next(0, MaxParkTime + 1);
                DistanceTravelledParking += Position.StreetEdge.ParkingSpots[LastOccupiedIndex].DistanceFromSource;
                
                await PublishDistanceTravelledParking();
                await PublishFuelConsumptionParking();
                await PublishCO2EmissionsParking();
                await PublishTimeSpentParking();
                await PublishDistanceFromDestination();
            }
        }
    }

    private void StayParked()
    {
        Console.WriteLine($"{this}\ttick | Parked at {Position} | {ParkTime} ticks remaining");
        if (ParkTime == 0)
        {
            DistanceTravelledParking = 0;
            Position.StreetEdge.FreeParkingSpot(LastOccupiedIndex);
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
        double currentSpeed = Position.StreetEdge.CurrentMaxSpeed();
        if (Position.DistanceFromSource < Position.StreetEdge.Length) // driving on street
        {
            // 1 tick = 1 second 
            Position = new StreetPosition(Position.StreetEdge, Position.DistanceFromSource + MathUtil.KmhToMs(currentSpeed));
            Console.WriteLine($"{this}\ttick | {Position.ToString()} | dest: {Destination.Id} | car count: {Position.StreetEdge.CarCount} | driving at {currentSpeed:F2}kmh/{Position.StreetEdge.SpeedLimit:F2}kmh");
        }
        else // node reached
        {
            var overlap = Position.DistanceFromSource - Position.StreetEdge.Length;
            Position.StreetEdge.DecrementCarCount();
            Position = new StreetPosition(Path.First(), overlap);
            Position.StreetEdge.IncrementCarCount();
            Path = Path.Skip(1);
            Console.WriteLine($"{this}\ttick | {Position.ToString()} | dest: {Destination.Id} | car count: {Position.StreetEdge.CarCount} | driving at {currentSpeed:F2}kmh/{Position.StreetEdge.SpeedLimit:F2}kmh");
            
            // TODO dont know if this is a good place to publish these 2 kpis, performance decreases
            double speedReduction = 100 - ((currentSpeed / Position.StreetEdge.SpeedLimit) * 100);
            await PublishSpeedReduction(speedReduction); 
            await PublishCarCount(Position.StreetEdge.CarCount);
        } 
    }

    private double CalculateDistanceFromDestination()
    {
        double distance = Position.StreetEdge.Length - Position.DistanceFromSource;
        var shortestPaths = PhysicalWorld.Graph.ShortestPathsDijkstra(
            edge => 100 - edge.SpeedLimit,
            Position.StreetEdge.Source);

        if (shortestPaths.Invoke(Destination, out var path))
        {
            Path = path;
            distance += Path.Sum(streetEdge => streetEdge.Length);
        } 
    
        return distance;
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
        DistanceTravelledParking += Position.StreetEdge.Length;
        StreetEdge nextStreet;
        if (DestinationReached())
        {
            // look for parking at random outgoing street from destination
            var outGoingStreets = GetOutGoingStreets(Destination);
            nextStreet = outGoingStreets.ToList().RandomElement();
        }
        else
        {
            // TODO currently looking for parking while driving around randomly, this can be replaced with heuristically searching for parking 
            var outGoingStreets = GetOutGoingStreets(Position.StreetEdge.Target);
            nextStreet = outGoingStreets.ToList().RandomElement();
        }
        Path = new List<StreetEdge>{nextStreet} ;
        LastParkingSpotPassedIndex = 0; 
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
    
    private async Task PublishCarCount(int carCount)
    {
        var payload = Encoding.UTF8.GetBytes(carCount.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/carCount", Payload = payload });
    }
    
    private async Task PublishSpeedReduction(double speedReduction)
    {
        var payload = Encoding.UTF8.GetBytes(speedReduction.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/speedReduction", Payload = payload });
    }

    private async Task PublishDistanceFromDestination() 
    {
        double distanceFromDestination = CalculateDistanceFromDestination();
        var payload = Encoding.UTF8.GetBytes(distanceFromDestination.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/distFromDest", Payload = payload });
    }
    
    private async Task PublishDistanceTravelledParking()
    {
        var payload = Encoding.UTF8.GetBytes(DistanceTravelledParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/distTravelledParking", Payload = payload });
    }
    
    private async Task PublishFuelConsumptionParking()
    {
        double totalFuelConsumptionParking = (DistanceTravelledParking / 1000) * (FuelConsumptionRate / 100);
        var payload = Encoding.UTF8.GetBytes(totalFuelConsumptionParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/fuelConsumption", Payload = payload });
    }
    
    private async Task PublishCO2EmissionsParking()
    {
        int totalCO2EmissionsParking = (int)((DistanceTravelledParking / 1000) * CO2EmissionRate);
        var payload = Encoding.UTF8.GetBytes(totalCO2EmissionsParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/parkingEmissions", Payload = payload });
    }
    

    private async Task PublishTimeSpentParking()
    {
        var payload = Encoding.UTF8.GetBytes(TicksSpentParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/timeSpentParking", Payload = payload });
    }

    public static async Task<CarClient> Create(MqttClientFactory clientFactory, int id,
        PhysicalWorld physicalWorld)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tickgen/tick"));
        return new CarClient(client, physicalWorld, id);
    }
}