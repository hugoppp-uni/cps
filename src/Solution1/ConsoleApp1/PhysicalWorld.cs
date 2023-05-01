using QuickGraph;

namespace ConsoleApp1;

// TODO needs to be thread safe
public class PhysicalWorld
{
    public PhysicalWorld(IMutableBidirectionalGraph<StreetNode, StreetEdge> graph)
    {
        Graph = graph;
        StreetNodes = graph.Vertices.ToList();
        StreetEdges = graph.Edges.ToList();
        foreach (var edge in StreetEdges)
        {
            edge.InitParkingSpots();
        }
    }


    public IMutableBidirectionalGraph<StreetNode, StreetEdge> Graph { get; }
    public IReadOnlyList<StreetNode> StreetNodes { get; }
    public IReadOnlyList<StreetEdge> StreetEdges { get; }
}