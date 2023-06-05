using ConsoleApp1.pgs;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;

namespace ConsoleApp1.clients;

public class PgsParkerClientBehaviour: ICarClientBehaviour
{
    private bool Logging { get; }
    public PgsParkerClientBehaviour(bool logging)
    {
        Logging = logging;
    }
    
    public void DriveAlongPath(CarData carData)
    {
        new CruiserClientBehaviour(Logging).DriveAlongPath(carData);
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
        carData.ReservedSpot = pathRespone.ReservedParkingSpot;
        carData.ReservedSpot.Reserved = true;
    }

    public void SeekParkingSpot(CarData carData)
    {
        carData.Path = new List<StreetEdge> { carData.World.ParkingSpotMap[carData.ReservedSpot] } ;
        new CruiserClientBehaviour(Logging).DriveAlongPath(carData);
    }

    public async Task<bool> AttemptLocalParking(CarData carData)
    {
        if (carData.Position.DistanceFromSource < carData.ReservedSpot.DistanceFromSource) return false;
        carData.Park(carData.ReservedSpot);
        return true;
    }

    public bool StayParked(CarData carData)
    {
        return new RandomParkerClientBehaviour(Logging).StayParked(carData);
    }
    
    public ICarClientBehaviour TogglePgs()
    {
        return new RandomParkerClientBehaviour(Logging);
    }
    
}