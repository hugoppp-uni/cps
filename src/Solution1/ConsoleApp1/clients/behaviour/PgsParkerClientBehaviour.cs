namespace ConsoleApp1.clients;

public class PgsParkerClientBehaviour: ICarClientBehaviour
{
    public void DriveAlongPath(MockCar car)
    {
        new CruiserClientBehaviour().DriveAlongPath(car);
    }

    public void UpdateDestination(MockCar car)
    {
        // TODO
        
        throw new NotImplementedException();
    }

    public void SeekParkingSpot(MockCar car)
    {
        // TODO
        
        throw new NotImplementedException();
    }

    public bool AttemptLocalParking(MockCar car)
    {
        // TODO 
        
        throw new NotImplementedException();
    }

    public bool StayParked(MockCar car)
    {
        return new RandomParkerClientBehaviour().StayParked(car);
    }
}