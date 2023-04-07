using QuickGraph;
using System.Text.Json;
using System.Text.Json.Serialization;
using Geolocation;

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

    public static IUndirectedGraph<StreetNode, StreetEdge> Parse(string json)
    {
        var doc = JsonDocument.Parse(json);

        var nodes = doc.SelectElements("$.elements[?(@.type=='node')]")
            .Where(el => el is not null)
            .Select(el => el!.Value)
            .Select(el => el.Deserialize<OsmNode>())
            .Where(el => el is not null)
            .Select(el => el!)
            .ToDictionary(el => el.Id,
                node => new StreetNode() { Id = node.Id, Latitude = node.Lat, Longitude = node.Lon });

        var ways = doc.SelectElements("$.elements[?(@.type=='way')]")
            .Where(el => el is not null)
            .Select(el => el!.Value)
            .Select(el => el.Deserialize<OsmWay>())
            .Where(el => el is not null)
            .Select(el => el!)
            .ToList();

        var graph = new UndirectedGraph<StreetNode, StreetEdge>();
        graph.AddVertexRange(nodes.Values);

        foreach (var osmWay in ways)
        {
            foreach (var (first, second) in osmWay.NodeIds.Zip(osmWay.NodeIds.Skip(1),
                         (first, second) => (first, second: second)))
            {
                if (nodes.TryGetValue(first, out var firstNode) && nodes.TryGetValue(second, out var secondNode))
                {
                    var streetEdge = new StreetEdge()
                    {
                        Source = firstNode,
                        Target = secondNode,
                        Length = GeoCalculator.GetDistance(firstNode.Coordinate, secondNode.Coordinate,
                            distanceUnit: DistanceUnit.Meters),
                        Tags = osmWay.Tags,
                        SpeedLimit = double.Parse(osmWay.Tags.GetValueOrDefault("maxspeed", "50")),
                    };
                    graph.AddEdge(streetEdge);
                }
            }
        }

        SimplifyGraph(graph);
        return graph;
    }

    public static void SimplifyGraph(UndirectedGraph<StreetNode, StreetEdge> graph)
    {
        for (bool needsRerun = true; needsRerun;)
        {
            needsRerun = false;
            foreach (var node in graph.Vertices.ToList())
            {
                if (graph.AdjacentDegree(node) == 2)
                {
                    var edge1 = graph.AdjacentEdge(node, 0);
                    var edge2 = graph.AdjacentEdge(node, 1);
                    if (edge1.Tags.GetValueOrDefault("name") != edge2.Tags.GetValueOrDefault("name"))
                        continue;

                    var source = edge1.Source == node ? edge1.Target : edge1.Source;
                    var target = edge2.Source == node ? edge2.Target : edge2.Source;
                    var newLength = edge1.Length + edge2.Length;
                    var newEdge = new StreetEdge()
                    {
                        Length = newLength,
                        Source = source,
                        Target = target,
                        Tags = edge1.Tags,
                        SpeedLimit = edge1.SpeedLimit,
                    };
                    graph.RemoveVertex(node);
                    graph.AddEdge(newEdge);
                    needsRerun = true;
                }
            }
        }
    }
}