using ConsoleApp1;
using ConsoleApp1.clients;
using ConsoleApp1.pgs;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;

// TODO: implement ParkingSpaceClient and reserving service for realistic PGS
// TODO: compare different street map scenarios (parameters)
// TODO: implement PGS as server with MQTT communication

const string assetsPath = "../../../assets/";
const int brokerPort = 1883;

// parse osm data into graph
var graph = StreetGraphParser.Parse(File.ReadAllText(Path.Combine(assetsPath, "street.json")));

// generate dot file of graph
var graphviz = graph.ToGraphvizFormatted(); 
File.WriteAllText(Path.Combine(assetsPath, "street_graph.dot"), graphviz);

// init client factory 
var mqttClientFactory = new MqttClientFactory { Host = "localhost", Port = brokerPort };

// set up cancellation 
CancellationTokenSource cancellationTokenSource = new();

// start tick client
var tickClientTask = (await TickClient.Create(mqttClientFactory)).Run(cancellationTokenSource.Token);

// sim
const int rogueParkerCount = 25;
const int parkerCount = 50;
const int cruiserCount = 200;
int simDataPublishInterval = 25;
var physicalWorld = new PhysicalWorld(graph, 10, parkerCount, cruiserCount, rogueParkerCount);

// parking guidance system
ParkingGuidanceSystem pgs = new ParkingGuidanceSystem(physicalWorld, new NearestParkingStrategy(), false);

// init cruisers 
var cruiserClients = Enumerable.Range(0, cruiserCount)
    .Select(i => CarClient.Create(mqttClientFactory, new CruiserClientBehaviour(false), physicalWorld, pgs, false, i));
var cruisers = await Task.WhenAll(cruiserClients);

// init parkers 
Random coinFlip = new Random();
var parkerClients = Enumerable.Range(0, parkerCount)
    .Select(i => CarClient.Create(mqttClientFactory, new RandomParkerClientBehaviour(true), physicalWorld, pgs, coinFlip.NextDouble() < 0.5, i));
var parkers = await Task.WhenAll(parkerClients);

// sim data
var simDataTask = await SimDataClient.Create(mqttClientFactory, physicalWorld, simDataPublishInterval);

// cancel with 'q'
while (Console.ReadKey().Key != ConsoleKey.Q)
{
}

cancellationTokenSource.Cancel();

// wait for rest
await Task.WhenAll(cruisers.Select(c => c.Disconnect()));
await Task.WhenAll(parkers.Select(c => c.Disconnect()));
await tickClientTask;