using Sewer56.Update.Resolvers.NuGet.Utilities;

namespace Sewer56.Update.Resolvers.NuGet;

/// <summary>
/// Allows you to specify the settings for the NuGet update resolver.
/// </summary>
public partial class NuGetUpdateResolverSettings
{
    /// <summary>
    /// Stores the ID of the package to obtain.
    /// </summary>
    public string? PackageId { get; set; }

    /// <summary>
    /// Provides access to the NuGet server to get the package details from.
    /// </summary>
    public NugetRepository? NugetRepository { get; set; }

    /// <summary>
    /// Allow for the grabbing of unlisted packages.
    /// </summary>
    public bool AllowUnlisted { get; set; }

    /// <summary/>
    /// <param name="packageId">The ID of the individual package.</param>
    /// <param name="nugetRepository">The repository from which to get the packages from.</param>
    public NuGetUpdateResolverSettings(string? packageId, NugetRepository? nugetRepository)
    {
        PackageId = packageId;
        NugetRepository = nugetRepository;
    }
    
    /// <summary/>
    public NuGetUpdateResolverSettings()
    {

    }

    /// <summary>
    /// Performs a shallow clone of the current resolver's settings.
    /// </summary>
    public NuGetUpdateResolverSettings Clone()
    {
        return new NuGetUpdateResolverSettings
        {
            PackageId = this.PackageId,
            NugetRepository = this.NugetRepository,
            AllowUnlisted = this.AllowUnlisted
        };
    }
}