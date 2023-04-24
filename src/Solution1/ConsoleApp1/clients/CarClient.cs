using System.Drawing;
using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using QuickGraph;
using QuickGraph.Algorithms;

namespace ConsoleApp1;

struct StreetPosition
{
    public StreetPosition(StreetEdge streetEdge, double distanceFromSource)
    {
        StreetEdge = streetEdge;
        DistanceFromSource = distanceFromSource;
    }

    public static StreetPosition WithRandomDistance(StreetEdge streetEdge)
    {
        return new StreetPosition(streetEdge, Random.Shared.NextDouble() * streetEdge.Length);
    }

    public override string ToString() => $"{StreetEdge.StreetName} ({DistanceFromSource:F1}m / {StreetEdge.Length:F1}m) | [src: {StreetEdge.Source.Id}, tgt: {StreetEdge.Target.Id}])";

    public StreetEdge StreetEdge { get; }
    public double DistanceFromSource { get; set; }
}

enum CarClientStatus
{
    DRIVING,
    PARKING,
    PARKED,
    PATHING_FAILED
}

public class CarClient : BaseClient
{
    private int Id { get; }
    private PhysicalWorld PhysicalWorld { get; }
    private StreetPosition Position { get; set; }
    private IEnumerable<StreetEdge> Path { get; set; }
    private StreetNode Destination { get; set; }

    private CarClientStatus Status { get; set; }
    
    private ParkingSpot LastParkingSpotPassed { get; set; }

    public override string ToString() => $"[CAR {Id}, {Status}]";

