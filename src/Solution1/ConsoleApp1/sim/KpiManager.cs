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
    private MockCar _car;
    private List<Kpi> _kpis;

    public KpiManager(IMqttClient mqttClient, MockCar car)
    {
        _mqttClient = mqttClient;
        _car = car;
        _kpis = new List<Kpi>();
    }

    public void Reset()
    {
        DistanceTravelledParking = 0;
        TicksSpentParking = 0;
        SpeedReductionRunningAvg = 0;
        SpeedReductionCount = 0;
        SpeedReductionSum = 0;
    }

    byte[] Encode(string obj)
    {
        return Encoding.UTF8.GetBytes(obj);
    }

    public void Publish()
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
        _kpis.Add(new Kpi("kpi/distFromDest", Encode(distanceFromDestination.ToString())));
        _kpis.Add(new Kpi("kpi/distTravelledParking", Encode(DistanceTravelledParking.ToString())));
        _kpis.Add(new Kpi("kpi/timeSpentParking", Encode(TicksSpentParking.ToString())));
        _kpis.Add(new Kpi("kpi/speedReduction", Encode(SpeedReductionRunningAvg.ToString())));
        _kpis.Add(new Kpi("kpi/fuelConsumption", Encode(totalFuelConsumptionParking.ToString())));
        _kpis.Add(new Kpi("kpi/parkingEmissions", Encode(totalCo2EmissionsParking.ToString())));
        
        // publish
        _kpis.ForEach(kpi =>
        {
            _mqttClient.PublishAsync(new MqttApplicationMessage { Topic = kpi.Topic, Payload = kpi.Payload });
        });
    }
}