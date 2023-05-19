using ConsoleApp1.sim.graph;
using QuickGraph;

namespace ConsoleApp1.sim;

public class PhysicalWorld
{
    public PhysicalWorld(IMutableBidirectionalGraph<StreetNode, StreetEdge> graph)
    {
        Graph = graph;
        StreetNodes = graph.Vertices.ToList();
        StreetEdges = graph.Edges.ToList();
        
        StreetEdges
            .Where(edge => edge.Tags.ContainsKey("name"))
            .ToList()
            .ForEach(edge => edge.InitParkingSpots(ParkingSpotMap));
    }

    public IMutableBidirectionalGraph<StreetNode, StreetEdge> Graph { get; }
    public IReadOnlyList<StreetNode> StreetNodes { get; }
    public IReadOnlyList<StreetEdge> StreetEdges { get; }
    
    public Dictionary<ParkingSpot, StreetEdge> ParkingSpotMap { get; } = new();
}