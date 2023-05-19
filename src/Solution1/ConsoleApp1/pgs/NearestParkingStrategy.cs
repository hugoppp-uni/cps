using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;

namespace ConsoleApp1.pgs;

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