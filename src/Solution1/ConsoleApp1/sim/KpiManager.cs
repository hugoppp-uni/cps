using System.Text;
using ConsoleApp1.sim;
using MQTTnet;
using MQTTnet.Client;
using QuickGraph.Algorithms;

namespace ConsoleApp1.clients;

public record Kpi(string Topic, byte[] Payload);

public class KpiManager
{
    public double SpeedReductionRunningAvg { get; set; }
    public double SpeedReductionSum { get; set; }
    public int SpeedReductionCount { get; set; }
    
    private const double FuelConsumptionRate = 6.5;  // liters per 100 km, average according to German Federal Environment Agency (UBA)
    private const int Co2EmissionRate = 131; // grams per km, average according to German Federal Environment Agency (UBA)
    
    public int TicksSpentParking { get; set; }
    public double DistanceTravelledParking { get; set; }
    
    private readonly IMqttClient _mqttClient;
    private readonly MockCar _car;

    public KpiManager(IMqttClient mqttClient, MockCar car)
    {
        _mqttClient = mqttClient;
        _car = car;
    }

    public void Reset()
    {
        DistanceTravelledParking = 0;
        TicksSpentParking = 0;
        SpeedReductionRunningAvg = 0;
        SpeedReductionCount = 0;
        SpeedReductionSum = 0;
    }

    public async Task Publish(Kpi kpi)
    {
        await _mqttClient.PublishAsync(new MqttApplicationMessage { Topic = kpi.Topic, Payload = kpi.Payload });
    }

    public async Task PublishAll()
    {
        // calc the rest
        double distanceFromDestination = _car.Position.StreetEdge.Length - _car.Position.DistanceFromSource;
        var shortestPaths = _car.World.Graph.ShortestPathsDijkstra(
            edge => 100 - edge.SpeedLimit,
            _car.Position.StreetEdge.Source);

        if (shortestPaths.Invoke(_car.Destination, out var path))
        {
            _car.Path = path;
            distanceFromDestination += _car.Path.Sum(streetEdge => streetEdge.Length);
        }
        
        double totalFuelConsumptionParking = (DistanceTravelledParking / 1000) * (FuelConsumptionRate / 100);
        int totalCo2EmissionsParking = (int)((DistanceTravelledParking / 1000) * Co2EmissionRate);
        
        // add all 
        await Publish(new Kpi("kpi/distFromDest", Encoding.UTF8.GetBytes(distanceFromDestination.ToString())));
        await Publish(new Kpi("kpi/distTravelledParking", Encoding.UTF8.GetBytes(DistanceTravelledParking.ToString())));
        await Publish(new Kpi("kpi/timeSpentParking", Encoding.UTF8.GetBytes(TicksSpentParking.ToString())));
        await Publish(new Kpi("kpi/speedReduction", Encoding.UTF8.GetBytes(SpeedReductionRunningAvg.ToString())));
        await Publish(new Kpi("kpi/fuelConsumption", Encoding.UTF8.GetBytes(totalFuelConsumptionParking.ToString())));
        await Publish(new Kpi("kpi/parkingEmissions", Encoding.UTF8.GetBytes(totalCo2EmissionsParking.ToString())));
    }
}