    private CarClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int id) : base(mqttClient)
    {
        Id = id;
        PhysicalWorld = physicalWorld;
        Status = CarClientStatus.DRIVING;
        Position = StreetPosition.WithRandomDistance(physicalWorld.StreetEdges.RandomElement());
        Console.WriteLine($"{this}\tinitial position: {Position.ToString()}");
        
        UpdateDestination();
    }
    
    /**
     * Main CarClient behaviour
     */
    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        switch (Status)
        {
            case CarClientStatus.PATHING_FAILED: // pathing failed, new dest needed
                Status = CarClientStatus.DRIVING;
                UpdateDestination();
                break;

            case CarClientStatus.PARKED: // car parked
                // TODO parked behaviour
                Console.WriteLine($"{this}\tParked at {Position}");
                break;

            case CarClientStatus.PARKING: // car looking for parking
                KeepDriving();
                if (Position.DistanceFromSource >= Position.StreetEdge.Length)
                {
                    NextStreetToLookForParking();
                }
                else
                {
                    TryParkingLocally();
                }
                break;

            case CarClientStatus.DRIVING:
                if (DestinationReached()) // car reached destination after driving
                {
                    InitLookingForParking();
                }
                else // car driving to destination
                {
                    KeepDriving();
                }
                break;
        }
    }


    private void UpdateDestination()
    {
        Destination = PhysicalWorld.StreetNodes.RandomElement();
        Console.WriteLine($"{this}\tupdated destination {Destination.Id}");
        UpdatePath();
    }

    private void InitLookingForParking()
    {
        Status = CarClientStatus.PARKING;
        IEnumerable<StreetEdge> outEdges;
        if (PhysicalWorld.Graph.TryGetOutEdges(Destination, out outEdges))
        {
            var ran = outEdges.ToList().RandomElement();
            Path = new List<StreetEdge>{ran} ;
            LastParkingSpotPassed = ran.ParkingSpots[0];
            Console.WriteLine($"{this}\tLooking for parking on {ran.StreetName}");
            TryParkingLocally();
        }
    }

    private bool DestinationReached()
    {
        return !Path.Any() && Destination == Position.StreetEdge.Target && Position.DistanceFromSource >= Position.StreetEdge.Length;
    }

    private void KeepDriving()
    {
        if (Position.DistanceFromSource < Position.StreetEdge.Length) // driving on street
        {
            // 1 tick = 1 second 
            Position = new StreetPosition(Position.StreetEdge, Position.DistanceFromSource + MathUtil.KmhToMs(Position.StreetEdge.CurrentMaxSpeed()));
            Console.WriteLine($"{this}\ttick | {Position.ToString()}");
        }
        else // node reached
        {
            var overlap = Position.DistanceFromSource - Position.StreetEdge.Length;
            Position = new StreetPosition(Path.First(), overlap);
            Path = Path.Skip(1);
            Console.WriteLine($"{this}\ttick | {Position.ToString()}");
        }
        //await PublishPosition(); 
    }

    private void TryParkingLocally()
    {
        int smallestIndexUncheckedSpot = LastParkingSpotPassed.Index;
        LastParkingSpotPassed = Position.StreetEdge.ParkingSpots[(int)Math.Floor(Position.DistanceFromSource / (ParkingSpot.Length + Position.StreetEdge.ParkingSpotSpacing))];
        int count = LastParkingSpotPassed.Index - smallestIndexUncheckedSpot;
            
        var checkForOccupancy = Position.StreetEdge.ParkingSpots.GetRange(smallestIndexUncheckedSpot, count + 1);

        ParkingSpot availableSpot = checkForOccupancy.LastOrDefault(ps => !ps.Occupied);
        if (availableSpot != null)
        {
            Console.WriteLine($"{this}\tAvailable spot at {availableSpot.DistanceFromSource} on {Position.StreetEdge.StreetName}");
            Position = new StreetPosition(Position.StreetEdge, availableSpot.DistanceFromSource);
            Status = CarClientStatus.PARKED;
        }
    }

    private IEnumerable<StreetEdge> GetOutGoingStreets(StreetNode node)
    {
        IEnumerable<StreetEdge> outEdges;
        if (PhysicalWorld.Graph.TryGetOutEdges(Position.StreetEdge.Target, out outEdges))
        {
            return outEdges;
        }
        return null!;
    }

    // TODO currently looking for parking while driving around randomly
    public void NextStreetToLookForParking()
    {
        StreetEdge nextStreet;
        if (DestinationReached())
        {
            // look for parking at random outgoing street from destination
            var outGoingStreets = GetOutGoingStreets(Destination);
            nextStreet = outGoingStreets.ToList().RandomElement();
        }
        else
        {
            // TODO this can be replaced with searching for parking algorithmically
            var outGoingStreets = GetOutGoingStreets(Position.StreetEdge.Target);
            nextStreet = outGoingStreets.ToList().RandomElement();
        }
        Path = new List<StreetEdge>{nextStreet} ;
        LastParkingSpotPassed = nextStreet.ParkingSpots[0];
        TryParkingLocally();
        Console.WriteLine($"{this}\tLooking for parking on {nextStreet.StreetName}");
    }

    private void UpdatePath()
    {
        // compute shortest path to destination
        if (PhysicalWorld.Graph.ShortestPathsDijkstra(edge => 100 - edge.SpeedLimit, Position.StreetEdge.Source)
            .Invoke(Destination, out var path))
        {
            Path = path;
            Console.WriteLine($"{this}\tpath: {string.Join(',', path.Select(p => p.StreetName))}");
        }
        else // path finding failed, i.e. when graph is not fully connected
        {
            Console.WriteLine($"{this}\tno path found [destination: {Destination.Id}, position: {Position.ToString()}, node: {Position.StreetEdge.Source.Id}]");
            // empty path
            Path = Enumerable.Empty<StreetEdge>();
            Status = CarClientStatus.PATHING_FAILED;
        }
    }
    
    // TODO publish car position 
    private async Task PublishPosition()
    {
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { CarId = Id, Position }));
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "position", Payload = payload });
    }


    public static async Task<CarClient> Create(MqttClientFactory clientFactory, int id,
        PhysicalWorld physicalWorld)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tick"));
        return new CarClient(client, physicalWorld, id);
    }
}