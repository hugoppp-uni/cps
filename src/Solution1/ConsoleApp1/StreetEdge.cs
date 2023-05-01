using QuickGraph;

namespace ConsoleApp1;

public record StreetEdge : Street, IEdge<StreetNode>
{
    public required StreetNode Source { get; init; }
    public required StreetNode Target { get; init; }
    // public string? StreetName => Tags.GetValueOrDefault("name");
    
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