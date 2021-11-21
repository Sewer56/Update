using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Structures;

namespace Sewer56.Update.Extensions;

/// <summary>
/// Utility extensions for <see cref="UpdateManager{T}"/>.
/// </summary>
public static class UpdateManagerExtensions
{
    /// <summary>
    /// Checks for new version and performs an update if available.
    /// </summary>
    public static async Task<bool> CheckPerformUpdateAsync(this IUpdateManager manager)
    {
        return await CheckPerformUpdateAsync(manager, null, null, null);
    }

    /// <summary>
    /// Checks for new version and performs an update if available.
    /// </summary>
    public static async Task<bool> CheckPerformUpdateAsync(this IUpdateManager manager, OutOfProcessOptions? outOfProcessOptions, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        return await CheckPerformUpdateAsync(manager, outOfProcessOptions, null, progress, cancellationToken);
    }

    /// <summary>
    /// Checks for new version and performs an update if available.
    /// </summary>
    public static async Task<bool> CheckPerformUpdateAsync(this IUpdateManager manager, OutOfProcessOptions? outOfProcessOptions, UpdateOptions? updateOptions = null, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        updateOptions ??= new UpdateOptions();
        outOfProcessOptions ??= new OutOfProcessOptions();

        // Check
        var result = await manager.CheckForUpdatesAsync(cancellationToken);
        if (!result.CanUpdate || result.LastVersion == null)
            return false;

        // Prepare
        await manager.PrepareUpdateAsync(result.LastVersion, progress, cancellationToken);

        // Apply
        var needsRestart = await manager.StartUpdateAsync(result.LastVersion, outOfProcessOptions, updateOptions);
        return needsRestart;
    }
}