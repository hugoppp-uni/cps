using System.Text;
using QuickGraph;

namespace ConsoleApp1;

public record Street
{
    public required double Length { get; init; }
    public int CarCount { get; set; }
    public required double SpeedLimit { get; init; }
    public List<ParkingSpot> ParkingSpots { get; set; } = new List<ParkingSpot>();
    public double ParkingDensity { get; set; } = 0.5; // default density
    public double ParkingFrequency { get; } = 0.01;
    public int NumParkingSpots { get; set; }
    public double ParkingSpotSpacing { get; set; }
    public double ParkingSpotLength { get; } = 5.0;

    public double CurrentMaxSpeed()
    {
        // TODO this produces negative numbers
        /*
        const double carLength = 5;
        double freeStreetLength = Length - CarCount * carLength;
        return Math.Min(SpeedLimit, MathUtil.GetSafeSpeedMs(freeStreetLength / CarCount));
        */
        return SpeedLimit;
    }

    public void InitParkingSpots()
    {
        if (ParkingDensity < 0 || ParkingDensity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ParkingDensity), "Value must be between 0 and 1.");
        }
        int maxParkingSpots = (int)Math.Floor(Length / ParkingSpotLength);
        NumParkingSpots = (int)Math.Floor(maxParkingSpots * ParkingDensity);
        ParkingSpotSpacing = (Length - NumParkingSpots * ParkingSpotLength) / NumParkingSpots;
        Random rand = new Random();
        ParkingSpots = Enumerable.Range(0, NumParkingSpots)
            .Select(i => new ParkingSpot(i, (ParkingSpotLength + ParkingSpotSpacing) * i, Length, ParkingSpotLength, rand.NextDouble() >= ParkingFrequency))
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
