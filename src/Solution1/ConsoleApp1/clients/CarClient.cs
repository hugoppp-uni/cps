using ConsoleApp1.pgs;
using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;
using ConsoleApp1.util;
using MQTTnet.Client;

namespace ConsoleApp1.clients;

public class CarClient: BaseClient
{
    protected CarClient(IMqttClient mqttClient, ICarClientBehaviour behaviour, PhysicalWorld world, ParkingGuidanceSystem pgs, int id, bool logging) : base(mqttClient)
    {
        Behaviour = behaviour;
        var kpiManager = new KpiManager(mqttClient, Car);
        Car = new MockCar(id, world, kpiManager, logging);
        Pgs = pgs;
        
        // get initial dest
        Car.Status = CarStatus.Driving;
        Car.KpiManager.Reset();
        Behaviour.UpdateDestination(Car);
    }

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
                Car.KpiManager.TicksSpentParking++;
                Behaviour.SeekParkingSpot(Car);
                new CruiserClientBehaviour().DriveAlongPath(Car);
                if (Behaviour.AttemptLocalParking(Car))
                {
                    Car.Status = CarStatus.Parked;
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