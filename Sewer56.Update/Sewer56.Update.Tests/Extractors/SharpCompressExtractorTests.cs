using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Extractors.SharpCompress;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Packaging.Structures.ReleaseBuilder;
using SharpCompress.Common;
using SharpCompress.Writers;
using Xunit;
using Xunit.Abstractions;

namespace Sewer56.Update.Tests.Extractors;

public class SharpCompressExtractorTests
{
    public string PackageFolder  = Path.Combine(Assets.TempFolder, "Package");
    public string OutputFolder   = Path.Combine(Assets.TempFolder, "Output");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public SharpCompressExtractorTests(ITestOutputHelper testOutputHelper)
    {
        IOEx.TryDeleteDirectory(Assets.TempFolder);
    }

    [Fact]
    public async Task SharpCompressCompressor_CanCompress()
    {
        var supportedFormats = new List<(ArchiveType, List<CompressionType>)>()
        {
            { 
                (ArchiveType.Tar, new List<CompressionType>() 
                {
                    CompressionType.None,
                    CompressionType.LZip,
                    CompressionType.GZip,
                    CompressionType.BZip2,
                })
            },

            {
                (ArchiveType.Zip, new List<CompressionType>()
                {
                    CompressionType.None,
                    CompressionType.Deflate,
                    CompressionType.BZip2,
                    CompressionType.LZMA,
                    CompressionType.PPMd,
                })
            },
        };

        var decompressor = new SharpCompressExtractor();
        foreach (var supportedFormat in supportedFormats)
        {
            foreach (var compressionType in supportedFormat.Item2)
            {
                IOEx.TryDeleteDirectory(OutputFolder);

                // Arrange
                var builder = new ReleaseBuilder<Empty>();
                builder.AddCopyPackage(new CopyBuilderItem<Empty>()
                {
                    FolderPath = Assets.ManyFileFolderOriginal,
                    Version = "1.0"
                });

                // Act
                var metadata = await builder.BuildAsync(new BuildArgs()
                {
                    FileName = "Package",
                    OutputFolder = this.OutputFolder,
                    PackageArchiver = new SharpCompressArchiver(new WriterOptions(compressionType), supportedFormat.Item1)
                });

                // Assert
                Assert.True(metadata.Releases.Count > 0);
                foreach (var release in metadata.Releases)
                {
                    var filePath = Path.Combine(OutputFolder, release.FileName);
                    Assert.True(File.Exists(filePath));

                    // Can decompress
                    var extractedPath = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(release.FileName), "out");
                    await decompressor.ExtractPackageAsync(filePath, extractedPath);
                }
            }
        }
    }
}