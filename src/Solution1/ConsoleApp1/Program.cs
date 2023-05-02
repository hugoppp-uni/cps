using System.Runtime.InteropServices;
using ConsoleApp1;
using QuickGraph;

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

// init cruisers 
var cruiserClients = Enumerable.Range(0, 200)
    .Select(i => CruiserClient.Create(mqttClientFactory, i, physicalWorld, false));
var cruisers = await Task.WhenAll(cruiserClients);

// init parkers 
var parkerClients = Enumerable.Range(0, 50)
    .Select(i => ParkerClient.Create(mqttClientFactory, i, physicalWorld, true));
var parkers = await Task.WhenAll(parkerClients);

// cancel with 'q'
while (Console.ReadKey().Key != ConsoleKey.Q)
{
}
cancellationTokenSource.Cancel();

// wait for rest
await Task.WhenAll(cruisers.Select(c => c.Disconnect()));
await Task.WhenAll(parkers.Select(c => c.Disconnect()));
await tickClientTask;