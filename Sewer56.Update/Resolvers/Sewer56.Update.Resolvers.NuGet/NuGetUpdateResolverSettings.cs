using System;
using System.Collections.Generic;
using System.Text;
using Sewer56.Update.Resolvers.NuGet.Utilities;

namespace Sewer56.Update.Resolvers.NuGet;

/// <summary>
/// Allows you to specify the settings for the NuGet update resolver.
/// </summary>
public class NuGetUpdateResolverSettings
{
    /// <summary>
    /// Stores the ID of the package to obtain.
    /// </summary>
    public string? PackageId;

    /// <summary>
    /// Provides access to the NuGet server to get the package details from.
    /// </summary>
    public NugetRepository? NugetRepository;

    /// <summary>
    /// Allow for the grabbing of unlisted packages.
    /// </summary>
    public bool AllowUnlisted;

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
}