using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Extractors.SharpCompress;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Packaging.Structures.ReleaseBuilder;
using Sewer56.Update.Resolvers.NuGet;
using SharpCompress.Common;
using SharpCompress.Writers;
using Xunit;
using Xunit.Abstractions;

namespace Sewer56.Update.Tests.Extractors;

/// <summary>
/// Tests for the NuGet Package Builder.
/// </summary>
public class NuGetPackageExtractorTests
{
    public string PackageFolder = Path.Combine(Assets.TempFolder, "Package");
    public string OutputFolder = Path.Combine(Assets.TempFolder, "Output");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public NuGetPackageExtractorTests(ITestOutputHelper testOutputHelper)
    {
        IOEx.TryDeleteDirectory(Assets.TempFolder);
    }

    [Fact]
    public async Task NuGetArchiver_CanArchive()
    {
        // Arrange
        var extractor = new NuGetPackageExtractor();
        var builder   = new ReleaseBuilder<Empty>();

        var allFiles = Directory.GetFiles(Assets.ManyFileFolderOriginal, ".", SearchOption.AllDirectories);
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
            PackageArchiver = new NuGetPackageArchiver(new NuGetPackageArchiverSettings()
            {
                Id = "NuGet.Package",
                Authors = new List<string>() { "Sewer56" },
                Description = "No"
            })
        });

        // Assert
        Assert.Single(metadata.Releases);
        var release = metadata.Releases[0];

        var filePath = Path.Combine(OutputFolder, release.FileName);
        Assert.True(File.Exists(filePath));
        Assert.EndsWith(".nupkg", release.FileName);

        // Can decompress
        var extractedPath = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(release.FileName), "out");
        await extractor.ExtractPackageAsync(filePath, extractedPath);

        foreach (var file in allFiles)
        {
            var relativePath = Paths.GetRelativePath(file, Assets.ManyFileFolderOriginal);
            var newPath = Paths.AppendRelativePath(relativePath, extractedPath);
            Assert.True(File.Exists(newPath));
        }
    }

    [Fact]
    public async Task NuGetArchiver_CanExtractLegacyPackage()
    {
        // Arrange
        var extractor     = new NuGetPackageExtractor();
        var allFiles      = Directory.GetFiles(Assets.NuGetLegacyPackageOriginalFiles, ".", SearchOption.AllDirectories);
        var packagePath   = Assets.NuGetLegacyPackage;
        var extractedPath = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(packagePath), "out");
        
        // Act
        await extractor.ExtractPackageAsync(packagePath, extractedPath);

        // Assert
        foreach (var file in allFiles)
        {
            var relativePath = Paths.GetRelativePath(file, Assets.NuGetLegacyPackageOriginalFiles);
            var newPath = Paths.AppendRelativePath(relativePath, extractedPath);
            Assert.True(File.Exists(newPath));
        }
    }
}