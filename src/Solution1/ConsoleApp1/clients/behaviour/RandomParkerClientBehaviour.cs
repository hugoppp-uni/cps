using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;

namespace ConsoleApp1.clients;

public class RandomParkerClientBehaviour: ICarClientBehaviour
{
    public void DriveAlongPath(MockCar car)
    {
        new CruiserClientBehaviour().DriveAlongPath(car);
    }

    public void UpdateDestination(MockCar car)
    {
        new CruiserClientBehaviour().UpdateDestination(car);
    }

    public void SeekParkingSpot(MockCar car)
    {
        if (car.TargetNodeReached())
        {
            // update kpis upon reached node
            car.KpiManager.DistanceTravelledParking += car.Position.StreetEdge.Length;
            double speedReductionP = 100 - ((car.Position.StreetEdge.CurrentMaxSpeed() / car.Position.StreetEdge.SpeedLimit) * 100);
            car.KpiManager.SpeedReductionSum += speedReductionP;
            car.KpiManager.SpeedReductionCount++;
            
            // turn on next random street to look for parking
            StreetEdge nextStreet;
            if (car.World.Graph.TryGetOutEdges(car.Position.StreetEdge.Target, out var outGoingStreets)) // TODO possible parking spot search heuristic 
            {
                nextStreet = outGoingStreets.ToList().RandomElement();
                car.Path = new List<StreetEdge> { nextStreet } ;
            }
        }
    }

    public bool AttemptLocalParking(MockCar car)
    {
        var previousPosition = car.Position.DistanceFromSource - MathUtil.KmhToMs(car.Position.StreetEdge.CurrentMaxSpeed());
        Stack<ParkingSpot> passedParkingSpots = new Stack<ParkingSpot>(
            car.Position.StreetEdge.ParkingSpots
                .Where(spot => spot.DistanceFromSource >= previousPosition && spot.DistanceFromSource <= car.Position.DistanceFromSource)
                .Reverse());

        while (passedParkingSpots.Count != 0 && passedParkingSpots.Peek().Occupied)
        {
            passedParkingSpots.Pop();
        }

        if (passedParkingSpots.Count == 0) return false;
        var nearestUnoccupiedSpot = passedParkingSpots.Peek();
        
        // physically park
        car.Position = new StreetPosition(car.Position.StreetEdge, nearestUnoccupiedSpot.DistanceFromSource);
        
        nearestUnoccupiedSpot.Occupied = true;
        car.Position.StreetEdge.DecrementCarCount();
        Random rand = new Random();
        car.ParkTime = rand.Next(0, MockCar.MaxParkTime + 1);
        
        car.World.DecrementUnoccupiedSpotCount();
        car.KpiManager.DistanceTravelledParking += car.Position.DistanceFromSource;
            
        //car.KpiManager.Publish(); // TODO this makes all cars stuck in parking
        return true;
    }

    public bool StayParked(MockCar car)
    {
        if(car.Logging)
            Console.WriteLine($"{car}\ttick | Parked at {car.Position} | {car.ParkTime} ticks remaining");
        if (car.ParkTime == 0)
        {
            car.ResetAfterParking();
        }
        //car.ParkTime--;
        return car.ParkTime > 0;
    }
    
}