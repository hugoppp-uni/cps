using System.Diagnostics;
using System.Text;
using MQTTnet;
using MQTTnet.Client;

namespace ConsoleApp1.clients;

public class TickClient : BaseClient
{
    private int _tick = 1;
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);


    public async Task Run(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var t1 = Stopwatch.GetTimestamp();
            await MqttClient.PublishAsync(
                new MqttApplicationMessage() { Topic = "tickgen/tick", Payload = Encoding.UTF8.GetBytes(_tick++.ToString()) },
                token);
            var t2 = Stopwatch.GetTimestamp();

            var timeToWait = Delay - TimeSpan.FromTicks(t2 - t1);
            if (timeToWait > TimeSpan.Zero)
                try
                {
                    await Task.Delay(timeToWait, token);
                }
                catch (TaskCanceledException)
                {
                }
        }

        await Disconnect();
    }

    protected override Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        var payloadString = arg.ApplicationMessage.ConvertPayloadToString();
        if (int.TryParse(payloadString, out int tickDelayMs))
        {
            Delay = TimeSpan.FromMilliseconds(tickDelayMs);
        }
        else
        {
            Console.WriteLine($"Invalid tick delay {payloadString}");
        }

        return Task.CompletedTask;
    }


    public static async Task<TickClient> Create(MqttClientFactory clientFactory)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tickgen/tick_delay"));
        return new TickClient(client);
    }

    public TickClient(IMqttClient mqttClient) : base(mqttClient)
    {
    }
}