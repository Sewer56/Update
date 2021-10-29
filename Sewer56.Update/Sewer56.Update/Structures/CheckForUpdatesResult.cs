using System.Collections.Generic;
using NuGet.Versioning;

namespace Sewer56.Update.Structures;

/// <summary>
/// Result of checking for updates.
/// </summary>
public class CheckForUpdatesResult
{
    /// <summary>
    /// All available package versions.
    /// </summary>
    public IReadOnlyList<NuGetVersion> Versions { get; }

    /// <summary>
    /// Last available package version.
    /// Null if there are no available packages.
    /// </summary>
    public NuGetVersion? LastVersion { get; }

    /// <summary>
    /// Whether there is a package with higher version than the current version.
    /// </summary>
    public bool CanUpdate { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="CheckForUpdatesResult"/>.
    /// </summary>
    public CheckForUpdatesResult(IReadOnlyList<NuGetVersion> versions, NuGetVersion? lastVersion, bool canUpdate)
    {
        Versions = versions;
        LastVersion = lastVersion;
        CanUpdate = canUpdate;
    }
}