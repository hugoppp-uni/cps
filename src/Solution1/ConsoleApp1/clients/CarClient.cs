using ConsoleApp1.pgs;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;
using MQTTnet.Client;

namespace ConsoleApp1.clients;

public class CarClient: BaseClient
{
    private const int _kpiPublishIntervall = 100;
    protected CarClient(IMqttClient mqttClient, ICarClientBehaviour behaviour,
        PhysicalWorld world, ParkingGuidanceSystem pgs, int id, bool logging) : base(mqttClient)
    {
        Behaviour = behaviour;

        CallCount = 0;
        CarData = new CarData(id, world, pgs, logging);
        
        Pgs = pgs;
        
        // get initial dest
        CarData.Status = CarStatus.Driving;
        CarData.ResetKpiMetrics();
        Behaviour.UpdateDestination(CarData);
    }

    public int CallCount { get; set; }

    public int KpiPublishIntervall { get; set; }

    public ParkingGuidanceSystem Pgs { get; set; }

    private ICarClientBehaviour Behaviour { get; set; }

    public CarData CarData { get; set; }
    
    /*
     * Creation through factory
     */
    public static async Task<CarClient> Create(MqttClientFactory clientFactory, ICarClientBehaviour behaviour,
        PhysicalWorld physicalWorld, ParkingGuidanceSystem pgs, int id, bool logging)
    {
        var client = await clientFactory.CreateClient(builder => builder.WithTopicFilter("tickgen/tick"));
        return new CarClient(client, behaviour, physicalWorld, pgs, id, logging);
    }

    /**
     * Main state machine
     */
    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        switch (CarData.Status)
        {
            case CarStatus.PathingFailed:
                if (CarData.Logging)
                {
                    Console.WriteLine($"{CarData} entered state PathingFailed ({CarData.Status}, {CarData.World.GetUnoccupiedSpotsCount()})");
                }
                CarData.Status = CarStatus.Driving;
                CarData.RespawnAtRandom();
                Behaviour.UpdateDestination(CarData);
                break;
            
            case CarStatus.Driving: 
                if (CarData.Logging)
                {
                    Console.WriteLine($"{CarData} entered state Driving ({CarData.Status})");
                }
                if (CarData.DestinationReached()) 
                {
                    CarData.Status = CarStatus.Parking;
                }
                else 
                {
                    Behaviour.DriveAlongPath(CarData);
                }
                break;

            case CarStatus.Parking:
                if (CarData.Logging)
                {
                    Console.WriteLine($"{CarData} entered state Parking ({CarData.Status})");
                }
                Behaviour.SeekParkingSpot(CarData);
                new CruiserClientBehaviour().DriveAlongPath(CarData);
                if (await Behaviour.AttemptLocalParking(CarData))
                {
                    CarData.Status = CarStatus.Parked;
                    await CarData.PublishAll(MqttClient);
                }
                break;
            
            case CarStatus.Parked:
                if (CarData.Logging)
                {
                    Console.WriteLine($"{CarData} entered state Parked ({CarData.Status})");
                }
                if (!Behaviour.StayParked(CarData))
                {
                    CarData.Status = CarStatus.Driving;
                    Behaviour.UpdateDestination(CarData);
                }
                break;
            
            default:
                throw new InvalidOperationException($"{this}\tInvalid status: {CarData.Status}");
        }
    }
    
}