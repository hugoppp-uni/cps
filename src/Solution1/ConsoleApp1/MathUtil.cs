namespace ConsoleApp1;

public class MathUtil
{
    public static double KmhToMs(double kmh) => kmh * 1000d / 3600d;
    public static double MsToKmh(double ms) => ms / (1000d / 3600d);

    private const int SafetyDeltaSeconds = 1;

    public static double GetSafeDistanceMeters(double speedMs)
    {
        //d = v * t
        return speedMs * SafetyDeltaSeconds;
    }

    public static double GetSafeSpeedMs(double distance)
    {
        //v = d / t
        return distance / SafetyDeltaSeconds;
    }
}