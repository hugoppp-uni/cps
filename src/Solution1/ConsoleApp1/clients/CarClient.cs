using System.Data;
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

    private int TicksSpentParking { get; set; } = 0;

    public static int MaxParkTime { get; } = 500;

    private int LastOccupiedIndex { get; set; }
    
    private int ParkTime { get; set; }

    public override string ToString() => $"[CAR\t{Id},\t{Status}\t]";

    private CarClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id) : base(mqttClient)
    {
        Id = id;
        PhysicalWorld = physicalWorld;
        
        // init position
        Position = StreetPosition.WithRandomDistance(physicalWorld.StreetEdges.RandomElement());
        Position.StreetEdge.IncrementCarCount();
        
        // get initial dest
        UpdateDestination();
    }
    
    public static async Task<CarClient> Create(MqttClientFactory clientFactory, int id,
        PhysicalWorld physicalWorld)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tickgen/tick"));
        return new CarClient(client, physicalWorld, id);
    }

    
    /**
     * Main CarClient behaviour
     */
    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        switch (Status)
        {
            case CarClientStatus.PATHING_FAILED: // pathing failed to destination failed and has to be redone
                HandlePathingFailed();
                break;

            case CarClientStatus.PARKED: // car is parked for a random amount of ticks
                HandleParked();
                break;

            case CarClientStatus.PARKING: // car is driving around randomly looking for a available parking spot
                await HandleParking();
                break;

            case CarClientStatus.DRIVING: // car is driving according to the shortes path to their random destination
                await HandleDriving();
                break;
            
            default:
                throw new InvalidOperationException($"{this}\tInvalid status: {Status}");
        }
    }

    private void HandlePathingFailed()
    {
        UpdateDestination();
    }

    private void HandleParked()
    {
        Console.WriteLine($"{this}\ttick | Parked at {Position} | {ParkTime} ticks remaining");
        if (ParkTime == 0)
        {
            ResetAfterParking();
        }
        else
        {
            ParkTime--;
        }
    }

    private async Task HandleParking()
    {
        TicksSpentParking++;
        await DriveAccordingToPath();
        if (NodeReached())
        {
            DistanceTravelledParking += Position.StreetEdge.Length;
            NextStreetToLookForParking();
        }
        else 
        {
            (bool parkingFound, int newLastPassedOrFound) = Position.StreetEdge.TryParkingLocally(Position.DistanceFromSource, LastParkingSpotPassedIndex);
            LastParkingSpotPassedIndex = newLastPassedOrFound;
            if (parkingFound) // available spot found
            {
                await ParkCar();
            }
        }
    }

    private async Task HandleDriving()
    {
        if (DestinationReached()) // car reached destination after driving
        {
            InitLookingForParking();
        }
        else 
        {
            await DriveAccordingToPath();
        }
    }

    private async Task ParkCar()
    {
        LastOccupiedIndex = LastParkingSpotPassedIndex;
        Position = new StreetPosition(Position.StreetEdge, Position.StreetEdge.ParkingSpots[LastOccupiedIndex].DistanceFromSource);
        Status = CarClientStatus.PARKED;
        Random rand = new Random();
        ParkTime = rand.Next(0, MaxParkTime + 1);
        DistanceTravelledParking += Position.StreetEdge.ParkingSpots[LastOccupiedIndex].DistanceFromSource;

        await PublishParkingKpis();
        await PublishEnvironmentKpis();
    }

    private void InitLookingForParking()
    {
        TicksSpentParking = 0;
        Status = CarClientStatus.PARKING;
        NextStreetToLookForParking();
    }

    private void NextStreetToLookForParking()
    {
        StreetEdge nextStreet;
        if (PhysicalWorld.Graph.TryGetOutEdges(Position.StreetEdge.Target, out var outGoingStreets)) // TODO possible parking spot search heuristic
        {
            nextStreet = outGoingStreets.ToList().RandomElement();
            Path = new List<StreetEdge> { nextStreet } ;
            LastParkingSpotPassedIndex = 0; 
        }
    }

    private bool DestinationReached()
    {
        return !Path.Any() && Destination == Position.StreetEdge.Target &&
               Position.DistanceFromSource >= Position.StreetEdge.Length;
    }

    private bool NodeReached()
    {
        return Position.DistanceFromSource >= Position.StreetEdge.Length;
    }


    private async Task DriveAccordingToPath()
    {
        if (Position.DistanceFromSource < Position.StreetEdge.Length) // driving on street
        {
            UpdatePosition();
        }
        else
        {
            await HandleNodeReached();
        }
    }

    private async Task HandleNodeReached()
    {
        var overlap = Position.DistanceFromSource - Position.StreetEdge.Length;
        Position.StreetEdge.DecrementCarCount();
        Position = new StreetPosition(Path.First(), overlap);
        Position.StreetEdge.IncrementCarCount();
        Path = Path.Skip(1);
        Console.WriteLine($"{this}\ttick | {Position.ToString()} | dest: {Destination.Id} | car count: {Position.StreetEdge.CarCount} | driving at {Position.StreetEdge.CurrentMaxSpeed():F2}kmh/{Position.StreetEdge.SpeedLimit:F2}kmh");

        await PublishTrafficKpis();
    }

    private void UpdatePosition()
    {
        double speed = Position.StreetEdge.CurrentMaxSpeed();
        Position = new StreetPosition(Position.StreetEdge, Position.DistanceFromSource + MathUtil.KmhToMs(speed));
        Console.WriteLine($"{this}\ttick | {Position.ToString()} | dest: {Destination.Id} | car count: {Position.StreetEdge.CarCount} | driving at {speed:F2}kmh/{Position.StreetEdge.SpeedLimit:F2}kmh");
    }

    private void ResetAfterParking()
    {
        DistanceTravelledParking = 0;
        Position.StreetEdge.FreeParkingSpot(LastOccupiedIndex);
        UpdateDestination();
    }

    private void UpdateDestination()
    {
        Status = CarClientStatus.DRIVING;
        Destination = PhysicalWorld.StreetNodes.RandomElement();
        UpdatePath();
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
            Path = Enumerable.Empty<StreetEdge>();
            Status = CarClientStatus.PATHING_FAILED;
        }
    }
    
    // ----------------------------------- PUBLISH KPIs ----------------------------------- 

    private async Task PublishTrafficKpis()
    {
        // car count
        var payload = Encoding.UTF8.GetBytes(Position.StreetEdge.CarCount.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/carCount", Payload = payload });
        
        // speed reduction
        double speedReduction = 100 - ((Position.StreetEdge.CurrentMaxSpeed() / Position.StreetEdge.SpeedLimit) * 100);
        payload = Encoding.UTF8.GetBytes(speedReduction.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/speedReduction", Payload = payload });
    }

    private async Task PublishParkingKpis()
    {
        // distance travelled parking
        var payload = Encoding.UTF8.GetBytes(DistanceTravelledParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/distTravelledParking", Payload = payload });
        
        // time spent parking
        payload = Encoding.UTF8.GetBytes(TicksSpentParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/timeSpentParking", Payload = payload });
        
        // distance from destination
        double distance = Position.StreetEdge.Length - Position.DistanceFromSource;
        var shortestPaths = PhysicalWorld.Graph.ShortestPathsDijkstra(
            edge => 100 - edge.SpeedLimit,
            Position.StreetEdge.Source);

        if (shortestPaths.Invoke(Destination, out var path))
        {
            Path = path;
            distance += Path.Sum(streetEdge => streetEdge.Length);
        }

        double distanceFromDestination = distance;
        payload = Encoding.UTF8.GetBytes(distanceFromDestination.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/distFromDest", Payload = payload });
    }

    private async Task PublishEnvironmentKpis()
    {
        // fuel consumption
        double totalFuelConsumptionParking = (DistanceTravelledParking / 1000) * (FuelConsumptionRate / 100);
        var payload = Encoding.UTF8.GetBytes(totalFuelConsumptionParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/fuelConsumption", Payload = payload });
        
        // co2 emissions
        int totalCO2EmissionsParking = (int)((DistanceTravelledParking / 1000) * CO2EmissionRate);
        payload = Encoding.UTF8.GetBytes(totalCO2EmissionsParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/parkingEmissions", Payload = payload });
    }

}