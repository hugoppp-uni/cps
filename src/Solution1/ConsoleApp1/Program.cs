using ConsoleApp1;

for (int i = 50; i > 1; i -= 1)
{
    Console.WriteLine($"{i}m - {MathUtil.MsToKmh(MathUtil.GetSafeSpeedMs(i)):F1}kmh");
}

for (int i = 0; i < 16; i++)
{
    Console.WriteLine(
        $"{i} cars on 100m - {MathUtil.MsToKmh(new Street { Length = 100, CarCount = i }.CurrentMaxSpeed()):F1}kmh");
}

// var graph = new AdjacencyGraph<Node, Street>();
// graph.AddVertexRange(new Node[] { new("1"), new("1"), new("1"), new("1"), new("1"), });
// graph.AddEdge(new Street(graph.Vertices.First(v), "4"));
// graph.AddEdge(new Street("1", "2"));
// graph.AddEdge(new Street("2", "4"));
// Console.WriteLine(graph.ToGraphviz());
// var dijkstraShortestPathAlgorithm = graph.ShortestPathsDijkstra(edge => 1, "1");
// if (dijkstraShortestPathAlgorithm("4", out var path))
// foreach (var edge in path)
// Console.WriteLine(edge);