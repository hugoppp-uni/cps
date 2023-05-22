using System.ComponentModel;
using System.Text;
using ConsoleApp1.sim;
using MQTTnet;
using MQTTnet.Client;
using QuickGraph;

namespace ConsoleApp1.clients;

public record DynamicSimData(
    int TotalSpotsAvailable,
    double ParkingFrequency,
    double StreetCarCountVariance
    );

public class SimDataClient: BaseClient
{
    
    public SimDataClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int publishInterval) : base(mqttClient)
    {
        World = physicalWorld;
        PublishInterval = publishInterval;
        CallCount = 0;
        
        PublishStaticSimData();
        PublishDynamicSimData();
    }

    private async void PublishStaticSimData()
    {
        // total parking spots
        var payload = Encoding.UTF8.GetBytes(World.ParkingSpotCount.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/static/parkingSpotCount", Payload = payload });
        
        // initially available spots
        payload = Encoding.UTF8.GetBytes(World.UnoccupiedSpotCount.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/static/initiallyAvailableSpots", Payload = payload });
        
        // car count
        payload = Encoding.UTF8.GetBytes(World.CarCount.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/static/carCount", Payload = payload });
        
        // cruiser count
        payload = Encoding.UTF8.GetBytes(World.CruiserCount.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/static/cruiserCount", Payload = payload });
        
        // parker count
        payload = Encoding.UTF8.GetBytes(World.ParkerCount.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/static/parkerCount", Payload = payload });
        
        // parking spot density
        payload = Encoding.UTF8.GetBytes(Street.ParkingSpotDensity.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/static/parkingSpotDensity", Payload = payload });
        
        // parking spot occupancy frequency
        payload = Encoding.UTF8.GetBytes(Street.InitialParkingSpotOccupancyRate.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/static/initialParkingSpotOccupancyRate", Payload = payload });
        
        // parking spot length
        payload = Encoding.UTF8.GetBytes(Street.ParkingSpotLength.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/static/parkingSpotLength", Payload = payload });
    }

    public int PublishInterval { get; set; }
    public int CallCount { get; set; }
    public PhysicalWorld World { get; set; }

    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        CallCount++;
        if (CallCount % PublishInterval == 0)
        {
            PublishDynamicSimData();
        }
    }

    private async void PublishDynamicSimData()
    {
        // unoccupied parking spot count
        var payload = Encoding.UTF8.GetBytes(World.UnoccupiedSpotCount.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/dynamic/unoccupiedSpotCount", Payload = payload });
        
        // todo meaningful congestion metric
        // load deviation
        double averageLoad = World.StreetEdges.Average(edge => edge.CarCount / edge.Length);
        double loadDeviation = 0;
        World.StreetEdges.ToList().ForEach( edge =>
        {
            double load = edge.CarCount / edge.Length;
            loadDeviation += Math.Pow(load - averageLoad, 2);
        });
        loadDeviation = Math.Sqrt(loadDeviation / World.StreetEdges.Count);
        payload = Encoding.UTF8.GetBytes(loadDeviation.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/dynamic/carCountVariance", Payload = payload });
        
        // car count weighted variance
        /*
        double sumWeights = World.StreetEdges.Sum(edge => edge.CarCount / World.CarCount);
        double carCountWeightedMean = World.StreetEdges.Sum(edge => edge.CarCount * (edge.CarCount / World.CarCount)) / sumWeights;
        double carCountWeightedSumSquaredDifferences = World.StreetEdges.Sum(edge => (edge.CarCount / World.CarCount) * Math.Pow(edge.CarCount - carCountWeightedMean, 2));
        double carCountVariance = carCountWeightedSumSquaredDifferences / sumWeights;
        
        payload = Encoding.UTF8.GetBytes(carCountVariance.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/dynamic/carCountVariance", Payload = payload });
        */
        
        /*
        var maxCongestionScore = World.StreetEdges.Max(edge => edge.CarCount / edge.Length);
        double congestionScoreWeightedSum = 0;
        World.StreetEdges.ToList().ForEach(edge =>
        {
            var congestionScore = edge.CarCount / edge.Length;
            var congestionWeight = congestionScore / maxCongestionScore;
            congestionScoreWeightedSum += congestionWeight * congestionScore;
        });

        var globalCongestionScore = congestionScoreWeightedSum / World.StreetEdges.Count;
        
        //var normalGlobalCongestionScore = globalCongestionScore / maxCongestionScore;
        payload = Encoding.UTF8.GetBytes(globalCongestionScore.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/dynamic/carCountVariance", Payload = payload });
        */

        // parking rate
        double simTimeH = (double)CallCount / 60;
        double parkingRateM = World.ParkEvents / simTimeH;
        payload = Encoding.UTF8.GetBytes(parkingRateM.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/dynamic/parkingRate", Payload = payload });
    }

    public static async Task<SimDataClient> Create(MqttClientFactory clientFactory, PhysicalWorld physicalWorld, int publishInterval)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tickgen/tick"));
        return new SimDataClient(client, physicalWorld, publishInterval);
    }
}