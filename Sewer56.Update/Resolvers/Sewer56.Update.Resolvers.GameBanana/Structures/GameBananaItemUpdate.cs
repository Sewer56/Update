using System;
using System.Text.Json.Serialization;
#pragma warning disable CS1591

namespace Sewer56.Update.Resolvers.GameBanana.Structures;

public class GameBananaItemUpdate
{
    private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

    [JsonPropertyName("_sTitle")]
    public string Title { get; set; }

    [JsonPropertyName("_aChangeLog")]
    public GameBananaItemUpdateChange[] Changes { get; set; }

    [JsonPropertyName("_sText")]
    public string Text { get; set; }

    [JsonPropertyName("_tsDateAdded")]
    public long DateAddedLong { get; set; }

    [JsonIgnore]
    public DateTime DateAdded => Epoch.AddSeconds(DateAddedLong);
}