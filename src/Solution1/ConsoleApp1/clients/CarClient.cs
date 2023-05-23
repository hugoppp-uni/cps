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
        Car = new MockCar(id, world, pgs, logging);
        
        Pgs = pgs;
        
        // get initial dest
        Car.Status = CarStatus.Driving;
        Car.ResetKpiMetrics();
        Behaviour.UpdateDestination(Car);
    }

    public int CallCount { get; set; }

    public int KpiPublishIntervall { get; set; }

    public ParkingGuidanceSystem Pgs { get; set; }

    private ICarClientBehaviour Behaviour { get; set; }

    public MockCar Car { get; set; }
    
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
        switch (Car.Status)
        {
            case CarStatus.PathingFailed:
                Car.Status = CarStatus.Driving;
                Car.RespawnAtRandom();
                Behaviour.UpdateDestination(Car);
                break;
            
            case CarStatus.Driving: 
                if (Car.DestinationReached()) 
                {
                    Car.Status = CarStatus.Parking;
                }
                else 
                {
                    Behaviour.DriveAlongPath(Car);
                }
                break;

            case CarStatus.Parking:
                Behaviour.SeekParkingSpot(Car);
                new CruiserClientBehaviour().DriveAlongPath(Car);
                if (await Behaviour.AttemptLocalParking(Car))
                {
                    Car.Status = CarStatus.Parked;
                    await Car.PublishAll(MqttClient);
                }
                break;
            
            case CarStatus.Parked:
                if (!Behaviour.StayParked(Car))
                {
                    Car.Status = CarStatus.Driving;
                    Behaviour.UpdateDestination(Car);
                }
                break;
            
            default:
                throw new InvalidOperationException($"{this}\tInvalid status: {Car.Status}");
        }
    }
    
}