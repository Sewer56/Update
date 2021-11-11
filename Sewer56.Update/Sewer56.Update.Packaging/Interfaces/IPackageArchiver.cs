using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Packaging.Interfaces;

/// <summary>
/// Provider for archiving packages.
/// </summary>
public interface IPackageArchiver
{
    /// <summary>
    /// Extracts contents of the given package to the given output directory.
    /// </summary>
    /// <param name="relativeFilePaths">List of relative paths of files to compress.</param>
    /// <param name="baseDirectory">The base directory to which these paths are relative to.</param>
    /// <param name="destPath">The path where to save the package.</param>
    /// <param name="metadata">Package metadata. Use for extra metadata if the archive format itself (e.g. NuGet) supports versioning.</param>
    /// <param name="progress">Reports progress back.</param>
    /// <param name="cancellationToken">Can be used to cancel the operation.</param>
    Task CreateArchiveAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, PackageMetadata metadata, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file extension for a given archive format.
    /// </summary>
    /// <returns>The file extension, including dot.</returns>
    string GetFileExtension();
}