using CommandLine;
using SevenZip;

namespace Sewer56.Update.Tool.Options.Groups;

public interface ISevenZipSharpOptions
{
    /// <summary>
    /// The format of the file to use.
    /// </summary>
    [Option(SetName = "SevenZipSharp", HelpText = $"[{nameof(Archiver.SevenZipSharp)} Specific] The format of the archive to use.", Default = OutArchiveFormat.SevenZip)]
    public OutArchiveFormat SevenZipSharpArchiveFormat { get; set; }

    /// <summary>
    /// The compression level to use.
    /// </summary>
    [Option(SetName = "SevenZipSharp", HelpText = $"[{nameof(Archiver.SevenZipSharp)} Specific] The compression level to use.", Default = CompressionLevel.Ultra)]
    public CompressionLevel SevenZipSharpCompressionLevel { get; set; }

    /// <summary>
    /// The compression method to use.
    /// </summary>
    [Option(SetName = "SevenZipSharp", HelpText = $"[{nameof(Archiver.SevenZipSharp)} Specific] The compression method to use.", Default = CompressionMethod.Lzma2)]
    public CompressionMethod SevenZipSharpCompressionMethod { get; set; }
}