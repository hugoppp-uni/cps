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
        [JsonPropertyName("lat")] public  required double Lat { get; init; }
        [JsonPropertyName("lon")] public required double Lon { get; init; }
    }

    public static AdjacencyGraph<StreetNode, StreetEdge> Parse(string json)
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

        var graph = new AdjacencyGraph<StreetNode, StreetEdge>();
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
                        SpeedLimit = double.Parse(osmWay.Tags.GetValueOrDefault("maxspeed", "50"))
                    };
                    graph.AddEdge(streetEdge);
                }
            }
        }

        return graph;
    }
}