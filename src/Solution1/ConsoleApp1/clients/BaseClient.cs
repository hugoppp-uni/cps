using MQTTnet.Client;

namespace ConsoleApp1;

public abstract class BaseClient
{
    
    protected readonly IMqttClient MqttClient;

    protected BaseClient(IMqttClient mqttClient)
    {
        MqttClient = mqttClient;
        MqttClient.ApplicationMessageReceivedAsync+=MqttClientOnApplicationMessageReceivedAsync;
    }

    protected abstract Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg);
    
    public async Task Disconnect()
    {
        await MqttClient.DisconnectAsync();
        MqttClient.Dispose();
    }
}