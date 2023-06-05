using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;
using QuickGraph.Algorithms;

namespace ConsoleApp1.clients;

public class CruiserClientBehaviour: ICarClientBehaviour
{
    private bool Logging { get; }
    public CruiserClientBehaviour(bool logging)
    {
        Logging = logging;
    }
    
    public void DriveAlongPath(CarData carData)
    {
        if (carData.Position.DistanceFromSource < carData.Position.StreetEdge.Length) // driving on street
        {
            // update position
            double speed = carData.Position.StreetEdge.CurrentMaxSpeedMs();
            carData.Position = new StreetPosition(carData.Position.StreetEdge, carData.Position.DistanceFromSource + speed);

            if (Logging)
            {
                Console.WriteLine($"{carData}\ttick | {carData.Position.ToString()} | dest: {carData.Destination.Id} | car count: {carData.Position.StreetEdge.CarCount} | driving at {MathUtil.MsToKmh(speed):F2}kmh/{MathUtil.MsToKmh(carData.Position.StreetEdge.SpeedLimitMs):F2}kmh");
            }
        }
        else
        {
            // turn on next street
            carData.DistanceTravelled += carData.Position.StreetEdge.Length;
            carData.Turn(carData.Path.First());
            if (Logging)
            {
                Console.WriteLine($"{carData}\ttick | {carData.Position.ToString()} | dest: {carData.Destination.Id} | car count: {carData.Position.StreetEdge.CarCount} | driving at {MathUtil.MsToKmh(carData.Position.StreetEdge.CurrentMaxSpeedMs()):F2}kmh/{MathUtil.MsToKmh(carData.Position.StreetEdge.SpeedLimitMs):F2}kmh");
            }
        }
    }
    
    public void UpdateDestination(CarData carData)
    {
        carData.Destination = carData.World.StreetNodes.RandomElement();
        if(!carData.TryUpdatePath()) carData.Status = CarStatus.PathingFailed;
    }

    public void SeekParkingSpot(CarData carData)
    {
        carData.Status = CarStatus.Driving;
        UpdateDestination(carData);
    }

    public Task<bool> AttemptLocalParking(CarData carData)
    {
        throw new NotImplementedException("Cruisers don't park");
    }

    public bool StayParked(CarData carData)
    {
        throw new NotImplementedException("Cruisers don't park.");
    }
    
    public ICarClientBehaviour TogglePgs()
    {
        // cruiser clients dont change
        return new CruiserClientBehaviour(Logging);
    }
}