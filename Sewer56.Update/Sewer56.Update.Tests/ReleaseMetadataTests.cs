using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Packaging.Structures.ReleaseBuilder;
using Xunit;

namespace Sewer56.Update.Tests;

public class ReleaseMetadataTests
{
    public string PackageFolder  = Path.Combine(Assets.TempFolder, "Package");
    public string OutputFolder   = Path.Combine(Assets.TempFolder, "Output");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public ReleaseMetadataTests()
    {
        IOEx.TryDeleteDirectory(Assets.TempFolder);
    }

    [Fact]
    public async Task GetRelease_CanSelectCorrectRelease()
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
            Version = "1.0.1"
        });

        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.SingleFileFolderOriginal, // Compressed to a single file or smth
            Version = "2.0"
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        // Act
        var releaseItem = metadata.GetRelease("2.0", new ReleaseMetadataVerificationInfo()
        {
            FolderPath = OutputFolder
        });

        // Assert
        Assert.NotNull(releaseItem);
        Assert.Equal("2.0", releaseItem.Version);
    }

    [Fact]
    public async Task GetRelease_CanPrioritiseDeltaPackages()
    {
        var originalVersionPath = Assets.ManyFileFolderOriginal;

        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderTarget,
            Version = "2.0"
        });

        builder.AddDeltaPackage(new DeltaBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderTarget,
            Version    = "2.0",

            PreviousVersion       = "1.0",
            PreviousVersionFolder = originalVersionPath
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName     = "Package",
            OutputFolder = this.OutputFolder
        });

        // Act
        var releaseItem = metadata.GetRelease("2.0", new ReleaseMetadataVerificationInfo()
        {
            FolderPath = originalVersionPath
        });

        // Assert
        Assert.NotNull(releaseItem);
        Assert.Equal("2.0", releaseItem.Version);
        Assert.Equal(PackageType.Delta, releaseItem.ReleaseType);
    }
}