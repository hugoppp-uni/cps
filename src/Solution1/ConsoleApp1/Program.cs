using System.Runtime.InteropServices;
using ConsoleApp1;
using QuickGraph;

// TODO autos fahren mit random destinations umher 
// TODO autos parken und fahren nach random zeit weiter
// TODO kpis messen wie average time parking (vom zeitpunkt destination erreicht bis parking spot found)
// TODO parkplaetze auf nodes oder edges ?? nodes -> graph muss nicht vereinfacht werden
// TODO graphen gerichtet machen

// parse osm data into graph
const string ASSETS_PATH = "../../../assets/";
var graph = StreetGraphParser.Parse(File.ReadAllText(ASSETS_PATH + "street.json"));

// generate dot file of graph
var graphviz = graph.ToGraphvizFormatted();//.Replace("->", "--");
File.WriteAllText(ASSETS_PATH + "street_graph.dot", graphviz);

/*
// init sim
var physicalWorld = new PhysicalWorld(graph);

// init client factory 
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
*/