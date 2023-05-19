using ConsoleApp1.sim;
using ConsoleApp1.util;
using MQTTnet.Client;

namespace ConsoleApp1.clients;

public class CruiserClient: CarClient
{
    protected CruiserClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id, bool logging) : base(mqttClient, physicalWorld, id, logging) {}
    
    /*
     * Creation through factory
     */
    public static async Task<CarClient> Create(MqttClientFactory clientFactory, int id,
        PhysicalWorld physicalWorld, bool logging)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tickgen/tick"));
        return new CruiserClient(client, physicalWorld, id, logging);
    }
    
    /**
     * Main ParkerClient behaviour
     */
    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        switch (Status)
        {
            case CarClientStatus.PathingFailed: // pathing failed to destination failed and has to be redone
                HandlePathingFailed();
                break;

            case CarClientStatus.Driving: // car is driving according to the shortest path to their random destination
                await HandleDriving();
                break;
            
            default:
                throw new InvalidOperationException($"{this}\tInvalid status: {Status}");
        }
    }
    
    protected override void TurnOnNextStreetEdge()
    {
        var overlap = Position.DistanceFromSource - Position.StreetEdge.Length;
        Position.StreetEdge.DecrementCarCount();
        Position = new StreetPosition(Path.First(), overlap);
        Position.StreetEdge.IncrementCarCount();
        Path = Path.Skip(1);
        
        if (Logging)
        {
            Console.WriteLine($"{this}\ttick | {Position.ToString()} | dest: {Destination.Id} | car count: {Position.StreetEdge.CarCount} | driving at {Position.StreetEdge.CurrentMaxSpeed():F2}kmh/{Position.StreetEdge.SpeedLimit:F2}kmh");
        }
    }
    
    protected override void UpdateDestination()
    {
        Status = CarClientStatus.Driving;
        Destination = PhysicalWorld.StreetNodes.RandomElement();
        UpdatePath();
    }

    protected override void HandleDestinationReached()
    {
        UpdateDestination();
    }

    protected override async Task HandleNodeReached()
    {
        TurnOnNextStreetEdge();
    }
    
}