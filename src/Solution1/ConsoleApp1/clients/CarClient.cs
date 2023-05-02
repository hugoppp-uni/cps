using System.Data;
using System.Drawing;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using QuickGraph;
using QuickGraph.Algorithms;

namespace ConsoleApp1;

public struct StreetPosition
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

public enum CarClientStatus
{
    DRIVING,
    PARKING,
    PARKED,
    PATHING_FAILED
}

public abstract class CarClient : BaseClient
{
    protected int Id { get; }
    protected PhysicalWorld PhysicalWorld { get; }
    protected StreetPosition Position { get; set; }
    protected IEnumerable<StreetEdge> Path { get; set; }
    protected StreetNode Destination { get; set; }

    protected CarClientStatus Status { get; set; }

    public override string ToString() => $"[CAR\t{Id},\t{Status}\t]";

    protected CarClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id) : base(mqttClient)
    {
        Id = id;
        PhysicalWorld = physicalWorld;
        
        // init position
        Position = StreetPosition.WithRandomDistance(physicalWorld.StreetEdges.RandomElement());
        Position.StreetEdge.IncrementCarCount();
        
        // get initial dest
        UpdateDestination();
    }

    protected void HandlePathingFailed()
    {
        UpdateDestination();
    }

    protected async Task HandleDriving()
    {
        if (DestinationReached()) // car reached destination after driving
        {
            HandleDestinationReached();
        }
        else 
        {
            await DriveAccordingToPath();
        }
    }
    
    protected abstract void HandleDestinationReached();
    
    private bool DestinationReached()
    {
        return !Path.Any() && Destination == Position.StreetEdge.Target &&
               Position.DistanceFromSource >= Position.StreetEdge.Length;
    }

    protected async Task DriveAccordingToPath()
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

        // TODO performance issues, find better way to publish traffic situation
        // await PublishTrafficKpis();
    }

    private void UpdatePosition()
    {
        double speed = Position.StreetEdge.CurrentMaxSpeed();
        Position = new StreetPosition(Position.StreetEdge, Position.DistanceFromSource + MathUtil.KmhToMs(speed));
        Console.WriteLine($"{this}\ttick | {Position.ToString()} | dest: {Destination.Id} | car count: {Position.StreetEdge.CarCount} | driving at {speed:F2}kmh/{Position.StreetEdge.SpeedLimit:F2}kmh");
    }

    protected void UpdateDestination()
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
    
}