using CommandLine;

namespace Sewer56.Update.Tool.Options.Groups;

public interface INuGetDownloadOptions
{
    [Option(SetName = "NuGet", HelpText = $"[{nameof(DownloadSource.NuGet)} Specific] ID of the package to be downloaded.")]
    public string NuGetPackageId { get; set; }
    
    [Option(SetName = "NuGet", HelpText = $"[{nameof(DownloadSource.NuGet)} Specific] Address of the NuGet V3 Feed to download the package from. Usually ends with `index.json`")]
    public string NuGetFeedUrl { get; set; }
    
    [Option(SetName = "NuGet", HelpText = $"[{nameof(DownloadSource.NuGet)} Specific] Set to true to allow downloading of unlisted packages.", Default = false)]
    public bool NuGetAllowUnlisted { get; set; }
}