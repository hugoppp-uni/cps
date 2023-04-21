using Geolocation;

namespace ConsoleApp1;

public record StreetNode
{
    public Coordinate Coordinate => new(Latitude, Longitude);

    //public int Number { get; set; }
    public long Id { get; set; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
}