﻿using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ConsoleApp1.pgs;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;
using MQTTnet;
using MQTTnet.Client;
using QuickGraph.Algorithms;

namespace ConsoleApp1.clients;

public class ParkerClientDeprecatedDeprecated: CarClientDeprecated
{
    // traffic metrics
    private double _speedReductionRunningAvg;
    private double _speedReductionSum;
    private long _speedReductionCount;

    private const double FuelConsumptionRate = 6.5;  // liters per 100 km, average according to German Federal Environment Agency (UBA)
    private const int Co2EmissionRate = 131; // grams per km, average according to German Federal Environment Agency (UBA)
    private const int MaxParkTime = 500;
    private double DistanceTravelledParking { get; set; }
    private int ParkTime { get; set; }
    private int TicksSpentParking { get; set; } = 0;
    private int LastOccupiedIndex { get; set; }
    private int LastParkingSpotPassedIndex { get; set; }
    private ParkingSpot? ReservedSpot { get; set; }
    
    private bool SupportsPgs { get; set; }
    public ParkingGuidanceSystem Pgs { get; set; }

    protected ParkerClientDeprecatedDeprecated(IMqttClient mqttClient, PhysicalWorld physicalWorld, ParkingGuidanceSystem pgs, int id,
        bool supportsPgs, bool logging) : base(mqttClient, physicalWorld, id, logging)
    {
        Pgs = pgs;
        SupportsPgs = supportsPgs;
    }

