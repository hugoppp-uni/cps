namespace ConsoleApp1;

public class ParkingSpot
{
    public bool Occupied { get; set; }
    public static double Length { get; } = 5.0;
    public double DistanceFromSource { get; }
    public double StreetLength { get; }
    public int Index { get; }
    

    public ParkingSpot(int index, double distanceFromSource, double streetLength)
    {
        StreetLength = streetLength;
        Occupied = true;
        Index = index;
        DistanceFromSource = distanceFromSource;
    }
    public override string ToString() => $"[{Index}] Occupied: {Occupied}, Position: {DistanceFromSource}/{StreetLength}"; 
}