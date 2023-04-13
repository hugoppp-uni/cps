using ConsoleApp1;


var graph = StreetGraphParser.Parse(File.ReadAllText("res/street.json"));
var physicalWorld = new PhysicalWorld(graph);
// var graphviz = graph.ToGraphvizFormatted().Replace("->", "--");

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