using ConsoleApp1.sim;

namespace ConsoleApp1.clients;

public interface ICarClientBehaviour
{
    public void DriveAlongPath(MockCar car);
    public void UpdateDestination(MockCar car);
    public void SeekParkingSpot(MockCar car);
    public Task<bool> AttemptLocalParking(MockCar car);
    public bool StayParked(MockCar car);

}