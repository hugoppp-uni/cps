using MQTTnet.Client;

namespace ConsoleApp1.clients;

public class RogueParkerBehaviour: ICarClientBehaviour
{
    public bool Logging { get; }
    private bool ConsiderReservation { get; set; }

    public RogueParkerBehaviour(bool logging)
    {
        ConsiderReservation = true;
        Logging = logging;
    }

    public void DriveAlongPath(CarData carData)
    {
        new RandomParkerClientBehaviour(Logging).DriveAlongPath(carData);
    }

    public void UpdateDestination(CarData carData)
    {
        new RandomParkerClientBehaviour(Logging).UpdateDestination(carData);
    }

    public void SeekParkingSpot(CarData carData)
    {
        new RandomParkerClientBehaviour(Logging).SeekParkingSpot(carData);
    }

    public Task<bool> AttemptLocalParking(CarData carData, bool considerReservation = true)
    {
        return new RandomParkerClientBehaviour(Logging).AttemptLocalParking(carData, ConsiderReservation);
    }

    public bool StayParked(CarData carData)
    {
        return new RandomParkerClientBehaviour(Logging).StayParked(carData);
    }

    public ICarClientBehaviour SetPgs(bool pgsOn)
    {
        // rogue parkers don't change
        return new RogueParkerBehaviour(Logging);
    }
    
    public async Task PublishAll(CarData carData, IMqttClient mqttClient)
    {
        // rogue parkers don't publish data
    }
    
    public void SetRogue(bool rogueOn)
    {
        ConsiderReservation = rogueOn;
    }
}