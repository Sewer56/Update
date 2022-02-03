using CommandLine;

namespace Sewer56.Update.Tool.Options.Groups;

public interface IGitHubReleasesDownloadOptions
{
    [Option(SetName = "GitHub Releases", HelpText = $"[{nameof(DownloadSource.GitHub)} Specific] User name of the user or organisation owning the package.")]
    public string GitHubUserName { get; set; }
    
    [Option(SetName = "GitHub Releases", HelpText = $"[{nameof(DownloadSource.GitHub)} Specific] Repository name of the repository containing the package.")]
    public string GitHubRepositoryName { get; set; }
    
    [Option(SetName = "GitHub Releases", HelpText = $"[{nameof(DownloadSource.GitHub)} Specific] Allows you to specify a Wildcard pattern (e.g. *Update.zip) for the file to be downloaded. This is a fallback used in cases no Release Metadata file can be found.")]
    public string GitHubLegacyFallbackPattern { get; set; }
}