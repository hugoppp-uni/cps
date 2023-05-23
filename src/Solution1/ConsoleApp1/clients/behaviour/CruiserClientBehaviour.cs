using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;
using QuickGraph.Algorithms;

namespace ConsoleApp1.clients;

public class CruiserClientBehaviour: ICarClientBehaviour
{
    public void DriveAlongPath(MockCar car)
    {
        if (car.Position.DistanceFromSource < car.Position.StreetEdge.Length) // driving on street
        {
            // update position
            double speed = car.Position.StreetEdge.CurrentMaxSpeed();
            car.Position = new StreetPosition(car.Position.StreetEdge, car.Position.DistanceFromSource + MathUtil.KmhToMs(speed));
            
            if(car.Logging)
                Console.WriteLine($"{car}\ttick | {car.Position.ToString()} | dest: {car.Destination.Id} | car count: {car.Position.StreetEdge.CarCount} | driving at {speed:F2}kmh/{car.Position.StreetEdge.SpeedLimit:F2}kmh");
        }
        else
        {
            // turn on next street
            car.Turn(car.Path.First());
            if(car.Logging)
                Console.WriteLine($"{car}\ttick | {car.Position.ToString()} | dest: {car.Destination.Id} | car count: {car.Position.StreetEdge.CarCount} | driving at {car.Position.StreetEdge.CurrentMaxSpeed():F2}kmh/{car.Position.StreetEdge.SpeedLimit:F2}kmh");
        }
    }
    
    public void UpdateDestination(MockCar car)
    {
        car.Destination = car.World.StreetNodes.RandomElement();
        
        // update path
        var shortestPaths = car.World.Graph.ShortestPathsDijkstra(
            edge => 100 - edge.SpeedLimit,
            car.Position.StreetEdge.Source);
    
        if (shortestPaths.Invoke(car.Destination, out var path))
        {
            car.Path = path.ToList();
        }
        else
        {
            car.Path = Enumerable.Empty<StreetEdge>();
            car.Status = CarStatus.PathingFailed;
        }
    }

    public void SeekParkingSpot(MockCar car)
    {
        car.Status = CarStatus.Driving;
        UpdateDestination(car);
    }

    public bool AttemptLocalParking(MockCar car)
    {
        throw new NotImplementedException("Cruisers don't park");
    }

    public bool StayParked(MockCar car)
    {
        throw new NotImplementedException("Cruisers don't park.");
    }
}