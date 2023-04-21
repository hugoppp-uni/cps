using QuickGraph;

namespace ConsoleApp1;

// TODO needs to be thread safe
public class PhysicalWorld
{
    public PhysicalWorld(IUndirectedGraph<StreetNode, StreetEdge> graph)
    {
        Graph = graph;
        StreetNodes = graph.Vertices.ToList();
        StreetEdges = graph.Edges.ToList();
    }


    public IUndirectedGraph<StreetNode, StreetEdge> Graph { get; }
    public IReadOnlyList<StreetNode> StreetNodes { get; }
    public IReadOnlyList<StreetEdge> StreetEdges { get; }
}