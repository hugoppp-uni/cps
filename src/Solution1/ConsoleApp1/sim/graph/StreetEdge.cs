using QuickGraph;

namespace ConsoleApp1.sim.graph;

public record StreetEdge : Street, IEdge<StreetNode>
{
    public required StreetNode Source { get; init; }
    public required StreetNode Target { get; init; }
    
    public string? StreetName 
    {
        get 
        {
            string name = Tags.GetValueOrDefault("name");
            if (string.IsNullOrEmpty(name))
            {
                string junctionString = Tags.GetValueOrDefault("junction");
                if (junctionString != null)
                {
                    return char.ToUpper(junctionString[0]) + junctionString.Substring(1);
                }
                return junctionString;
            } 
            return name;
        }
    }
    
    public int UnoccupiedSpotCount { get; set; }
    public void InitParkingSpots(Dictionary<ParkingSpot, StreetEdge> parkingSpotMap)
    {
        int maxParkingSpots = (int)Math.Floor(Length / ParkingSpotLength);
        NumParkingSpots = (int)Math.Floor(maxParkingSpots * ParkingSpotDensity);
        ParkingSpotSpacing = (Length - NumParkingSpots * ParkingSpotLength) / NumParkingSpots;
        Random rand = new Random();

        UnoccupiedSpotCount = 0;
        ParkingSpots = Enumerable.Range(0, NumParkingSpots)
            .Select(i =>
            {
                double distanceFromSource = (ParkingSpotLength + ParkingSpotSpacing) * i;
                bool occupied = rand.NextDouble() >= InitialParkingSpotOccupancyRate;
                if (!occupied) UnoccupiedSpotCount++;
                return new ParkingSpot(i, distanceFromSource, Length, ParkingSpotLength, occupied, false);
            }).ToList();
        
        ParkingSpots.ForEach(parkingSpot => parkingSpotMap.Add(parkingSpot, this));
    }


    public required Dictionary<string, string?> Tags { get; init; }
    
    public override string ToString()
    {
        var streetInfo = base.ToString();
        var sourceInfo = $"Source: {Source.Id}, ({Source.Latitude:F6}, {Source.Longitude:F6})";
        var targetInfo = $"Target: {Target.Id}, ({Target.Latitude:F6}, {Target.Longitude:F6})";
        var name = $"Street: {StreetName}";

        return $"{streetInfo}{sourceInfo}\n{targetInfo}\n{name}\n";
    }

}