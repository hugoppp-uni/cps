using MQTTnet;
using MQTTnet.Client;
using QuickGraph;

namespace ConsoleApp1.clients;

public class MqttClientFactory
{
    private static readonly MqttFactory MqttFactory = new();
    public required string Host { get; init; }
    public required int Port { get; init; }

    public async Task<IMqttClient> CreateClient(
        Action<MqttClientSubscribeOptionsBuilder>? subscribeOptionsConfig = null
    )
    {
        var mqttClient = MqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(Host, Port)
            .Build();

        try
        {
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }

        if (subscribeOptionsConfig is not null)
        {
            var subscribeOptions = MqttFactory.CreateSubscribeOptionsBuilder();
            subscribeOptionsConfig?.Invoke(subscribeOptions);
            await mqttClient.SubscribeAsync(subscribeOptions.Build(), CancellationToken.None);
        }

        return mqttClient;
    }
}