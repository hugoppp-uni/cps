using ConsoleApp1.sim;
using MQTTnet.Client;

namespace ConsoleApp1.clients;

public interface ICarClientBehaviour
{
    public void DriveAlongPath(CarData carData);
    public void UpdateDestination(CarData carData);
    public void SeekParkingSpot(CarData carData);
    public Task<bool> AttemptLocalParking(CarData carData, bool considerReservation = true);
    public bool StayParked(CarData carData);
    public ICarClientBehaviour SetPgs(bool pgsOn);
    public Task PublishAll(CarData carData, IMqttClient mqttClient);
    public void SetRogue(bool rogueOn);
}
