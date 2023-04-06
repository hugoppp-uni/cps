using ConsoleApp1;

for (int i = 50; i > 1; i -= 1)
{
    Console.WriteLine($"{i}m - {MathUtil.MsToKmh(MathUtil.GetSafeSpeedMs(i)):F1}kmh");
}

for (int i = 0; i < 16; i++)
{
    Console.WriteLine(
        $"{i} cars on 100m - {MathUtil.MsToKmh(new Street { Length = 100, CarCount = i, SpeedLimit = 50 }.CurrentMaxSpeed()):F1}kmh");
}

var graph = StreetGraphParser.Parse(File.ReadAllText("res/street.json"));
var graphviz = graph.ToGraphvizFormatted();