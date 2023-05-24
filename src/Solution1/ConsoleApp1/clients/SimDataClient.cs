using System.ComponentModel;
using System.Text;
using ConsoleApp1.sim;
using MQTTnet;
using MQTTnet.Client;
using QuickGraph;

namespace ConsoleApp1.clients;

public class SimDataClient: BaseClient
{
    private SimDataClient(IMqttClient mqttClient, PhysicalWorld physicalWorld, int publishInterval) : base(mqttClient)
    {
        World = physicalWorld;
        PublishInterval = publishInterval;
        CallCount = 0;
        
        PublishStaticSimData();
        PublishDiagnostics();
    }

    private async void PublishStaticSimData()
    {
        // total parking spots
        var payload = Encoding.UTF8.GetBytes(World.ParkingSpotCount.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/static/parkingSpotCount", Payload = payload });
        
        // initially available spots
        payload = Encoding.UTF8.GetBytes(World.InitialSpotCount.ToString());
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

    private int PublishInterval { get; set; }
    private int CallCount { get; set; }
    private PhysicalWorld World { get; set; }

    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        CallCount++;
        if (CallCount % PublishInterval == 0)
        {
            PublishDiagnostics();
        }
    }

    private async void PublishDiagnostics()
    {
        // unoccupied parking spot count
        var payload = Encoding.UTF8.GetBytes(World.GetUnoccupiedSpotsCount().ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/dynamic/unoccupiedSpotCount", Payload = payload });
        
        // traffic density average
        double trafficDensitySum = World.StreetEdges.Sum(edge => edge.TrafficDensity);
        double avgTrafficDensity = trafficDensitySum / World.StreetEdges.Count;
        payload = Encoding.UTF8.GetBytes(avgTrafficDensity.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/dynamic/trafficDensity", Payload = payload });

        // todo parking rate: make this rolling avg
        double simTimeM = (double)CallCount / 60;
        double parkingRateM = World.ParkEvents / simTimeM;

        payload = Encoding.UTF8.GetBytes(parkingRateM.ToString());
        await MqttClient.PublishAsync(new MqttApplicationMessage { Topic = "simData/dynamic/parkingRate", Payload = payload });
    }

    private int _previousParkEvents { get; set; } = 0;
    private int _previousCallCount { get; set; } = 0;

    public static async Task<SimDataClient> Create(MqttClientFactory clientFactory, PhysicalWorld physicalWorld, int publishInterval)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tickgen/tick"));
        return new SimDataClient(client, physicalWorld, publishInterval);
    }
}