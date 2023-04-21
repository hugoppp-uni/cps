using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
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

    public override string ToString() => $"{StreetEdge.StreetName} ({DistanceFromSource:F1}m / {StreetEdge.Length:F1}m)";

    public StreetEdge StreetEdge { get; }
    public double DistanceFromSource { get; }
}

public class CarClient : BaseClient
{
    private int Id { get; }
    private PhysicalWorld PhysicalWorld { get; }
    private StreetPosition Position { get; set; }
    private IEnumerable<StreetEdge> Path { get; set; }
    private StreetNode Destination { get; set; } 

    private CarClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id) : base(mqttClient)
    {
        Id = id;
        PhysicalWorld = physicalWorld;
        Position = StreetPosition.WithRandomDistance(physicalWorld.StreetEdges.RandomElement());
        Destination = PhysicalWorld.StreetNodes.RandomElement();
        Console.WriteLine($"[CAR {Id}]\tinitial position: {Position.ToString()} | initial destination {Destination.Id}");
        UpdatePath();
    }


    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        // destination reached or path not found, generate new destination and path
        if (!Path.Any())
        {
            Destination = PhysicalWorld.StreetNodes.RandomElement();
            Console.WriteLine($"[CAR {Id}]\t{Destination.Id} reached | new destination: {Destination.Id}");
            UpdatePath();
        }
        else if (Path != null && Path.Any()) // still following path
        {
            // TODO update position according to tempo on tick
            Position = new StreetPosition(Path.First(), 0);
            Path = Path.Skip(1);
            //await PublishPosition(); 
        } 
    }

    private void UpdatePath()
    {
        // compute shortest path to destination
        if (PhysicalWorld.Graph.ShortestPathsDijkstra(edge => 100 - edge.SpeedLimit, Position.StreetEdge.Source)
            .Invoke(Destination, out var path))
        {
            Path = path;
            Console.WriteLine($"[CAR {Id}]\tpath: {string.Join(',', path.Select(p => p.StreetName))}");
        }
        else // path finding failed, i.e. when graph is not fully connected
        {
            // TODO find new destination 
            Console.WriteLine("--------------");
            Console.WriteLine($"[CAR {Id}]\tno path found [destination: {Destination.Id}, position: {Position.ToString()}, node: {Position.StreetEdge.Source.Id}]");
            // empty path
            Path = Enumerable.Empty<StreetEdge>();
        }
    }
    
    // TODO publish car position 
    private async Task PublishPosition()
    {
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { CarId = Id, Position }));
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "position", Payload = payload });
    }


    public static async Task<CarClient> Create(MqttClientFactory clientFactory, int id,
        PhysicalWorld physicalWorld)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tick"));
        return new CarClient(client, physicalWorld, id);
    }
}