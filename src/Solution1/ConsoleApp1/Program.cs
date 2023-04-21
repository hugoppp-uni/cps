using System.Runtime.InteropServices;
using ConsoleApp1;
using QuickGraph;

// TODO car clients park at free parking spaces in close proximity to their destination
// TODO after arbitrary amount of time they find a random new destination and repeat the proces
// TODO measure kpis such as average time spent finding parking 

// parse osm data into graph
const string ASSETS_PATH = "../../../assets/";
var graph = StreetGraphParser.Parse(File.ReadAllText(ASSETS_PATH + "street.json"));

// generate dot file of graph
var graphviz = graph.ToGraphvizFormatted();//.Replace("->", "--");
File.WriteAllText(ASSETS_PATH + "street_graph.dot", graphviz);

// init sim
var physicalWorld = new PhysicalWorld(graph);

// init client factory 
var mqttClientFactory = new MqttClientFactory { Host = "localhost" };

// set up cancellation 
CancellationTokenSource cancellationTokenSource = new();

// start tick client
var tickClientTask = (await TickClient.Create(mqttClientFactory)).Run(cancellationTokenSource.Token);

// init car clients
var carClient = Enumerable.Range(0, 5)
    .Select(i => CarClient.Create(mqttClientFactory, i, physicalWorld));
var cars = await Task.WhenAll(carClient);

// cancel with 'q'
while (Console.ReadKey().Key != ConsoleKey.Q)
{
}
cancellationTokenSource.Cancel();

// wait for rest
await Task.WhenAll(cars.Select(c => c.Disconnect()));
await tickClientTask;