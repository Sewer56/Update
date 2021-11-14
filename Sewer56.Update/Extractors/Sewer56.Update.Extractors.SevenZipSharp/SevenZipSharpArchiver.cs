using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SevenZip;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;

namespace Sewer56.Update.Extractors.SevenZipSharp;

/// <summary>
/// Simple archiver that performs archiving using the 7z wrapper called SevenZipSharp.
/// </summary>
public class SevenZipSharpArchiver : IPackageArchiver
{
    private SevenZipSharpArchiverSettings _settings;

    /// <summary/>
    public SevenZipSharpArchiver(SevenZipSharpArchiverSettings? settings = null)
    {
        _settings = settings ?? new SevenZipSharpArchiverSettings();
    }

    /// <inheritdoc />
    public async Task CreateArchiveAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, CreateArchiveExtras extras, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var compressor = new SevenZipCompressor()
        {
            CompressionMode = CompressionMode.Create,
            IncludeEmptyDirectories = false,
            DirectoryStructure      = true,

            ArchiveFormat = _settings.ArchiveFormat,
            CompressionLevel = _settings.CompressionLevel,
            CompressionMethod = _settings.CompressionMethod,
        };

        compressor.Compressing += (sender, args) => { progress?.Report(args.PercentDone / 100.0); };
        var fullFileNames = relativeFilePaths.Select(x => Path.GetFullPath(Paths.AppendRelativePath(x, baseDirectory))).ToArray();
        await compressor.CompressFilesAsync(destPath, Path.GetFullPath(baseDirectory).Length + 1, fullFileNames);
    }

    /// <inheritdoc />
    public string GetFileExtension()
    {
        return _settings.ArchiveFormat switch
        {
            OutArchiveFormat.SevenZip => ".7z",
            OutArchiveFormat.Zip => ".zip",
            OutArchiveFormat.GZip => ".gz",
            OutArchiveFormat.BZip2 => ".bz2",
            OutArchiveFormat.Tar => ".tar",
            OutArchiveFormat.XZ => ".xz",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

/// <summary>
/// Settings to use with the SevenZipSharp archiver.
/// </summary>
public class SevenZipSharpArchiverSettings
{
    /// <summary>
    /// The format of the file to use.
    /// </summary>
    public OutArchiveFormat ArchiveFormat { get; set; } = OutArchiveFormat.SevenZip;

    /// <summary>
    /// The compression level to use.
    /// </summary>
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Ultra;

    /// <summary>
    /// The compression method to use.
    /// </summary>
    public CompressionMethod CompressionMethod { get; set; } = CompressionMethod.Lzma2;
}