using ConsoleApp1.sim;
using ConsoleApp1.sim.graph;

namespace ConsoleApp1.pgs;

public record PathResponse(IEnumerable<StreetEdge> PathToReservedParkingSpot, ParkingSpot ReservedParkingSpot);
