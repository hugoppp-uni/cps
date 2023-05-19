using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;

namespace ConsoleApp1.pgs;

public interface IParkingStrategy
{
    public ParkingSpot FindParkingSpot(PhysicalWorld world, StreetNode destination);
}