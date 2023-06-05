using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;
using MQTTnet.Client;

namespace ConsoleApp1.clients;

public class RandomParkerClientBehaviour: ICarClientBehaviour
{
    
    private bool Logging { get; }
    public RandomParkerClientBehaviour(bool logging)
    {
        Logging = logging;
    }
    
    
    public void DriveAlongPath(CarData carData)
    {
        new CruiserClientBehaviour(Logging).DriveAlongPath(carData);
    }

    public void UpdateDestination(CarData carData)
    {
        new CruiserClientBehaviour(Logging).UpdateDestination(carData);
    }

    public void SeekParkingSpot(CarData carData)
    {
        if (carData.TargetNodeReached())
        {
            // update kpis upon reached node
            carData.UpdateTrafficKpis();
            carData.DistanceTravelled += carData.Position.StreetEdge.Length;
            
            // turn on next random street to look for parking
            StreetEdge nextStreet;
            if (carData.World.Graph.TryGetOutEdges(carData.Position.StreetEdge.Target, out var outGoingStreets)) // TODO possible parking spot search heuristic 
            {
                nextStreet = outGoingStreets.ToList().RandomElement();
                carData.Path = new List<StreetEdge> { nextStreet } ;
            }
        }
        new CruiserClientBehaviour(Logging).DriveAlongPath(carData);
    }

    public async Task<bool> AttemptLocalParking(CarData carData, bool considerReservation = true)
    {
        var previousPosition = carData.Position.DistanceFromSource - MathUtil.KmhToMs(carData.Position.StreetEdge.CurrentMaxSpeedMs());
        Stack<ParkingSpot> passedParkingSpots = new Stack<ParkingSpot>(
            carData.Position.StreetEdge.ParkingSpots
                .Where(spot => spot.DistanceFromSource >= previousPosition && spot.DistanceFromSource <= carData.Position.DistanceFromSource)
                .Reverse());

        while (passedParkingSpots.Count != 0 && (considerReservation ? (passedParkingSpots.Peek().Occupied || passedParkingSpots.Peek().Reserved) : passedParkingSpots.Peek().Occupied))
        {
            passedParkingSpots.Pop();
        }
        
        if (passedParkingSpots.Count == 0) return false;
        var nearestUnoccupiedSpot = passedParkingSpots.Peek();
        
        // physically park
        carData.Park(nearestUnoccupiedSpot);
        return true;
    }

    public bool StayParked(CarData carData)
    {
        if (Logging)
        {
            Console.WriteLine($"{carData}\ttick | Parked at {carData.Position} | {carData.ParkTime} ticks remaining");
        }
        if (carData.ParkTime <= 0)
        {
            carData.ResetAfterParking();
            return false;
        }
        carData.ParkTime--;
        return true;
    }

    public ICarClientBehaviour SetPgs(bool pgsOn)
    {
        return pgsOn ? new PgsParkerClientBehaviour(Logging) : new RandomParkerClientBehaviour(Logging);
    }

    public async Task PublishAll(CarData carData, IMqttClient mqttClient)
    {
        await carData.PublishAll(mqttClient);
    }

    public void SetRogue(bool rogueOn)
    {
        // for rogue parkers only
    }
}