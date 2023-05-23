using ConsoleApp1.pgs;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;

namespace ConsoleApp1.clients;

public class PgsParkerClientBehaviour: ICarClientBehaviour
{
    public void DriveAlongPath(CarData carData)
    {
        new CruiserClientBehaviour().DriveAlongPath(carData);
    }

    public void UpdateDestination(CarData carData)
    { 
        // call to calculate path to destination
        carData.Destination = carData.World.StreetNodes.RandomElement();
        if (!carData.TryUpdatePath())
        {
            carData.Status = CarStatus.PathingFailed;
            return;
        }
        
        PathResponse? pathRespone = carData.Pgs.RequestGuidanceFromServer(carData.Position, carData.Destination);
        if (pathRespone is null)
        {
            // TODO: this generates new dest, implement behaviour in case of not finding parking spot
            carData.Status = CarStatus.PathingFailed;
            return;
        }
        
        carData.Path = pathRespone.PathToReservedParkingSpot;
        carData.Destination = carData.World.ParkingSpotMap[pathRespone.ReservedParkingSpot].Target;
        
        // reserve spot
        carData.OccupiedSpot = pathRespone.ReservedParkingSpot;
        carData.OccupiedSpot.Occupied = true;
    }

    public void SeekParkingSpot(CarData carData)
    {
        carData.Path = new List<StreetEdge> { carData.World.ParkingSpotMap[carData.OccupiedSpot] } ;
    }

    public async Task<bool> AttemptLocalParking(CarData carData)
    {
        if (carData.Position.DistanceFromSource < carData.OccupiedSpot.DistanceFromSource) return false;
        carData.Park();
        return true;
    }

    public bool StayParked(CarData carData)
    {
        return new RandomParkerClientBehaviour().StayParked(carData);
    }
}