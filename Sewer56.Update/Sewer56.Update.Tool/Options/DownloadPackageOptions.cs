using System;
using CommandLine;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Options;

/// <summary>
/// Options for downloading a package from the command line.
/// </summary>

[Verb("DownloadPackage", HelpText = "Downloads the latest package from a specified source.")]
internal class DownloadPackageOptions : IGitHubReleasesDownloadOptions, INuGetDownloadOptions, IGameBananaDownloadOptions
{
    [Option(Required = true, HelpText = $"Where the downloaded package file should be saved. This is a folder if {nameof(Extract)} is true, else the file name.")]
    public string OutputPath { get; set; }

    [Option(Required = false, HelpText = "Extracts the package using the default extractor. 7z for Windows, else SharpCompress.", Default = false)]
    public bool Extract { get; set; }

    [Option(Required = true, HelpText = "Where the package should be downloaded from.", Default = DownloadSource.GitHub)]
    public DownloadSource Source { get; set; }

    [Option(Required = false, HelpText = "The file name to be used for the Release Metadata File.", Default = ReleaseMetadata.DefaultFileName)]
    public string MetadataFileName { get; set; }

    [Option(Required = false, HelpText = "Set to true to download latest pre-release.", Default = false)]
    public bool? AllowPrereleases { get; set; }

    /* GitHub Specific */
    public string GitHubUserName { get; set; }
    public string GitHubRepositoryName { get; set; }
    public string GitHubLegacyFallbackPattern { get; set; }
    public bool? GitHubInheritVersionFromTag { get; set; }

    /* NuGet Specific */
    public string NuGetPackageId { get; set; }
    public string NuGetFeedUrl { get; set; }
    public bool? NuGetAllowUnlisted { get; set; }

    /* NuGet Specific */
    public string GameBananaModType { get; set; }
    public int GameBananaItemId { get; set; }
}

public enum DownloadSource
{
    GitHub,
    NuGet,
    GameBanana
}