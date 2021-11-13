using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Packaging.Compressors;

/// <summary>
/// Compresses packages into a zip file using the DEFLATE compression algorithm.
/// </summary>
public class ZipPackageArchiver : IPackageArchiver
{
    /// <inheritdoc />
    public async Task CreateArchiveAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, CreateArchiveExtras extras, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        await using var zipStream = new FileStream(destPath, FileMode.Create);
        using var zipFile  = new ZipArchive(zipStream, ZipArchiveMode.Create);
        var progressSlicer = new ProgressSlicer(progress);
        
        for (var x = 0; x < relativeFilePaths.Count; x++)
        {
            var relativePath    = relativeFilePaths[x];

            var entry = zipFile.CreateEntry(relativePath, CompressionLevel.Optimal);
            await using var sourceStream = File.Open(Paths.AppendRelativePath(relativePath, baseDirectory), FileMode.Open, FileAccess.Read);
            await using var entryStream  = entry.Open();

            var progressForFile = progressSlicer.Slice((double) sourceStream.Length / extras.TotalUncompressedSize);
            await sourceStream.CopyToAsyncEx(entryStream, 131072, progressForFile, cancellationToken);
        }
    }

    /// <inheritdoc />
    public string GetFileExtension() => ".zip";
}