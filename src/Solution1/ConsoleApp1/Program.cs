using ConsoleApp1;
using ConsoleApp1.clients;
using ConsoleApp1.pgs;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;

// TODO: kpi: distance driven to parking spot / distance from source to destination ! REPLACE W/ DISTANCE TRAVELLED PARKING
// TODO: implement parking guidance switch
// TODO: handle MQTT connection errors
// TODO: overhaul config magic numbers (parking density, parking frequency, cruiser count, parker count, max park time) maybe change at runtime

// TODO: refactor CarClient, ParkerClient and CruiserClient into composition
// TODO: compare daytime scenarios
// TODO: compare different street map scenarios

const string assetsPath = "../../../assets/";
const int brokerPort = 1883;

// parse osm data into graph
var graph = StreetGraphParser.Parse(File.ReadAllText(Path.Combine(assetsPath, "street.json")));

// generate dot file of graph
var graphviz = graph.ToGraphvizFormatted(); 
File.WriteAllText(Path.Combine(assetsPath, "street_graph.dot"), graphviz);

// init sim
var physicalWorld = new PhysicalWorld(graph);

// init client factory 
var mqttClientFactory = new MqttClientFactory { Host = "localhost", Port = brokerPort };

// set up cancellation 
CancellationTokenSource cancellationTokenSource = new();

// start tick client
var tickClientTask = (await TickClient.Create(mqttClientFactory)).Run(cancellationTokenSource.Token);

// init cruisers 
var cruiserClients = Enumerable.Range(0, 250)
    .Select(i => CruiserClient.Create(mqttClientFactory, i, physicalWorld, false));
var cruisers = await Task.WhenAll(cruiserClients);

// init parkers 
ParkingGuidanceSystem pgs = new ParkingGuidanceSystem(physicalWorld, new NearestParkingStrategy(), true);
var parkerClients = Enumerable.Range(0, 50)
    .Select(i => ParkerClient.Create(mqttClientFactory, i, physicalWorld, pgs, true, true));
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