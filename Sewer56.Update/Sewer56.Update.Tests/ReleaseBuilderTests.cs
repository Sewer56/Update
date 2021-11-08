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
using Sewer56.Update.Resolvers.GameBanana;
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
            OutputFolder = this.OutputFolder,
        });

        // Assert
        Assert.True(metadata.Releases.Count > 0);
        foreach (var release in metadata.Releases)
        {
            Assert.True(File.Exists(Path.Combine(OutputFolder, release.FileName)));
        }
    }

    [Fact]
    public async Task Build_CanBuildExistingPackageRelease()
    {
        const string PackageExtraData = "This package is cool!";
        const string ExistingPackageVer = "1.0.1";
        List<string> ignoreRegexes = new List<string>()
        {
            ".*Neon.*Genesis.*Evangelion.*",
        };

        var existingPackagePath = Path.Combine(PackageFolder, ExistingPackageVer);

        // Arrange
        var existingPackage = await Package<string>.CreateAsync(Assets.ManyFileFolderOriginal, existingPackagePath, ExistingPackageVer, PackageExtraData, ignoreRegexes);

        var builder = new ReleaseBuilder<string>();
        builder.AddExistingPackage(new ExistingPackageBuilderItem()
        {
            Path = existingPackagePath
        });

        // Act
        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        // Assert
        Assert.Single(metadata.Releases);
        foreach (var release in metadata.Releases)
            Assert.True(File.Exists(Path.Combine(OutputFolder, release.FileName)));

        Assert.Equal(ExistingPackageVer, metadata.Releases[0].Version);
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

    [Fact]
    public async Task Build_GameBanana_SupportsFileNameLengthFilter()
    {
        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderTarget,
            Version = "1.0.0"
        });

        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderOriginal,
            Version = "1.0.0-pre" // Oh no, Monika messed something up!
        });

        // Act
        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "SuperDuperCoolPackageBamKaWham",
            OutputFolder = this.OutputFolder,
            FileNameFilter = GameBananaUtilities.SanitizeFileName
        });

        // Assert
        Assert.Equal(2, metadata.Releases.Count);
        Assert.Equal(2, metadata.Releases.Select(x => x.FileName).ToHashSet().Count); // All files are unique.
        foreach (var release in metadata.Releases)
        {
            Assert.True(release.FileName.Length <= GameBananaUtilities.MaxUnmodifiedFileNameLength);
            Assert.True(File.Exists(Path.Combine(OutputFolder, release.FileName)));
        }
    }

}