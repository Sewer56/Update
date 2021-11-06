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
    /// Set to true to allow prereleases.
    /// </summary>
    public bool AllowPrereleases { get; set; } = false;
}