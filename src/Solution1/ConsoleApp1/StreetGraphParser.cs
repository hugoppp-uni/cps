using QuickGraph;
using System.Text.Json;
using System.Text.Json.Serialization;
using Geolocation;
using QuickGraph.Serialization.DirectedGraphML;

namespace ConsoleApp1;

public static class StreetGraphParser
{
    private class OsmWay
    {
        [JsonPropertyName("id")] public required int Id { get; init; }
        [JsonPropertyName("tags")] public required Dictionary<string, string> Tags { get; init; }
        [JsonPropertyName("nodes")] public required IReadOnlyList<long> NodeIds { get; init; }
    }

    private class OsmNode
    {
        [JsonPropertyName("id")] public required long Id { get; init; }
        [JsonPropertyName("lat")] public required double Lat { get; init; }
        [JsonPropertyName("lon")] public required double Lon { get; init; }
    }

    private static bool dbgd = false;
    public static IMutableBidirectionalGraph<StreetNode, StreetEdge> Parse(string json)
    {
        var doc = JsonDocument.Parse(json);

        var graph = new BidirectionalGraph<StreetNode, StreetEdge>();

        // parse for street nodes and add to graph
        var nodes = doc.SelectElements("$.elements[?(@.type=='node')]")
            .Where(el => el is not null)
            .Select(el => el!.Value)
            .Select(el => el.Deserialize<OsmNode>())
            .Where(el => el is not null)
            .Select(el => el!)
            .ToDictionary(el => el.Id,
                node => new StreetNode() { Id = node.Id, Latitude = node.Lat, Longitude = node.Lon });
        
        graph.AddVertexRange(nodes.Values);

        // parse for osm ways
        var ways = doc.SelectElements("$.elements[?(@.type=='way')]")
            .Where(el => el is not null)
            .Select(el => el!.Value)
            .Select(el => el.Deserialize<OsmWay>())
            .Where(el => el is not null)
            .Select(el => el!)
            .ToList();

        // create street nodes from osm ways and add to graph
        foreach (var osmWay in ways)
        {
            foreach (var (first, second) in osmWay.NodeIds.Zip(osmWay.NodeIds.Skip(1),
                         (first, second) => (first, second: second)))
            {
                if (nodes.TryGetValue(first, out var firstNode) && nodes.TryGetValue(second, out var secondNode))
                {
                    // create street edges
                    var forwardEdge = new StreetEdge()
                    {
                        Source = firstNode,
                        Target = secondNode,
                        Length = GeoCalculator.GetDistance(firstNode.Coordinate, secondNode.Coordinate,
                            distanceUnit: DistanceUnit.Meters),
                        Tags = osmWay.Tags,
                        SpeedLimit = double.Parse(osmWay.Tags.GetValueOrDefault("maxspeed", "50")),
                    };
                    var backwardEdge = new StreetEdge()
                    {
                        Source = secondNode,
                        Target = firstNode,
                        Length = forwardEdge.Length,
                        Tags = osmWay.Tags,
                        SpeedLimit = forwardEdge.SpeedLimit,
                    };
                    
                    // add edge to graph
                    graph.AddEdge(forwardEdge);
                    graph.AddEdge(backwardEdge);
                }
            }
        }

        SimplifyGraph(graph);
        return graph;
    }

    public static void SimplifyGraph(IMutableBidirectionalGraph<StreetNode, StreetEdge> graph)
    {
        for (bool needsRerun = true; needsRerun;)
        {
            needsRerun = false;
            foreach (var node in graph.Vertices.ToList())
            {
                // if only 2 streets go in and out of a node it can be simplified to 1 street
                if (graph.InDegree(node) == 2 && graph.OutDegree(node) == 2)
                {
                    // same index -> same street
                    var in0 = graph.InEdge(node, 0);
                    var out0 = graph.OutEdge(node, 0);
                    
                    var in1 = graph.InEdge(node, 1);
                    var out1 = graph.OutEdge(node, 1);
                    
                    // streets leading into different streets without crossing
                    if (in0.Tags.GetValueOrDefault("name") != in1.Tags.GetValueOrDefault("name"))
                        continue;
                    if (out0.Tags.GetValueOrDefault("name") != out1.Tags.GetValueOrDefault("name"))
                        continue;
                    if (in0.Tags.GetValueOrDefault("name") != out0.Tags.GetValueOrDefault("name"))
                        continue;
                    if (in1.Tags.GetValueOrDefault("name") != out1.Tags.GetValueOrDefault("name"))
                        continue;
                    
                    // new length for new connecting edges
                    var newLength = in0.Length + in1.Length;
                    
                    // find sources and targets for new connecting edges
                    var forwardSource = in0.Source;
                    var forwardTarget = out1.Target;
                    var backwardSource = in1.Source;
                    var backwardTarget = out0.Target;
                    
                    // assemble new connecting edges
                    var connectingForwardEdge = new StreetEdge()
                    {
                        Length = newLength,
                        Source = forwardSource,
                        Target = forwardTarget,
                        Tags = in0.Tags,
                        SpeedLimit = in0.SpeedLimit,
                    };
                    var connectingBackwardEdge = new StreetEdge()
                    {
                        Length = newLength,
                        Source = backwardSource,
                        Target = backwardTarget,
                        Tags = in1.Tags,
                        SpeedLimit = in1.SpeedLimit,
                    };
                    
                    // collapse node and add new connecting edges
                    graph.RemoveVertex(node);
                    graph.AddEdge(connectingForwardEdge);
                    graph.AddEdge(connectingBackwardEdge);
                    
                    needsRerun = true;
                }
            }
        }
    }
}