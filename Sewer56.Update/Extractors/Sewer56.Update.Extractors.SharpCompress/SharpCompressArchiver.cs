using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging.Interfaces;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace Sewer56.Update.Extractors.SharpCompress;

/// <summary>
/// An implementation that compresses packages using SharpCompress.
/// </summary>
public class SharpCompressArchiver : IPackageArchiver
{
    private WriterOptions _writerOptions;
    private ArchiveType _archiveType;

    /// <summary/>
    /// <param name="writerOptions">Options for the archive writer.</param>
    /// <param name="archiveType">The archive type to create.</param>
    public SharpCompressArchiver(WriterOptions writerOptions, ArchiveType archiveType)
    {
        _writerOptions = writerOptions;
        _archiveType = archiveType;
    }

    /// <inheritdoc />
    public Task CreateArchiveAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        using Stream stream = File.OpenWrite(destPath);
        using var writer = WriterFactory.Open(stream, _archiveType, _writerOptions);

        for (var x = 0; x < relativeFilePaths.Count; x++)
        {
            progress?.Report((float) x / relativeFilePaths.Count);
            var fullPath = Paths.AppendRelativePath(relativeFilePaths[x], baseDirectory);
            writer.Write(relativeFilePaths[x], fullPath);
        }

        // Write Format to End of Stream
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string GetFileExtension()
    {
        return _archiveType switch
        {
            ArchiveType.Rar => ".rar",
            ArchiveType.Zip => ".zip",
            ArchiveType.Tar => $".tar{GetTarCompressionTypeExtension()}",
            ArchiveType.SevenZip => ".7z",
            ArchiveType.GZip => ".gz",
            _ => ".bin"
        };
    }

    private string GetTarCompressionTypeExtension()
    {
        return _writerOptions.CompressionType switch
        {
            CompressionType.GZip => ".gz",
            CompressionType.BZip2 => ".bz2",
            CompressionType.LZip => ".lz",
            CompressionType.Xz => ".xz",
            _ => ""
        };
    }
}