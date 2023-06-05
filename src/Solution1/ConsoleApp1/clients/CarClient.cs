using System.Text;
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
        PhysicalWorld world, ParkingGuidanceSystem pgs, bool parked, int id) : base(mqttClient)
    {
        Behaviour = behaviour;

        CallCount = 0;
        CarData = new CarData(id, world, pgs);
        
        Pgs = pgs;

        // get initial dest
        CarData.ResetKpiMetrics();
        CarData.Status = CarStatus.Driving;
        Behaviour.UpdateDestination(CarData);
        
        if (parked)
        {
            CarData.Status = CarStatus.Parked;
            CarData.Park(world.GetRandomUnoccupied() ?? throw new InvalidOperationException());
        }
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
        PhysicalWorld physicalWorld, ParkingGuidanceSystem pgs, bool parked, int id)
    {
        var client = await clientFactory.CreateClient(builder => builder.
                WithTopicFilter("tickgen/tick").
                WithTopicFilter("pgs/on").
                WithTopicFilter("rogue/on"));
        
        return new CarClient(client, behaviour, physicalWorld, pgs, parked, id);
    }

    /**
     * Main state machine
     */
    protected override async Task MqttClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        string topic = arg.ApplicationMessage.Topic;
        string payloadStr = Encoding.Default.GetString(arg.ApplicationMessage.Payload);
        if (topic == "pgs/on")
            Behaviour.SetPgs(bool.Parse(payloadStr));
        else if (topic == "rogue/on")
            Behaviour.SetRogue(bool.Parse(payloadStr));

        switch (CarData.Status)
        {
            case CarStatus.PathingFailed:
                CarData.Status = CarStatus.Driving;
                CarData.RespawnAtRandom();
                Behaviour.UpdateDestination(CarData);
                break;
            
            case CarStatus.Driving: 
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
                Behaviour.SeekParkingSpot(CarData);
                if (await Behaviour.AttemptLocalParking(CarData))
                {
                    CarData.Status = CarStatus.Parked;
                    await Behaviour.PublishAll(CarData, MqttClient);
                }
                break;
            
            case CarStatus.Parked:
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