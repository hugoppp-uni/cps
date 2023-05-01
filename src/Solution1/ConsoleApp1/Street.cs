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

    private object _streetLock = new object();

    public double CurrentMaxSpeed()
    {
        // TODO this produces negative numbers
        // TODO lock the street
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
    
    public (bool parkingFound, int lastPassedOrFoundIndex) TryParkingLocally(double distanceFromSource, int lastPassedIndex)
    {
        lock (_streetLock)
        {
            if (ParkingSpots.Count == 0) return (false, -1); // street too short to have parking
            int smallestIndexUncheckedSpot = lastPassedIndex;
            int newLastPassedOrFoundIndex = CalculateLastPassedIndex(distanceFromSource, lastPassedIndex);

            var checkForOccupancy = ParkingSpots
                .Skip(smallestIndexUncheckedSpot)
                .Take(lastPassedIndex - smallestIndexUncheckedSpot + 1);

            ParkingSpot availableSpot = checkForOccupancy.LastOrDefault(ps => !ps.Occupied);
            bool parkingFound = false;
            if (availableSpot != null)
            {
                // Console.WriteLine($"{this}\tAvailable spot at {availableSpot.DistanceFromSource} on {Position.StreetEdge.StreetName}");
                availableSpot.Occupied = true;
                CarCount--;
                newLastPassedOrFoundIndex = availableSpot.Index;
                parkingFound = true;
            }
            return (parkingFound, newLastPassedOrFoundIndex);
        }
    }

    public void FreeParkingSpot(int spotIndex)
    {
        lock (_streetLock)
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
        lock (_streetLock)
        {
            CarCount++;
        }
    }
    
    public void DecrementCarCount()
    {
        lock (_streetLock)
        {
            CarCount--;
        }
    }
    
    public override string ToString()
    {
        string parkingInfo = ParkingSpots.Count == 0
            ? "No parking."
            : $"Parking spots ({ParkingSpots.Count}): \n{string.Join("\n", ParkingSpots.Select(p => p.ToString()))}";

        return $"Length: {Length}, Car count: {CarCount}, Speed limit: {SpeedLimit}, Parking Density: {ParkingDensity}, Parking Spacing: {ParkingSpotSpacing:F2}\n{parkingInfo}\n";
    }


}
