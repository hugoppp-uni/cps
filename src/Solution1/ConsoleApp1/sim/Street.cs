using ConsoleApp1.sim.graph;
using ConsoleApp1.util;

namespace ConsoleApp1.sim;

public record Street
{
    private readonly double _speedLimit;
    public required double Length { get; init; }
    public static double CarLength { get; } = 5.0;


    public int CarCount { get; set; }

    public required double SpeedLimitMs { get; init; }

    public List<ParkingSpot> ParkingSpots { get; set; } = new List<ParkingSpot>();
    public static double ParkingSpotDensity { get; set; } = 0.5; // between 0 and 1
    public static double InitialParkingSpotOccupancyRate { get; } = 0.01;
    public int NumParkingSpots { get; set; }
    public double ParkingSpotSpacing { get; set; }
    public static double ParkingSpotLength { get; } = 5.0;

    public double CurrentMaxSpeedMs()
    {
        lock(this)
        {
            double freeStreetLength = Length - CarCount * CarLength;
            double recommendedSpeed = MathUtil.GetSafeSpeedMs(freeStreetLength / CarCount);
            return Math.Min(SpeedLimitMs, recommendedSpeed);
        }
    }

    public double CurrentCoverDuration()
    {
        lock (this)
        {
            double timeS = Length / MathUtil.KmhToMs(CurrentMaxSpeedMs());
            return timeS;
        }
    }

    public void IncrementCarCount()
    {
        lock (this)
        {
            CarCount++;
        }
    }

    public void DecrementCarCount()
    {
        lock (this)
        {
            CarCount--;
        }
    }

    public override string ToString()
    {
        string parkingInfo = ParkingSpots.Count == 0
            ? "No parking."
            : $"Parking spots ({ParkingSpots.Count}): \n{string.Join("\n", ParkingSpots.Select(p => p.ToString()))}";

        return
            $"Length: {Length}, Car count: {CarCount}, Speed limit: {SpeedLimitMs}, Parking Density: {ParkingSpotDensity}, Parking Spacing: {ParkingSpotSpacing:F2}\n{parkingInfo}\n";
    }
}