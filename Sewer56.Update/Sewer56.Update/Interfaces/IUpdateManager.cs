using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.Update.Exceptions;
using Sewer56.Update.Structures;

namespace Sewer56.Update.Interfaces;

/// <summary>
/// Interface for <see cref="UpdateManager{T}"/>.
/// </summary>
public interface IUpdateManager : IDisposable
{
    /// <summary>
    /// Information about the item, for which the updates are managed.
    /// </summary>
    ItemMetadata Updatee { get; }

    /// <summary>
    /// Checks for updates.
    /// </summary>
    Task<CheckForUpdatesResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an update to given version has been prepared.
    /// </summary>
    bool IsUpdatePrepared(NuGetVersion version);

    /// <summary>
    /// Gets a list of all prepared updates.
    /// </summary>
    IReadOnlyList<NuGetVersion> GetPreparedUpdates();

    /// <summary>
    /// Prepares an update to specified version.
    /// </summary>
    Task PrepareUpdateAsync(NuGetVersion version, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs an update. If the current application is being the one updated,
    /// it is closed and a new instance of the application is launched to perform the update.
    /// 
    /// Otherwise the update is done in-process.
    /// </summary>
    /// <param name="version">The version to upgrade the package to.</param>
    /// <param name="options">Options to use if the update needs to be performed out of process and application restarted.</param>
    /// <exception cref="FileInUseException">One of the files to be patched is currently in use.</exception>
    /// <returns>Returns true if closing the process is required (out of process), else false.</returns>
    Task<bool> StartUpdateAsync(NuGetVersion version, OutOfProcessOptions options);
}