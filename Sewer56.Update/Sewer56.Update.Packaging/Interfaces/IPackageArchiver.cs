using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
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
    /// <param name="extras">Package metadata. Use for extra metadata if the archive format itself (e.g. NuGet) supports versioning.</param>
    /// <param name="progress">Reports progress back.</param>
    /// <param name="cancellationToken">Can be used to cancel the operation.</param>
    Task CreateArchiveAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, CreateArchiveExtras extras, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file extension for a given archive format.
    /// </summary>
    /// <returns>The file extension, including dot.</returns>
    string GetFileExtension();

    /// <summary>
    /// Gets the file size of all files provided combined.
    /// </summary>
    /// <param name="relativeFilePaths">List of relative paths of files to compress.</param>
    /// <param name="baseDirectory">The base directory to which these paths are relative to.</param>
    /// <returns>The total file size of all files.</returns>
    public static long GetTotalFileSize(List<string> relativeFilePaths, string baseDirectory)
    {
        long totalSize = 0;

        foreach (var relativeFilePath in relativeFilePaths)
        {
            var fileInfo = new FileInfo(Paths.AppendRelativePath(relativeFilePath, baseDirectory));
            totalSize += fileInfo.Length;
        }

        return totalSize;
    }
}

/// <summary>
/// Optional components of interest passed onto the <see cref="IPackageArchiver"/> interface methods.
/// </summary>
public class CreateArchiveExtras
{
    /// <summary>
    /// Metadata of the package to be compressed.
    /// Can be used for extra metadata if the archive format itself (e.g. NuGet) supports versioning.
    /// </summary>
    public PackageMetadata Metadata { get; set; } = new PackageMetadata();

    /// <summary>
    /// Total size of all uncompressed files included in the package.
    /// </summary>
    public long TotalUncompressedSize { get; set; }
}