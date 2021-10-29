using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sewer56.Update.Packaging.Interfaces;

/// <summary>
/// Provider for compressing packages.
/// </summary>
public interface IPackageCompressor
{
    /// <summary>
    /// Extracts contents of the given package to the given output directory.
    /// </summary>
    /// <param name="relativeFilePaths">List of relative paths of files to compress.</param>
    /// <param name="baseDirectory">The base directory to which these paths are relative to.</param>
    /// <param name="destPath">The path where to save the package.</param>
    /// <param name="progress">Reports progress back.</param>
    /// <param name="cancellationToken">Can be used to cancel the operation.</param>
    Task CompressPackageAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}