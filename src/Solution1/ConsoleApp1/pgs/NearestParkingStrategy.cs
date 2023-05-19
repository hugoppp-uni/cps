using System.Collections;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;

namespace ConsoleApp1.pgs;

/**
 * Breadth-First-Search ordered by DistanceFromSource
 */
public class NearestParkingStrategy: IParkingStrategy
{
    public ParkingSpot FindParkingSpot(PhysicalWorld world, StreetNode destination)
    {
        var visited = new HashSet<StreetEdge>();
        var queue = new Queue<StreetNode>();
        queue.Enqueue(destination);

        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();
            var outEdges = world.Graph.OutEdges(currentNode);
            var inEdges = world.Graph.InEdges(currentNode);
            var adjacentEdges = outEdges.Concat(inEdges);
            
            foreach (var edge in adjacentEdges)
            {
                // filter unoccupied spots and sort by distance from source ascending
                var unoccupiedSpots = edge.ParkingSpots.Where(spot => !spot.Occupied)
                    .OrderBy(spot => spot.DistanceFromSource)
                    .ToList();
                
                // check if unoccupied found
                if (unoccupiedSpots.Count > 0)
                {
                    var freeSpot = unoccupiedSpots[0];
                    return freeSpot;
                }
                
                // check if edge has been visited
                if (!visited.Contains(edge))
                {
                    // enqueue adjacent unvisited node
                    queue.Enqueue(edge.Target);
                    visited.Add(edge);
                }
            }
        }
        
        // no unoccupied spot found
        return null!;
    }
}