    /*
     * Creation through factory
     */
    public static async Task<CarClientDeprecated> Create(MqttClientFactory clientFactory, int id,
        PhysicalWorld physicalWorld, ParkingGuidanceSystem pgs, bool supportsPgs, bool logging)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tickgen/tick"));
        return new ParkerClientDeprecatedDeprecated(client, physicalWorld, pgs, id, supportsPgs, logging);
    }
    
    /**
     * Main ParkerClient behaviour
     */
    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        switch (Status)
        {
            case CarStatus.PathingFailed: // pathing failed to destination failed and has to be redone
                HandlePathingFailed();
                break;

            case CarStatus.Parked: // car is parked for a random amount of ticks
                HandleParked();
                break;

            case CarStatus.Parking: // car is driving around randomly looking for a available parking spot
                await HandleParking();
                break;

            case CarStatus.Driving: // car is driving according to the shortest path to their random destination
                await HandleDriving();
                break;
            
            default:
                throw new InvalidOperationException($"{this}\tInvalid status: {Status}");
        }
    }
    
    protected async override void UpdateDestination()
    {
        Status = CarStatus.Driving;
        if (SupportsPgs)
        {
            var destination = PhysicalWorld.StreetNodes.RandomElement();
            PathResponse? pathResponse = Pgs.RequestGuidanceFromServer(Position, destination);
            if (pathResponse is null)
            {
                // TODO: this generates a new dest, implement behaviour in case of not finding a parking spot
                Status = CarStatus.PathingFailed;
            }
            else
            {
                Path = pathResponse.PathToReservedParkingSpot;
                ReservedSpot = pathResponse.ReservedParkingSpot;
            }
            
        }
        else
        {
            Destination = PhysicalWorld.StreetNodes.RandomElement();
            UpdatePath();
        }
    }
    
    protected override void TurnOnNextStreetEdge()
    {
        var overlap = Position.DistanceFromSource - Position.StreetEdge.Length;
        Position.StreetEdge.DecrementCarCount();
        Position = new StreetPosition(Path.First(), overlap);
        Position.StreetEdge.IncrementCarCount();
        Path = Path.Skip(1);
        if (Logging)
        {
            Console.WriteLine($"{this}\ttick | {Position.ToString()} | dest: {Destination.Id} | car count: {Position.StreetEdge.CarCount} | driving at {Position.StreetEdge.CurrentMaxSpeed():F2}kmh/{Position.StreetEdge.SpeedLimit:F2}kmh");
        }
        
        if (ReservedSpot != null && !Path.Any())
        {
            Status = CarStatus.Parking;
        }
    }

    
    protected override void HandleDestinationReached()
    {
        // init looking for parking
        TicksSpentParking = 0;
        Status = CarStatus.Parking;
        NextStreetToLookForParking();
    }
    
    private void NextStreetToLookForParking()
    {
        StreetEdge nextStreet;
        if (PhysicalWorld.Graph.TryGetOutEdges(Position.StreetEdge.Target, out var outGoingStreets)) // TODO possible parking spot search heuristic
        {
            nextStreet = outGoingStreets.ToList().RandomElement();
            Path = new List<StreetEdge> { nextStreet } ;
            LastParkingSpotPassedIndex = 0; 
        }
    }
    
    private void HandleParked()
    {
        if (Logging)
        {
            Console.WriteLine($"{this}\ttick | Parked at {Position} | {ParkTime} ticks remaining");
        }
        if (ParkTime == 0)
        {
            ResetAfterParking();
        }
        else
        {
            ParkTime--;
        }
    }
    
    private void ResetAfterParking()
    {
        DistanceTravelledParking = 0;
        ResetTrafficMetrics();
        Position.StreetEdge.FreeParkingSpot(LastOccupiedIndex);
        PhysicalWorld.IncrementUnoccupiedSpotCount();
        UpdateDestination();
    }
    
    private async Task HandleParking()
    {
        TicksSpentParking++;
        await DriveAccordingToPath();
        if (NodeReached())
        {
            DistanceTravelledParking += Position.StreetEdge.Length;
            NextStreetToLookForParking();
        }
        else
        {
            bool parkingFound;
            if (ReservedSpot != null && SupportsPgs)
            {
                parkingFound = Position.DistanceFromSource >= ReservedSpot.DistanceFromSource;
                LastParkingSpotPassedIndex = ReservedSpot.Index;
            }
            else
            {
                (parkingFound, int newLastPassedOrFound) = Position.StreetEdge.TryParkingLocally(Position.DistanceFromSource, LastParkingSpotPassedIndex);
                LastParkingSpotPassedIndex = newLastPassedOrFound;
            }
            if (parkingFound) // available spot found
            {
                PhysicalWorld.IncrementParkEvents();
                await ParkCar();
            }
        }
    }
    
    private bool NodeReached()
    {
        return Position.DistanceFromSource >= Position.StreetEdge.Length;
    }
    
    protected override async Task HandleNodeReached()
    {
        TurnOnNextStreetEdge();
        UpdateTrafficMetrics();
    }

    private void UpdateTrafficMetrics()
    {
        double speedReductionP = 100 - ((Position.StreetEdge.CurrentMaxSpeed() / Position.StreetEdge.SpeedLimit) * 100);
        _speedReductionSum += speedReductionP;
        _speedReductionCount++;
        _speedReductionRunningAvg = _speedReductionSum / _speedReductionCount;
    }
    
    private void ResetTrafficMetrics()
    {
        _speedReductionRunningAvg = 0;
        _speedReductionCount = 0;
        _speedReductionSum = 0;
    }
    
    private async Task ParkCar()
    {
        LastOccupiedIndex = LastParkingSpotPassedIndex;
        Position = new StreetPosition(Position.StreetEdge, Position.StreetEdge.ParkingSpots[LastOccupiedIndex].DistanceFromSource);
        Status = CarStatus.Parked;
        PhysicalWorld.DecrementUnoccupiedSpotCount();
        Random rand = new Random();
        ParkTime = rand.Next(0, MaxParkTime + 1);
        DistanceTravelledParking += Position.StreetEdge.ParkingSpots[LastOccupiedIndex].DistanceFromSource;

        await PublishTrafficKpis();
        await PublishParkingKpis();
        await PublishEnvironmentKpis();
    }
    
    // ----------------------------------- PUBLISH KPIs ----------------------------------- 
    
    private async Task PublishTrafficKpis()
    {
        // speed reduction
        var payload = Encoding.UTF8.GetBytes(_speedReductionRunningAvg.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/speedReduction", Payload = payload });
    }
    
    private async Task PublishParkingKpis()
    {
        // distance travelled parking
        var payload = Encoding.UTF8.GetBytes(DistanceTravelledParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/distTravelledParking", Payload = payload });
        
        // time spent parking
        payload = Encoding.UTF8.GetBytes(TicksSpentParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/timeSpentParking", Payload = payload });
        
        // distance from destination
        double distance = Position.StreetEdge.Length - Position.DistanceFromSource;
        var shortestPaths = PhysicalWorld.Graph.ShortestPathsDijkstra(
            edge => 100 - edge.SpeedLimit,
            Position.StreetEdge.Source);

        if (shortestPaths.Invoke(Destination, out var path))
        {
            Path = path;
            distance += Path.Sum(streetEdge => streetEdge.Length);
        }

        double distanceFromDestination = distance;
        payload = Encoding.UTF8.GetBytes(distanceFromDestination.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/distFromDest", Payload = payload });
    }
    
    private async Task PublishEnvironmentKpis()
    {
        // fuel consumption
        double totalFuelConsumptionParking = (DistanceTravelledParking / 1000) * (FuelConsumptionRate / 100);
        var payload = Encoding.UTF8.GetBytes(totalFuelConsumptionParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/fuelConsumption", Payload = payload });
        
        // co2 emissions
        int totalCo2EmissionsParking = (int)((DistanceTravelledParking / 1000) * Co2EmissionRate);
        payload = Encoding.UTF8.GetBytes(totalCo2EmissionsParking.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "kpi/parkingEmissions", Payload = payload });
    }
    
}