using CommandLine;

namespace Sewer56.Update.Tool.Options.Groups;

public interface IGameBananaDownloadOptions
{
    [Option(SetName = "GameBanana", HelpText = $"[{nameof(DownloadSource.GameBanana)} Specific] Legacy use only. All items today and in the future are going to be \"Mod\".", Default = "Mod")]
    public string GameBananaModType { get; set; }
    
    [Option(SetName = "GameBanana", HelpText = $"[{nameof(DownloadSource.GameBanana)} Specific] Unique identifier for the individual mod. This is the last number of a GameBanana Mod Page URL. e.g. https://gamebanana.com/mods/150118 -> 150118")]
    public int GameBananaItemId { get; set; }
}