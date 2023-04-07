using System.Text;
using ConsoleApp1;
using MQTTnet.Client;

for (int i = 50; i > 1; i -= 1)
{
    Console.WriteLine($"{i}m - {MathUtil.MsToKmh(MathUtil.GetSafeSpeedMs(i)):F1}kmh");
}

for (int i = 0; i < 16; i++)
{
    Console.WriteLine(
        $"{i} cars on 100m - {MathUtil.MsToKmh(new Street { Length = 100, CarCount = i, SpeedLimit = 50 }.CurrentMaxSpeed()):F1}kmh");
}

var graph = StreetGraphParser.Parse(File.ReadAllText("res/street.json"));
var physicalWorld = new PhysicalWorld(graph);
var graphviz = graph.ToGraphvizFormatted().Replace("->", "--");

var mqttClientFactory = new MqttClientFactory { Host = "localhost" };

CancellationTokenSource cancellationTokenSource = new();
var tickClientTask = (await TickClient.Create(mqttClientFactory)).Run(cancellationTokenSource.Token);
var carClient = Enumerable.Range(0, 10)
    .Select(i => CarClient.Create(mqttClientFactory, i, physicalWorld));
var cars = await Task.WhenAll(carClient);

while (Console.ReadKey().Key != ConsoleKey.Q)
{
}

cancellationTokenSource.Cancel();
await Task.WhenAll(cars.Select(c => c.Disconnect()));
await tickClientTask;