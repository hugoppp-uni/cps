using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;

namespace ConsoleApp1.clients;

public class MockCar
{
    private int Id { get; }
    public PhysicalWorld World { get; }
    public StreetPosition Position { get; set; }
    public IEnumerable<StreetEdge> Path { get; set; }
    public StreetNode Destination { get; set; }

    public ParkingSpot OccupiedSpot { get; set; } = null!;
    public CarStatus Status { get; set; }
    public bool Logging { get; set; }
    public const int MaxParkTime = 500;
    public int ParkTime { get; set; }

    public override string ToString() => $"[CAR\t{Id},\t{Status}\t]";
    public MockCar(int id, PhysicalWorld world, KpiManager kpiManager, bool logging)
    {
        Id = id;
        World = world;
        Path = Enumerable.Empty<StreetEdge>();
        Logging = logging;
        KpiManager = kpiManager;
        
        // init position
        Position = StreetPosition.WithRandomDistance(world.StreetEdges.RandomElement());
        Position.StreetEdge.IncrementCarCount();
    }
    
    public KpiManager KpiManager { get; set; }
    
    public bool DestinationReached()
    {
        return !Path.Any() && Destination == Position.StreetEdge.Target &&
               Position.DistanceFromSource >= Position.StreetEdge.Length;
    }
    
    public bool TargetNodeReached()
    {
        return Position.DistanceFromSource >= Position.StreetEdge.Length;
    }
    
    public void RespawnAtRandom()
    {
        lock (Position.StreetEdge)
        {
            var previousPosition = Position;
            Position = StreetPosition.WithRandomDistance(World.StreetEdges.RandomElement());
            lock (Position.StreetEdge)
            {
                previousPosition.StreetEdge.DecrementCarCount();
                Position.StreetEdge.IncrementCarCount();
            }
        }
    }

    public void ResetAfterParking()
    {
        lock (Position.StreetEdge)
        {
            OccupiedSpot.Occupied = false;
            Position.StreetEdge.IncrementCarCount();
        }

        // kpi
        KpiManager.Reset();

        // diagnostics
        World.IncrementUnoccupiedSpotCount();
    }

    public void Turn(StreetEdge next)
    {
        lock (Position.StreetEdge)
        {
            var overlap = Position.DistanceFromSource - Position.StreetEdge.Length;
            var previousPosition = Position;
            Position = new StreetPosition(next, overlap);
            lock (Position.StreetEdge)
            {
                previousPosition.StreetEdge.DecrementCarCount();
                Position.StreetEdge.IncrementCarCount();
            }
            Path = Path.Skip(1);
        }
    }
}