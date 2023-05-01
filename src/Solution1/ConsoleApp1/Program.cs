using System.Runtime.InteropServices;
using ConsoleApp1;
using QuickGraph;

// TODO measure kpis 
    // TODO fuel consumption (while parking)
    // TODO CO2 emmisions (avg in US 200 g/km)
// TODO expection for broken connection
// TODO implement tick to daytime mapping for pretty
// TODO overhaul magic numbers into config and base on more research to make sim more realistic
// TODO praking guidance system
    // TODO regular cars, guided cars, regular parking spaces, guidance parking spaces
    // TODO compare daytime scenarios
    // TODO compare different street map scenarios

// parse osm data into graph
const string ASSETS_PATH = "../../../assets/";
var graph = StreetGraphParser.Parse(File.ReadAllText(ASSETS_PATH + "street.json"));

// generate dot file of graph
var graphviz = graph.ToGraphvizFormatted();//.Replace("->", "--");
File.WriteAllText(ASSETS_PATH + "street_graph.dot", graphviz);

// init sim
var physicalWorld = new PhysicalWorld(graph);

// init client factory 
const int BROKER_PORT = 8883;
var mqttClientFactory = new MqttClientFactory { Host = "localhost", Port = BROKER_PORT };

// set up cancellation 
CancellationTokenSource cancellationTokenSource = new();

// start tick client
var tickClientTask = (await TickClient.Create(mqttClientFactory)).Run(cancellationTokenSource.Token);

// init car clients
var carClient = Enumerable.Range(0, 100)
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