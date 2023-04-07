using QuickGraph;

namespace ConsoleApp1;

//needs to be thread safe
public class PhysicalWorld
{
    public PhysicalWorld(IUndirectedGraph<StreetNode, StreetEdge> graph)
    {
        Graph = graph;
        StreetNodes = graph.Vertices.ToList();
        StreetEdges = graph.Edges.ToList();
    }


    private IUndirectedGraph<StreetNode, StreetEdge> Graph { get; }
    private List<StreetNode> StreetNodes { get; }
    private List<StreetEdge> StreetEdges { get; }
}