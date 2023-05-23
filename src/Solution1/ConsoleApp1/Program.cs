using ConsoleApp1;
using ConsoleApp1.clients;
using ConsoleApp1.pgs;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;

// TODO: implement parking guidance switch
// TODO: handle MQTT connection errors
// TODO: implement PGS as server with MQTT communication
// TODO: implement ParkingSpaceClient and reserving service for realistic PGS
// TODO: compare different street map scenarios (parameters)

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
const int parkerCount = 10;
const int cruiserCount = 400;
int simDataPublishInterval = 20;
var physicalWorld = new PhysicalWorld(graph, parkerCount, cruiserCount);

// sim meta data
var simDataTask = await SimDataClient.Create(mqttClientFactory, physicalWorld, simDataPublishInterval);

// parking guidance system
ParkingGuidanceSystem pgs = new ParkingGuidanceSystem(physicalWorld, new NearestParkingStrategy(), false);

// init cruisers 
var cruiserClients = Enumerable.Range(0, cruiserCount)
    .Select(i => CarClient.Create(mqttClientFactory, new CruiserClientBehaviour(), physicalWorld, pgs, i, false));
var cruisers = await Task.WhenAll(cruiserClients);

// init parkers 
var parkerClients = Enumerable.Range(0, parkerCount)
    .Select(i => CarClient.Create(mqttClientFactory, new RandomParkerClientBehaviour(), physicalWorld, pgs, i, true));
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