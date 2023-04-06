using QuickGraph;
using QuickGraph.Graphviz;

namespace ConsoleApp1;

public static class Extensions
{
    public static string ToGraphvizFormatted(this IEdgeListGraph<StreetNode, StreetEdge> graph)
    {
        var graphviz = graph.ToGraphviz(algorithm =>
        {
            algorithm.FormatEdge += (sender, args) =>
            {
                args.EdgeFormatter.Label.Value =
                    $"{args.Edge.Tags.GetValueOrDefault("name")} ({args.Edge.Length:0}m)";
            };
        });
        return graphviz ?? throw new Exception();
    }
}