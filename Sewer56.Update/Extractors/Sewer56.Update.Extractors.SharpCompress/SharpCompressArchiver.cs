using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.Structures;
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
    public Task CreateArchiveAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, CreateArchiveExtras extras, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        using Stream stream = File.OpenWrite(destPath);
        using var writer = WriterFactory.Open(stream, _archiveType, _writerOptions);
        var progressSlicer = new ProgressSlicer(progress);

        for (var x = 0; x < relativeFilePaths.Count; x++)
        {
            var fullPath           = Paths.AppendRelativePath(relativeFilePaths[x], baseDirectory);
            using var sourceStream = File.Open(fullPath, FileMode.Open);
            var slice = progressSlicer.Slice((double) sourceStream.Length / extras.TotalUncompressedSize);
            writer.Write(relativeFilePaths[x], sourceStream);
            slice.Report(1);
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