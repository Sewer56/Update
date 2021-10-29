using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging.Interfaces;

namespace Sewer56.Update.Packaging.Compressors;

/// <summary>
/// Compresses packages into a zip file using the DEFLATE compression algorithm.
/// </summary>
public class ZipPackageCompressor : IPackageCompressor
{
    /// <inheritdoc />
    public Task CompressPackageAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        // TODO: Make this actually async.
        using var zipFile = ZipFile.Open(destPath, ZipArchiveMode.Update);
        for (var x = 0; x < relativeFilePaths.Count; x++)
        {
            progress?.Report((float) x / relativeFilePaths.Count);
            var relativePath = relativeFilePaths[x];
            zipFile.CreateEntryFromFile(Paths.AppendRelativePath(relativePath, baseDirectory), relativePath, CompressionLevel.Optimal);
        }

        progress?.Report(1);
        return Task.CompletedTask;
    }
}