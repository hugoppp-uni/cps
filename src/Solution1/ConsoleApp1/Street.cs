using QuickGraph;

namespace ConsoleApp1;

public class Street
{
    public int Length { get; set; }
    public int CarCount { get; set; }
    public double SpeedLimit { get; set; } = MathUtil.KmhToMs(50);

    public double CurrentMaxSpeed()
    {
        const double carLength = 5;
        double freeStreetLenght = Length - CarCount * carLength;
        return Math.Min(SpeedLimit, MathUtil.GetSafeSpeedMs(freeStreetLenght / (double)CarCount));
    }

}
