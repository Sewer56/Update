using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;

namespace Sewer56.Update.Packaging.Extractors;

/// <summary>
/// Extracts files from zip-archived packages.
/// </summary>
public class ZipPackageExtractor : IPackageExtractor
{
    /// <inheritdoc />
    public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        // Read the zip
        using var archive = new ZipArchive(new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read), ZipArchiveMode.Read, false);

        // For progress reporting
        var totalBytes = archive.Entries.Sum(e => e.Length);
        var totalBytesCopied = 0L;

        // Loop through all entries
        foreach (var entry in archive.Entries)
        {
            // Get destination paths
            var entryDestFilePath = Path.Combine(destDirPath, entry.FullName);
            var entryDestDirPath  = Path.GetDirectoryName(entryDestFilePath);

            // Create directory
            if (!string.IsNullOrWhiteSpace(entryDestDirPath))
                Directory.CreateDirectory(entryDestDirPath);

            // If the entry is a directory - continue
            if (entry.FullName.Last() == Path.DirectorySeparatorChar || entry.FullName.Last() == Path.AltDirectorySeparatorChar)
                continue;

            // Extract entry
            await using var input  = entry.Open();
            await using var output = File.Create(entryDestFilePath);
            using var buffer = new ArrayRental<byte>(65536);

            int bytesCopied;
            do
            {
                bytesCopied = await input.CopyBufferedToAsync(output, buffer.Array, cancellationToken);
                totalBytesCopied += bytesCopied;
                progress?.Report(1.0 * totalBytesCopied / totalBytes);
            } 
            while (bytesCopied > 0);
        }
    }
}