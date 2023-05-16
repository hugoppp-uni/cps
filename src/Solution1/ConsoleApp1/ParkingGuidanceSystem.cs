using QuickGraph.Algorithms;

namespace ConsoleApp1;

public record PathResponse(IEnumerable<StreetEdge> PathToReservedParkingSpot, ParkingSpot ReservedParkingSpot);

public class ParkingGuidanceSystem 
{
    public PhysicalWorld World { get; set; }
    public bool Logging { get; set; }
    
    public ParkingGuidanceSystem(PhysicalWorld physicalWorld, bool logging)
    {
        Logging = logging;
        World = physicalWorld;
    }

    public PathResponse RequestGuidanceFromServer(StreetPosition position, StreetNode destination)
    {
        // todo find nearest parking spot to dest
        var outEdges = World.Graph.OutEdges(destination);
        var inEdges = World.Graph.InEdges(destination);

        var adjacentEdges = outEdges.Concat(inEdges);
        // todo handle firstordefault
        var edgeWithUnoccupied = adjacentEdges.FirstOrDefault(edge => edge.ParkingSpots.Any(spot => !spot.Occupied));
        var freeSpot = edgeWithUnoccupied.ParkingSpots.FirstOrDefault(spot => !spot.Occupied);
        freeSpot.Occupied = true;
        
        // todo with traffic congestion heuristic
        var shortestPaths = World.Graph.ShortestPathsDijkstra(
            edge => 100 - edge.SpeedLimit,
            position.StreetEdge.Source);
    
        if (shortestPaths.Invoke(edgeWithUnoccupied.Source, out var path))
        {
            return new PathResponse(path.Append(edgeWithUnoccupied).ToList(), freeSpot);
        }
        else
        {
            // Path = Enumerable.Empty<StreetEdge>();
        }
        
        // find path to parking spot with traffic congestion heuristic
        // return path k
        return null;
    }
    
}