using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Exceptions;
using Sewer56.Update.Packaging.Structures;
using Xunit;
using Xunit.Abstractions;

namespace Sewer56.Update.Tests;

public class ReleaseMetadataBuilderTests
{
    public string PackageFolder = Path.Combine(Assets.TempFolder, "Package");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public ReleaseMetadataBuilderTests()
    {
        IOEx.TryDeleteDirectory(Assets.TempFolder);
    }

    [Fact]
    public async Task ReleaseMetadata_ReleaseInheritsCoreMetadataProperties()
    {
        // Arrange
        var packageOne = await Package<Empty>.CreateAsync(Assets.ManyFileFolderOriginal, $"{PackageFolder}/1.0", "1.0");
        var packageTwo = await Package<Empty>.CreateAsync(Assets.ManyFileFolderOriginal, $"{PackageFolder}/1.1", "1.1");

        // Act
        var builder = new ReleaseMetadataBuilder<Empty>();
        builder.AddPackage(new ReleaseMetadataBuilder<Empty>.ReleaseMetadataBuilderItem()
        {
            FileName = Path.GetDirectoryName(packageOne.FolderPath),
            Package  = packageOne
        });

        builder.AddPackage(new ReleaseMetadataBuilder<Empty>.ReleaseMetadataBuilderItem()
        {
            FileName = Path.GetDirectoryName(packageTwo.FolderPath),
            Package  = packageTwo
        });

        // Assert
        var releaseMetadata = builder.Build();
        Assert.Equal(2, releaseMetadata.Releases.Count);
        Assert.Equal("1.0", releaseMetadata.Releases[0].Version);
        Assert.Equal("1.1", releaseMetadata.Releases[1].Version);

        Assert.Equal(Path.GetDirectoryName(packageOne.FolderPath), releaseMetadata.Releases[0].FileName);
        Assert.Equal(Path.GetDirectoryName(packageTwo.FolderPath), releaseMetadata.Releases[1].FileName);

        Assert.Equal(PackageType.Copy, releaseMetadata.Releases[0].ReleaseType);
        Assert.Equal(PackageType.Copy, releaseMetadata.Releases[1].ReleaseType);
    }

    [Fact]
    public async Task ReleaseMetadata_SupportsDeltaPackages()
    {
        const string OldVersion = "1.0";
        const string NewVersion = "1.0.1";

        // Arrange
        var deltaPackage = await Package<Empty>.CreateDeltaAsync(Assets.ManyFileFolderOriginal, Assets.ManyFileFolderTarget, PackageFolder, NewVersion, OldVersion);

        // Act
        var builder = new ReleaseMetadataBuilder<Empty>();
        builder.AddPackage(new ReleaseMetadataBuilder<Empty>.ReleaseMetadataBuilderItem()
        {
            FileName = Path.GetDirectoryName(deltaPackage.FolderPath),
            Package  = deltaPackage
        });
        
        // Assert
        var releaseMetadata = builder.Build();
        Assert.Single(releaseMetadata.Releases);
        Assert.Equal(PackageType.Delta, releaseMetadata.Releases[0].ReleaseType);
        Assert.NotNull(releaseMetadata.Releases[0].Delta);

        Assert.NotNull(releaseMetadata.Releases[0].Delta.DeltaHashes);
        Assert.Equal(OldVersion, releaseMetadata.Releases[0].Delta.Version);
    }

    [Fact]
    public void ReleaseMetadata_ThrowsIfNameNull()
    {
        // Assert
        var builder = new ReleaseMetadataBuilder<Empty>();
        Assert.Throws<BuilderValidationFailedException>(() =>
        {
            builder.AddPackage(new ReleaseMetadataBuilder<Empty>.ReleaseMetadataBuilderItem()
            {
                Package = new PackageMetadata<Empty>()
            });
        });
    }

    [Fact]
    public void ReleaseMetadata_ThrowsIfPackageNull()
    {
        // Assert
        var builder = new ReleaseMetadataBuilder<Empty>();
        Assert.Throws<BuilderValidationFailedException>(() =>
        {
            builder.AddPackage(new ReleaseMetadataBuilder<Empty>.ReleaseMetadataBuilderItem()
            {
                FileName = "Package"
            });
        });
    }

}