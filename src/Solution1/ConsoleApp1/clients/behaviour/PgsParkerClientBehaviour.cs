using ConsoleApp1.pgs;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;

namespace ConsoleApp1.clients;

public class PgsParkerClientBehaviour: ICarClientBehaviour
{
    public void DriveAlongPath(MockCar car)
    {
        new CruiserClientBehaviour().DriveAlongPath(car);
    }

    public void UpdateDestination(MockCar car)
    { 
        // call to calculate path to destination
        car.Destination = car.World.StreetNodes.RandomElement();
        if (!car.TryUpdatePath())
        {
            car.Status = CarStatus.PathingFailed;
            return;
        }
        
        PathResponse? pathRespone = car.Pgs.RequestGuidanceFromServer(car.Position, car.Destination);
        if (pathRespone is null)
        {
            // TODO: this generates new dest, implement behaviour in case of not finding parking spot
            car.Status = CarStatus.PathingFailed;
            return;
        }
        
        car.Path = pathRespone.PathToReservedParkingSpot;
        car.Destination = car.World.ParkingSpotMap[pathRespone.ReservedParkingSpot].Target;
        
        // reserve spot
        car.OccupiedSpot = pathRespone.ReservedParkingSpot;
        car.OccupiedSpot.Occupied = true;
    }

    public void SeekParkingSpot(MockCar car)
    {
        car.Path = new List<StreetEdge> { car.World.ParkingSpotMap[car.OccupiedSpot] } ;
    }

    public async Task<bool> AttemptLocalParking(MockCar car)
    {
        if (car.Position.DistanceFromSource < car.OccupiedSpot.DistanceFromSource) return false;
        car.Park();
        return true;
    }

    public bool StayParked(MockCar car)
    {
        return new RandomParkerClientBehaviour().StayParked(car);
    }
}