using ConsoleApp1.sim;

namespace ConsoleApp1.clients;

public interface ICarClientBehaviour
{
    public void DriveAlongPath(CarData carData);
    public void UpdateDestination(CarData carData);
    public void SeekParkingSpot(CarData carData);
    public Task<bool> AttemptLocalParking(CarData carData);
    public bool StayParked(CarData carData);
    public ICarClientBehaviour TogglePgs();
}
