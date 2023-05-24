namespace ConsoleApp1.sim;

public class ParkingSpot
{
    public bool Occupied { get; set; }
    public bool Reserved { get; set; }
    public double Length { get; }
    public double DistanceFromSource { get; }
    public double StreetLength { get; }
    public int Index { get; }
    

    public ParkingSpot(int index, double distanceFromSource, double streetLength, double length, bool occupied, bool reserved)
    {
        Length = length;
        StreetLength = streetLength;
        Occupied = occupied;
        Reserved = reserved;
        Index = index;
        DistanceFromSource = distanceFromSource;
    }

    public override string ToString() => $"[{Index}] Occupied: {Occupied}, Position: {DistanceFromSource}/{StreetLength}";

}