using CommandLine;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Options;

/// <summary>
/// Options for downloading a package from the command line.
/// </summary>
[Verb("DownloadPackage", HelpText = "Downloads the latest package from a specified source. Version of the downloaded package is written to standard output/console.")]
internal class DownloadPackageOptions : IDownloadPackageOptions, IPackageResolverOptions, IProgressBarOptions
{
    public string OutputPath { get; set; }

    public bool Extract { get; set; }

    public int ReleaseIndex { get; set; }

    /* Progress Bar Options */
    public bool NoProgressBar { get; set; }

    /* Package Resolver Options */
    public string MetadataFileName { get; set; }

    public bool? AllowPrereleases { get; set; }

    public DownloadSource Source { get; set; }

    /* GitHub Specific */
    public string GitHubUserName { get; set; }
    public string GitHubRepositoryName { get; set; }
    public string GitHubLegacyFallbackPattern { get; set; }
    public bool? GitHubInheritVersionFromTag { get; set; }

    /* NuGet Specific */
    public string NuGetPackageId { get; set; }
    public string NuGetFeedUrl { get; set; }
    public bool? NuGetAllowUnlisted { get; set; }

    /* GameBanana Specific */
    public string GameBananaModType { get; set; }
    public int GameBananaItemId { get; set; }
}

public enum DownloadSource
{
    GitHub,
    NuGet,
    GameBanana
}