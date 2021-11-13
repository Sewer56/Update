using System;
using CommandLine;
using Sewer56.Update.Extractors.SharpCompress;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace Sewer56.Update.Tool.Options.Groups;

internal interface ISharpCompressOptions
{
    [Option(SetName = "SharpCompress", HelpText = $"[{nameof(Archiver.SharpCompress)} Specific] The archive and compression format to use with SharpCompress.", Default = Archiver.Zip)]
    public SharpCompressFormat SharpCompressFormat { get; set; }
}

public enum SharpCompressFormat
{
    // Tar Section
    Tar,
    TarLz,
    TarGz,
    TarBz2,

    // Zip Section
    Zip,
    ZipDeflate,
    ZipBz2,
    ZipLzma,
    ZipPpmd
}

public static class SharpCompressOptionsExtensions
{
    public static SharpCompressArchiver GetArchiver(this SharpCompressFormat format)
    {
        return new SharpCompressArchiver(new WriterOptions(GetCompressionType(format)), GetArchiveType(format));
    }

    public static CompressionType GetCompressionType(this SharpCompressFormat format)
    {
        return format switch
        {
            SharpCompressFormat.Tar => CompressionType.None,
            SharpCompressFormat.TarLz => CompressionType.LZip,
            SharpCompressFormat.TarGz => CompressionType.GZip,
            SharpCompressFormat.TarBz2 => CompressionType.BZip2,
            SharpCompressFormat.Zip => CompressionType.None,
            SharpCompressFormat.ZipDeflate => CompressionType.Deflate,
            SharpCompressFormat.ZipBz2 => CompressionType.BZip2,
            SharpCompressFormat.ZipLzma => CompressionType.LZMA,
            SharpCompressFormat.ZipPpmd => CompressionType.PPMd,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public static ArchiveType GetArchiveType(this SharpCompressFormat format)
    {
        if (format >= SharpCompressFormat.Tar && format <= SharpCompressFormat.TarBz2)
            return ArchiveType.Tar;

        if (format >= SharpCompressFormat.Zip && format <= SharpCompressFormat.ZipPpmd)
            return ArchiveType.Zip;

        throw new ArgumentException("Cannot determine archive type from SharpCompress format.");
    }
}