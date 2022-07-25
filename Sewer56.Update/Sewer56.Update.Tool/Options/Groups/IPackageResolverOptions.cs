using CommandLine;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Tool.Options.Groups;

internal interface IPackageResolverOptions : IGitHubReleasesDownloadOptions, INuGetDownloadOptions, IGameBananaDownloadOptions
{
    [Option(Required = true, HelpText = "Where the package should be downloaded from.", Default = DownloadSource.GitHub)]
    DownloadSource Source { get; set; }

    [Option(Required = false, HelpText = "The file name to be used for the Release Metadata File.", Default = ReleaseMetadata.DefaultFileName)]
    string MetadataFileName { get; set; }

    [Option(Required = false, HelpText = "Set to true to download latest pre-release.", Default = false)]
    bool? AllowPrereleases { get; set; }
}