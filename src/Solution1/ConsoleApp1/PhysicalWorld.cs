using QuickGraph;

namespace ConsoleApp1;

// TODO needs to be thread safe
public class PhysicalWorld
{
    public PhysicalWorld(IMutableBidirectionalGraph<StreetNode, StreetEdge> graph)
    {
        Graph = graph;
        StreetNodes = graph.Vertices.ToList();
        //foreach (var (node, index) in StreetNodes.Select((node, index) => (node, index)))
        //{
            //node.Number = index;
        //}

        
        StreetEdges = graph.Edges.ToList();
    }


    public IMutableBidirectionalGraph<StreetNode, StreetEdge> Graph { get; }
    public IReadOnlyList<StreetNode> StreetNodes { get; }
    public IReadOnlyList<StreetEdge> StreetEdges { get; }
}