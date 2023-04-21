using System.Collections;
using MQTTnet;
using MQTTnet.Client;
using QuickGraph;
using QuickGraph.Graphviz;

namespace ConsoleApp1;

public static class Extensions
{
    public static string ToGraphvizFormatted(this IMutableBidirectionalGraph<StreetNode, StreetEdge> graph)
    {
        var graphviz = graph.ToGraphviz(algorithm =>
        {
            algorithm.FormatVertex += (sender, args) =>
            {
                args.VertexFormatter.Label = $"{args.Vertex.Id}";
            };
            algorithm.FormatEdge += (sender, args) =>
            {
                args.EdgeFormatter.Label.Value =
                    $"{args.Edge.Tags.GetValueOrDefault("name")} ({args.Edge.Length:0}m)";
            };
        });
        return graphviz ?? throw new Exception();
    }
    
    public static T RandomElement<T>(this IReadOnlyList<T> collection)
    {
        return collection[Random.Shared.Next(0, collection.Count)];
    }


}