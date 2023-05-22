using ConsoleApp1.sim.graph;
using QuickGraph;

namespace ConsoleApp1.sim;

public class PhysicalWorld
{
    public PhysicalWorld(IMutableBidirectionalGraph<StreetNode, StreetEdge> graph, int parkerCount, int cruiserCount)
    {
        Graph = graph;
        StreetNodes = graph.Vertices.ToList();
        StreetEdges = graph.Edges.ToList();
        
        StreetEdges
            .Where(edge => edge.Tags.ContainsKey("name"))
            .ToList()
            .ForEach(edge =>
            {
                edge.InitParkingSpots(ParkingSpotMap);
                UnoccupiedSpotCount += edge.UnoccupiedSpotCount;
            });

        // sim data 
        CruiserCount = cruiserCount;
        ParkerCount = parkerCount;
        CarCount = cruiserCount + parkerCount;
        ParkingSpotCount = ParkingSpotMap.Count;
        ParkEvents = 0;

    }

    public int UnoccupiedSpotCount { get; set; }
    
    public void IncrementParkEvents()
    {
        lock (this)
        {
            ParkEvents++;
        }
    }

    public int ParkEvents { get; set; }

    public void IncrementUnoccupiedSpotCount()
    {
        lock (this)
        {
            UnoccupiedSpotCount++;
        }
    }

    public void DecrementUnoccupiedSpotCount()
    {
        lock (this)
        {
            UnoccupiedSpotCount--;
        }
    }

    public int ParkingSpotCount { get; set; }

    public int CarCount { get; set; }

    public int ParkerCount { get; set; }

    public int CruiserCount { get; set; }
    

    public IMutableBidirectionalGraph<StreetNode, StreetEdge> Graph { get; }
    public IReadOnlyList<StreetNode> StreetNodes { get; }
    public IReadOnlyList<StreetEdge> StreetEdges { get; }
    
    public Dictionary<ParkingSpot, StreetEdge> ParkingSpotMap { get; } = new();
}