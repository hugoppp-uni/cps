using MQTTnet.Client;

namespace ConsoleApp1;

public class CarClient: BaseClient
{
    private int Id { get;  }
    private PhysicalWorld PhysicalWorld { get;  }

    private CarClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id) : base(mqttClient)
    {
        Id = id;
        PhysicalWorld = physicalWorld;
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