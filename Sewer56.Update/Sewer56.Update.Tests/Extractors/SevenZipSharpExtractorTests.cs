using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using SevenZip;
using Sewer56.Update.Extractors.SevenZipSharp;

namespace Sewer56.Update.Tests.Extractors;

public class SevenZipSharpExtractorTests
{
    public string PackageFolder = Path.Combine(Assets.TempFolder, "Package");
    public string OutputFolder = Path.Combine(Assets.TempFolder, "Output");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public SevenZipSharpExtractorTests(ITestOutputHelper testOutputHelper)
    {
        IOEx.TryDeleteDirectory(Assets.TempFolder);
    }

    [Fact]
    public async Task SevenZipCompressor_CanCompress()
    {
        var supportedFormats = new List<(OutArchiveFormat, List<(CompressionMethod, string)>)>()
        {
            {
                (OutArchiveFormat.SevenZip, new List<(CompressionMethod, string)>()
                {
                    (CompressionMethod.Copy, ".7z"),
                    (CompressionMethod.Deflate, ".7z"),
                    (CompressionMethod.Deflate64, ".7z"),
                    (CompressionMethod.Lzma, ".7z"),
                    (CompressionMethod.Lzma2, ".7z"),
                    (CompressionMethod.Ppmd, ".7z"),
                })
            },

            {
                (OutArchiveFormat.Zip, new List<(CompressionMethod, string)>()
                {
                    (CompressionMethod.Copy, ".zip"),
                    (CompressionMethod.Deflate, ".zip"),
                    (CompressionMethod.Deflate64, ".zip"),
                    (CompressionMethod.Lzma, ".zip"),
                    (CompressionMethod.Ppmd, ".zip"),
                })
            },
        };

        var decompressor = new SevenZipSharpExtractor();
        foreach (var supportedFormat in supportedFormats)
        {
            foreach (var compressionAndExtension in supportedFormat.Item2)
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
                    PackageArchiver = new SevenZipSharpArchiver(new SevenZipSharpArchiverSettings()
                    {
                        CompressionLevel = CompressionLevel.Ultra,
                        ArchiveFormat = supportedFormat.Item1,
                        CompressionMethod = compressionAndExtension.Item1
                    })
                });

                // Assert
                Assert.True(metadata.Releases.Count > 0);
                foreach (var release in metadata.Releases)
                {
                    var filePath = Path.Combine(OutputFolder, release.FileName);
                    Assert.True(File.Exists(filePath));
                    Assert.EndsWith(compressionAndExtension.Item2, release.FileName);

                    // Can decompress
                    var extractedPath = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(release.FileName), "out");
                    await decompressor.ExtractPackageAsync(filePath, extractedPath);
                }
            }
        }
    }
}

