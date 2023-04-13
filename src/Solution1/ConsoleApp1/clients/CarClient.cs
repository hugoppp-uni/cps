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

    private CarClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id) : base(mqttClient)
    {
        Id = id;
        PhysicalWorld = physicalWorld;
        Position = StreetPosition.WithRandomDistance(physicalWorld.StreetEdges.RandomElement());
        var destination = PhysicalWorld.StreetNodes.RandomElement();
        if (PhysicalWorld.Graph.ShortestPathsDijkstra(edge => 100 - edge.SpeedLimit, Position.StreetEdge.Source)
            .Invoke(destination, out var path))
        {
            Console.WriteLine($"starting at: {Position}\n" +
                              $"\ttarget: {PhysicalWorld.Graph.AdjacentEdge(destination, 0).StreetName}\n" +
                              $"\tpath: {string.Join(',', path.Select(p => p.StreetName))}");
        }
    }


    protected override Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        Console.WriteLine($"Tick car {Id}");
        return Task.CompletedTask;
    }


    public static async Task<CarClient> Create(MqttClientFactory clientFactory, int id,
        PhysicalWorld physicalWorld)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tick"));
        return new CarClient(client, physicalWorld, id);
    }
}