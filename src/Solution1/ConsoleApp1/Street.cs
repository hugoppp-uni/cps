using QuickGraph;

namespace ConsoleApp1;

public record Street
{
    public required double Length { get; init; }
    public int CarCount { get; set; }
    public required double SpeedLimit { get; init; }

    public double CurrentMaxSpeed()
    {
        const double carLength = 5;
        double freeStreetLenght = Length - CarCount * carLength;
        return Math.Min(SpeedLimit, MathUtil.GetSafeSpeedMs(freeStreetLenght / CarCount));
    }

}
