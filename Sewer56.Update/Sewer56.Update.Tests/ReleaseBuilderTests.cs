using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Packaging.Structures.ReleaseBuilder;
using Xunit;

namespace Sewer56.Update.Tests;

public class ReleaseBuilderTests
{
    public string PackageFolder  = Path.Combine(Assets.TempFolder, "Package");
    public string OutputFolder   = Path.Combine(Assets.TempFolder, "Output");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public ReleaseBuilderTests()
    {
        IOEx.TryDeleteDirectory(Assets.TempFolder);
    }

    [Fact]
    public async Task Build_CanBuildSinglePackageRelease()
    {
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
            OutputFolder = this.OutputFolder
        });

        // Assert
        Assert.True(metadata.Releases.Count > 0);
        foreach (var release in metadata.Releases)
        {
            Assert.True(File.Exists(Path.Combine(OutputFolder, release.FileName)));
        }
    }

    [Fact]
    public async Task Build_CanBuildDeltaPackageRelease()
    {
        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        builder.AddDeltaPackage(new DeltaBuilderItem<Empty>()
        {
            Version = "1.0.1",
            FolderPath = Assets.ManyFileFolderTarget,
            
            PreviousVersion = "1.0",
            PreviousVersionFolder = Assets.ManyFileFolderOriginal
        });

        // Act
        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        // Assert
        Assert.True(metadata.Releases.Count > 0);
        foreach (var release in metadata.Releases)
        {
            Assert.True(File.Exists(Path.Combine(OutputFolder, release.FileName)));
        }
    }

    [Fact]
    public async Task Build_CanBuildMultiPackageRelease()
    {
        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderOriginal,
            Version = "1.0"
        });

        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderTarget,
            Version = "1.0.1" // Oh no, Monika messed something up!
        });

        // Act
        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        // Assert
        Assert.Equal(2, metadata.Releases.Count);
        Assert.Equal(2, metadata.Releases.Select(x => x.FileName).ToHashSet().Count); // All files are unique.
        foreach (var release in metadata.Releases)
        {
            Assert.True(File.Exists(Path.Combine(OutputFolder, release.FileName)));
        }
    }

}