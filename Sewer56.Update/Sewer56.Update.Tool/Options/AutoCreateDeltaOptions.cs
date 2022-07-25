using CommandLine;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Options;

[Verb("AutoCreateDelta", HelpText = "Automatically downloads packages and creates deltas for the last X releases. Paths of generated packages are written to standard output/console.")]
internal class AutoCreateDeltaOptions : IPackageResolverOptions, ICurrentPackageDetails
{
    [Option(Required = false, HelpText = "Number of previous releases to download and make deltas for.", Default = 1)]
    public int NumReleases { get; set; }

    [Option(Required = true, HelpText = "The folder where the generated delta packages should be saved.")]
    public string OutputPath { get; set; }

    /* ICurrentPackageDetails */
    public string FolderPath { get; set; }
    public string Version { get; set; }
    public string IgnoreRegexesPath { get; set; }
    public string IncludeRegexesPath { get; set; }

    /* IPackageResolverOptions: Options */
    public DownloadSource Source { get; set; }
    public string MetadataFileName { get; set; }
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

    /* GameBanana Specific */
    public string GameBananaModType { get; set; }
    public int GameBananaItemId { get; set; }
}