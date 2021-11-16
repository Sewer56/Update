using System.Text.Json.Serialization;
#pragma warning disable CS1591

namespace Sewer56.Update.Resolvers.GameBanana.Structures;

public class GameBananaItemUpdateChange
{
    [JsonPropertyName("cat")]
    public string? Category { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}