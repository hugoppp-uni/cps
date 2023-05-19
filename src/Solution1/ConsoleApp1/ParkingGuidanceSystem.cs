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
        var edgeWithUnoccupied = adjacentEdges.FirstOrDefault(edge => edge.ParkingSpots.Any(spot => !spot.Occupied));
        if (edgeWithUnoccupied == null) return null!; // TODO: in this case breadth search would continue 
        var freeSpot = edgeWithUnoccupied.ParkingSpots.FirstOrDefault(spot => !spot.Occupied); // TODO: handle null
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
        var freeSpot = _parkingStrategy.FindParkingSpot(World, destination);
        if (freeSpot == null) return null!; // TODO: in this case there is no free spot in the graph
        StreetEdge edgeWithUnoccupied = World.ParkingSpotMap.GetValueOrDefault(freeSpot);
        if (edgeWithUnoccupied == null) return null!; //TODO: edge is not in the map
        freeSpot.Occupied = true;
        
        // TODO: find path with traffic congestion heuristic
        var shortestPaths = World.Graph.ShortestPathsDijkstra(
            _searchEdgeWeights,
            position.StreetEdge.Source);
    
        if (shortestPaths.Invoke(edgeWithUnoccupied.Source, out var path))
        {
            return new PathResponse(path.Append(edgeWithUnoccupied).ToList(), freeSpot);
        }
        else
        {
            // Path = Enumerable.Empty<StreetEdge>();
            return null!; // TODO: in this case there is no pathing to the found spot
        }
        
        // find path to parking spot with traffic congestion heuristic
        // return path k
        return null!;
    }
    
}