using System.Text;
using QuickGraph;

namespace ConsoleApp1;

public record Street
{
    public required double Length { get; init; }
    public int CarCount { get; set; }
    public required double SpeedLimit { get; init; }
    public List<ParkingSpot> ParkingSpots { get; set; } = new List<ParkingSpot>();
    public double ParkingDensity { get; set; } = 0.8; // default density
    public int NumParkingSpots { get; set; }
    public double ParkingSpotSpacing { get; set; }

    public double CurrentMaxSpeed()
    {
        const double carLength = 5;
        double freeStreetLength = Length - CarCount * carLength;
        return Math.Min(SpeedLimit, MathUtil.GetSafeSpeedMs(freeStreetLength / CarCount));
    }

    public void InitParkingSpots()
    {
        if (ParkingDensity < 0 || ParkingDensity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ParkingDensity), "Value must be between 0 and 1.");
        }
        int maxParkingSpots = (int)Math.Floor(Length / ParkingSpot.Length);
        NumParkingSpots = (int)Math.Floor(maxParkingSpots * ParkingDensity);
        ParkingSpotSpacing = (Length - NumParkingSpots * ParkingSpot.Length) / NumParkingSpots;
        ParkingSpots = Enumerable.Range(0, NumParkingSpots)
            .Select(i => new ParkingSpot(i, (ParkingSpot.Length + ParkingSpotSpacing) * i, Length))
            .ToList();
    }
    
    public override string ToString()
    {
        string parkingInfo = ParkingSpots.Count == 0
            ? "No parking."
            : $"Parking spots ({ParkingSpots.Count}): \n{string.Join("\n", ParkingSpots.Select(p => p.ToString()))}";

        return $"Length: {Length}, Car count: {CarCount}, Speed limit: {SpeedLimit}, Parking Density: {ParkingDensity}, Parking Spacing: {ParkingSpotSpacing:F2}\n{parkingInfo}\n";
    }


}
