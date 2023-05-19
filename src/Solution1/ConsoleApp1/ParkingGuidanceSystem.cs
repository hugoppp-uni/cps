using QuickGraph;
using QuickGraph.Algorithms;

namespace ConsoleApp1;

public record PathResponse(IEnumerable<StreetEdge> PathToReservedParkingSpot, ParkingSpot ReservedParkingSpot);

public interface IParkingStrategy
{
    public ParkingSpot FindParkingSpot(PhysicalWorld world, StreetNode destination);
}

public class NearestParkingStrategy: IParkingStrategy
{
    public ParkingSpot FindParkingSpot(PhysicalWorld world, StreetNode destination)
    {
        // TODO: algorithm
        var outEdges = world.Graph.OutEdges(destination);
        var inEdges = world.Graph.InEdges(destination);
        var adjacentEdges = outEdges.Concat(inEdges);

        var freeSpot = adjacentEdges
            .FirstOrDefault(edge => edge.ParkingSpots.Any(spot => !spot.Occupied))?
            .ParkingSpots.FirstOrDefault(spot => !spot.Occupied);

        if (freeSpot is null)
        {
            return null!;
        }
        
        return freeSpot;
    }
}

public class ParkingGuidanceSystem 
{
    public PhysicalWorld World { get; set; }
    public bool Logging { get; set; }

    private IParkingStrategy _parkingStrategy = new NearestParkingStrategy();

    private Func<StreetEdge, double> _searchEdgeWeights = edge => 100 - edge.SpeedLimit;
    
    public ParkingGuidanceSystem(PhysicalWorld physicalWorld, bool logging)
    {
        Logging = logging;
        World = physicalWorld;
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