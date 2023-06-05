using ConsoleApp1.sim.graph;
using ConsoleApp1.util;
using QuickGraph;

namespace ConsoleApp1.sim;

public class PhysicalWorld
{
    public PhysicalWorld(IMutableBidirectionalGraph<StreetNode, StreetEdge> graph, int parkingSpotCount, int parkerCount, int cruiserCount,
        int rogueParkerCount)
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
            });
        InitialSpotCount = GetUnoccupiedSpotsCount();

        // sim data 
        CruiserCount = cruiserCount;
        ParkerCount = parkerCount;
        RogueParkerCount = rogueParkerCount;
        CarCount = cruiserCount + parkerCount;
        MaxParkingSpots = ParkingSpotMap.Count;
        ParkEvents = 0;

    }

    public int InitialSpotCount { get; set; }
    
    public void IncrementParkEvents()
    {
        lock (this)
        {
            ParkEvents++;
        }
    }

    public int ParkEvents { get; set; }

    public int MaxParkingSpots { get; set; }

    public int CarCount { get; set; }

    public int ParkerCount { get; set; }
    public int RogueParkerCount { get; set; }
    public int CruiserCount { get; set; }
    

    public IMutableBidirectionalGraph<StreetNode, StreetEdge> Graph { get; }
    public IReadOnlyList<StreetNode> StreetNodes { get; }
    public IReadOnlyList<StreetEdge> StreetEdges { get; }
    
    public Dictionary<ParkingSpot, StreetEdge> ParkingSpotMap { get; } = new();

    public int GetUnoccupiedSpotsCount()
    {
        int spotCount = 0;
    
        foreach (var streetEdge in StreetEdges)
        {
            spotCount += streetEdge.ParkingSpots.Count(spot => !spot.Occupied);
        }
    
        return spotCount;
    }

    public ParkingSpot? GetRandomUnoccupied()
    {
        List<ParkingSpot> unoccupiedParkingSpots = StreetEdges
            .SelectMany(edge => edge.ParkingSpots)
            .Where(spot => !spot.Occupied)
            .ToList();

        if (unoccupiedParkingSpots.Count > 0)
        {
            return unoccupiedParkingSpots.RandomElement();
        }
        return null;
    }

}