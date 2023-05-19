using ConsoleApp1.util;

namespace ConsoleApp1.sim;

public record Street
{
    public required double Length { get; init; }
    public int CarCount { get; set; }
    public double CarLength { get; } = 5.0;
    public required double SpeedLimit { get; init; }
    public List<ParkingSpot> ParkingSpots { get; set; } = new List<ParkingSpot>();
    public double ParkingDensity { get; set; } = 0.5; // between 0 and 1
    public double ParkingFrequency { get; } = 0.01;
    public int NumParkingSpots { get; set; }
    public double ParkingSpotSpacing { get; set; }
    public double ParkingSpotLength { get; } = 5.0;


    public double CurrentMaxSpeed()
    {
        lock (this)
        {
            double freeStreetLength = Length - CarCount * CarLength;
            double recommendedSpeed = MathUtil.GetSafeSpeedMs(freeStreetLength / CarCount);
            return Math.Min(SpeedLimit, recommendedSpeed);
        }
    }

    public double CurrentCoverDuration()
    {
        lock (this)
        {
            double timeS = Length / MathUtil.KmhToMs(CurrentMaxSpeed());
            return timeS;
        }
    }

    public (bool parkingFound, int lastPassedOrFoundIndex) TryParkingLocally(double distanceFromSource,
        int lastPassedIndex)
    {
        lock (this)
        {
            if (ParkingSpots.Count == 0) return (false, -1); // street too short to have parking
            int smallestIndexUncheckedSpot = lastPassedIndex;
            int newLastPassedOrFoundIndex = CalculateLastPassedIndex(distanceFromSource, lastPassedIndex);

            var checkForOccupancy = ParkingSpots
                .Skip(smallestIndexUncheckedSpot)
                .Take(lastPassedIndex - smallestIndexUncheckedSpot + 1);

            ParkingSpot? availableSpot = checkForOccupancy.LastOrDefault(ps => !ps.Occupied);
            if (availableSpot == null)
                return (false, newLastPassedOrFoundIndex);

            availableSpot.Occupied = true;
            CarCount--;
            newLastPassedOrFoundIndex = availableSpot.Index;
            return (true, newLastPassedOrFoundIndex);
        }
    }

    public void FreeParkingSpot(int spotIndex)
    {
        lock (this)
        {
            ParkingSpots[spotIndex].Occupied = false;
            CarCount++;
        }
    }

    private int CalculateLastPassedIndex(double distanceFromSource, int lastPassedIndex)
    {
        int lastPassedIndexFromDistance = (int)Math.Floor(distanceFromSource /
                                                          (ParkingSpots[lastPassedIndex].Length + ParkingSpotSpacing));
        return Math.Min(lastPassedIndexFromDistance, ParkingSpots.Count - 1);
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
            $"Length: {Length}, Car count: {CarCount}, Speed limit: {SpeedLimit}, Parking Density: {ParkingDensity}, Parking Spacing: {ParkingSpotSpacing:F2}\n{parkingInfo}\n";
    }
}