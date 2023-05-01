using QuickGraph;

namespace ConsoleApp1;

public class PhysicalWorld
{
    public PhysicalWorld(IMutableBidirectionalGraph<StreetNode, StreetEdge> graph)
    {
        Graph = graph;
        StreetNodes = graph.Vertices.ToList();
        StreetEdges = graph.Edges.ToList();
        foreach (var edge in StreetEdges)
        {
            if (edge.Tags.ContainsKey("name")) // parking spots on named streets only
            {
                edge.InitParkingSpots();
            }
        }
    }


    public IMutableBidirectionalGraph<StreetNode, StreetEdge> Graph { get; }
    public IReadOnlyList<StreetNode> StreetNodes { get; }
    public IReadOnlyList<StreetEdge> StreetEdges { get; }
}