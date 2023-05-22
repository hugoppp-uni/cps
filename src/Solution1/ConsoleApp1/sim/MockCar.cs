﻿using ConsoleApp1.sim;
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
        Position.StreetEdge.DecrementCarCount();
        Position = StreetPosition.WithRandomDistance(World.StreetEdges.RandomElement());
        Position.StreetEdge.IncrementCarCount();
    }

    public void ResetAfterParking()
    {
        OccupiedSpot.Occupied = false;
        OccupiedSpot = null!;
        Position.StreetEdge.IncrementCarCount();

        // kpi
        KpiManager.Reset();

        // diagnostics
        World.IncrementUnoccupiedSpotCount();
    }
}