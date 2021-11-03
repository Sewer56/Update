using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.IO;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Sewer56.Update.Extractors.SharpCompress;

/// <summary>
/// A file extractor based on the SharpCompress library by Adam Hathcock.
/// For supported formats, refer to <a href="https://github.com/adamhathcock/sharpcompress/blob/master/FORMATS.md"/>.
/// </summary>
public class SharpCompressExtractor : IPackageExtractor
{
    private static ExtractionOptions _options = new ExtractionOptions()
    {
        ExtractFullPath = true,
        Overwrite = true
    };

    /// <inheritdoc />
    public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var createdDirectorySet = new CreatedDirectorySet();
        await using var fileStream = File.Open(sourceFilePath, FileMode.Open);
        using IReader? reader = ReaderFactory.Open(fileStream);

        long totalStreamSize = fileStream.Length;
        var progressSlicer   = new ProgressSlicer(progress);

        while (reader.MoveToNextEntry())
        {
            if (reader.Entry.IsDirectory || reader.Entry.CompressedSize <= 0)
                continue;

            await using var decompStream = reader.OpenEntryStream();
            var fileProgress = progressSlicer.Slice((double) reader.Entry.CompressedSize / totalStreamSize);

            var fullPath = Path.Combine(destDirPath, reader.Entry.Key);
            createdDirectorySet.CreateDirectoryIfNeeded(Path.GetDirectoryName(fullPath)!);
            
            await using var outputStream = File.Create(fullPath);
            await decompStream.CopyToAsyncEx(outputStream, 524288, fileProgress, cancellationToken);
        }
        
        progress?.Report(1);
    }
}