using QuickGraph;

namespace ConsoleApp1;

public record StreetEdge : Street, IEdge<StreetNode>
{
    public required StreetNode Source { get; init; }
    public required StreetNode Target { get; init; }
    public string? StreetName => Tags.GetValueOrDefault("name");
    public required Dictionary<string, string?> Tags { get; init; }
}