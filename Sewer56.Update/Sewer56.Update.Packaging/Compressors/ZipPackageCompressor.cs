using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;

namespace Sewer56.Update.Packaging.Compressors;

/// <summary>
/// Compresses packages into a zip file using the DEFLATE compression algorithm.
/// </summary>
public class ZipPackageCompressor : IPackageCompressor
{
    /// <inheritdoc />
    public async Task CompressPackageAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        await using var zipStream = new FileStream(destPath, FileMode.Create);
        using var zipFile  = new ZipArchive(zipStream, ZipArchiveMode.Update);
        var progressSlicer = new ProgressSlicer(progress);
        for (var x = 0; x < relativeFilePaths.Count; x++)
        {
            var progressForFile = progressSlicer.Slice((float)x / relativeFilePaths.Count);
            var relativePath    = relativeFilePaths[x];

            var entry = zipFile.CreateEntry(relativePath, CompressionLevel.Optimal);
            await using var sourceStream = File.Open(Paths.AppendRelativePath(relativePath, baseDirectory), FileMode.Open, FileAccess.Read);
            await using var entryStream  = entry.Open();

            await sourceStream.CopyToAsyncEx(entryStream, 65536, progressForFile, cancellationToken);
        }

        progress?.Report(1);
    }
}