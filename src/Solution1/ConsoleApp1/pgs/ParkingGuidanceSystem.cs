using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using QuickGraph;
using QuickGraph.Algorithms;

namespace ConsoleApp1.pgs;

public class ParkingGuidanceSystem 
{
    public PhysicalWorld World { get; set; }
    public bool Logging { get; set; }

    private IParkingStrategy _parkingStrategy;

    private Func<StreetEdge, double> _speedLimitEdgeWeights = edge => 100 - edge.SpeedLimit;
    private Func<StreetEdge, double> _congestionEdgeWeights = edge => 1 / edge.CurrentCoverDuration(); 
    private Func<StreetEdge, double> _searchEdgeWeights;
    
    public ParkingGuidanceSystem(PhysicalWorld physicalWorld, IParkingStrategy parkingStrategy,
        bool logging)
    {
        Logging = logging;
        World = physicalWorld;
        
        _parkingStrategy = parkingStrategy;
        _searchEdgeWeights = _congestionEdgeWeights;
    }

    public PathResponse RequestGuidanceFromServer(StreetPosition position, StreetNode destination)
    {
        ParkingSpot freeSpot = _parkingStrategy.FindParkingSpot(World, destination);
        if (freeSpot is null) // no free parking spot found
        {
            return null!; 
        }
        
        // get edge with free spot from map
        if (World.ParkingSpotMap.TryGetValue(freeSpot, out var edgeWithFreeSpot))
        {
            // occupy free spot
            freeSpot.Occupied = true;
            
            // path to free spot
            var shortestPaths = World.Graph.ShortestPathsDijkstra(
                _searchEdgeWeights,
                position.StreetEdge.Source);
        
            if (shortestPaths.Invoke(edgeWithFreeSpot.Source, out var path))
            {
                return new PathResponse(path.Append(edgeWithFreeSpot).ToList(), freeSpot);
            }

            // pathing failed 
            return null!; 
        }

        // edge not in the map
        throw new KeyNotFoundException("Incoherent parking spot map");
    }
    
}