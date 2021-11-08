using System;
using System.Text.Json.Serialization;
#pragma warning disable CS1591

namespace Sewer56.Update.Resolvers.GameBanana.Structures;

public class GameBananaItemFile
{
    private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

    [JsonPropertyName("_sFile")]
    public string FileName { get; set; }

    [JsonPropertyName("_nFilesize")]
    public long Filesize { get; set; }

    [JsonPropertyName("_sDownloadUrl")]
    public string DownloadUrl { get; set; }

    [JsonPropertyName("_tsDateAdded")]
    public long DateAddedLong { get; set; }

    [JsonIgnore]
    public DateTime DateAdded => Epoch.AddSeconds(DateAddedLong);
}