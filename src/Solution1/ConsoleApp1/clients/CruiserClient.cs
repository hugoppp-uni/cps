using MQTTnet.Client;

namespace ConsoleApp1;

public class CruiserClient: CarClient
{
    protected CruiserClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id) : base(mqttClient, physicalWorld, id) {}
    
    /*
     * Creation through factory
     */
    public static async Task<CarClient> Create(MqttClientFactory clientFactory, int id,
        PhysicalWorld physicalWorld)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tickgen/tick"));
        return new CruiserClient(client, physicalWorld, id);
    }
    
    /**
     * Main ParkerClient behaviour
     */
    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        switch (Status)
        {
            case CarClientStatus.PATHING_FAILED: // pathing failed to destination failed and has to be redone
                HandlePathingFailed();
                break;

            case CarClientStatus.DRIVING: // car is driving according to the shortest path to their random destination
                await HandleDriving();
                break;
            
            default:
                throw new InvalidOperationException($"{this}\tInvalid status: {Status}");
        }
    }

    protected override void HandleDestinationReached()
    {
        UpdateDestination();
    }
    
}