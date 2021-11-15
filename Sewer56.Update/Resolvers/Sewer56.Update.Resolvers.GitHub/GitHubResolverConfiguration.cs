namespace Sewer56.Update.Resolvers.GitHub;

/// <summary>
/// Stores the configuration for <see cref="GitHubReleaseResolver"/>
/// </summary>
public class GitHubResolverConfiguration
{
    /// <summary>
    /// The user name associated with the resolver.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The repository name associated with the resolver.
    /// </summary>
    public string? RepositoryName { get; set; }

    /// <summary>
    /// Allows you to specify a Wildcard pattern (e.g. *Update.zip) for the file to be downloaded.
    /// This is a fallback used in cases no Release Metadata file can be found.
    /// </summary>
    public string? LegacyFallbackPattern { get; set; } = null;
}