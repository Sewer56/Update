using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Versioning;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Extensions;

/// <summary>
/// Extensions to help you work with collections of NuGet packages.
/// </summary>
public static class NuGetExtensions
{
    /// <summary>
    /// Converts all <see cref="ReleaseMetadata"/> Release version into NuGet Versions.
    /// </summary>
    public static List<NuGetVersion> GetNuGetVersionsFromReleaseMetadata(this ReleaseMetadata metadata, bool allowPrereleases)
    {
        var nugetVersions = new List<NuGetVersion>(metadata.Releases.Count);
        foreach (var release in metadata.Releases)
        {
            var nuGetVersion = new NuGetVersion(release.Version);
            if (nuGetVersion.IsPrerelease && !allowPrereleases)
                continue;

            nugetVersions.Add(nuGetVersion);
        }

        nugetVersions.Sort((a, b) => a.CompareTo(b)); // Sort ascending.
        return nugetVersions;
    }
}