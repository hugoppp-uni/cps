using MQTTnet.Client;
using QuickGraph.Algorithms;

namespace ConsoleApp1.clients;

public enum CarClientStatus
{
    Driving,
    Parking,
    Parked,
    PathingFailed
}

public abstract class CarClient : BaseClient
{
    protected int Id { get; }
    protected PhysicalWorld PhysicalWorld { get; }
    protected StreetPosition Position { get; set; }
    protected IEnumerable<StreetEdge> Path { get; set; }
    protected StreetNode Destination { get; set; }

    protected CarClientStatus Status { get; set; }
    
    protected bool Logging { get; set; }

    public override string ToString() => $"[CAR\t{Id},\t{Status}\t]";
    
    protected abstract void HandleDestinationReached();
    protected abstract Task HandleNodeReached();
    protected abstract void TurnOnNextStreetEdge();
    protected abstract void UpdateDestination();

    protected CarClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id, bool logging) : base(mqttClient)
    {
        Id = id;
        PhysicalWorld = physicalWorld;
        Logging = logging;
        Path = Enumerable.Empty<StreetEdge>();
        
        // init position
        Position = StreetPosition.WithRandomDistance(physicalWorld.StreetEdges.RandomElement());
        Position.StreetEdge.IncrementCarCount();
        
        // get initial dest
        UpdateDestination();
    }

    protected void HandlePathingFailed()
    {
        RespawnAtRandom();
    }

    protected void RespawnAtRandom()
    {
        Position.StreetEdge.DecrementCarCount();
        Position = StreetPosition.WithRandomDistance(PhysicalWorld.StreetEdges.RandomElement());
        Position.StreetEdge.IncrementCarCount();
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
    
    private void UpdatePosition()
    {
        double speed = Position.StreetEdge.CurrentMaxSpeed();
        Position = new StreetPosition(Position.StreetEdge, Position.DistanceFromSource + MathUtil.KmhToMs(speed));
        
        if (Logging)
        {
            Console.WriteLine($"{this}\ttick | {Position.ToString()} | dest: {Destination.Id} | car count: {Position.StreetEdge.CarCount} | driving at {speed:F2}kmh/{Position.StreetEdge.SpeedLimit:F2}kmh");
        }
        
    }

    protected void UpdatePath()
    {
        var shortestPaths = PhysicalWorld.Graph.ShortestPathsDijkstra(
            edge => 100 - edge.SpeedLimit,
            Position.StreetEdge.Source);
    
        if (shortestPaths.Invoke(Destination, out var path))
        {
            Path = path.ToList();
            if (Logging)
            {
                Console.WriteLine($"{this}\tpath: {string.Join(',', Path.Select(p => p.StreetName))}");
            }
        }
        else
        {
            if (Logging)
            {
                Console.WriteLine($"{this}\tno path found [destination: {Destination.Id}, position: {Position}, node: {Position.StreetEdge.Source.Id}]");
            }
            Path = Enumerable.Empty<StreetEdge>();
            Status = CarClientStatus.PathingFailed;
        }
    }
    
}