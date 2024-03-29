﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Exceptions;
using Sewer56.Update.Packaging.Extractors;
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
    public async Task Build_WithoutVersionSuffix()
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
            OutputFolder = this.OutputFolder,
            DontAppendVersionToPackages = true
        });

        // Assert Packages Exist
        Assert.True(metadata.Releases.Count > 0);
        foreach (var release in metadata.Releases)
        {
            Assert.True(File.Exists(Path.Combine(OutputFolder, release.FileName)));
        }

        // Assert No Version in Name
        Assert.DoesNotContain(metadata.Releases, item => item.FileName.Contains("1.0"));
    }

    [Fact]
    public async Task Build_WithoutVersionSuffix_WithMultiCopyPackages_ThrowsBuilderValidationFailedException()
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

        // Act
        await Assert.ThrowsAsync<BuilderValidationFailedException>(() => builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder,
            DontAppendVersionToPackages = true
        }));
    }

    [Fact]
    public async Task Build_WithoutVersionSuffix_IgnoresDeltaPackages()
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
            DontAppendVersionToPackages = true
        });

        // Assert No Version in Name
        Assert.Contains(metadata.Releases, item => item.FileName.Contains("1.0.1"));
    }

    [Fact]
    public async Task Build_WithoutVersionSuffix_CountsDeltasInExistingPackages()
    {
        // Arrange
        var builder = new ReleaseBuilder<Empty>();

        // => Add 1 copy/regular package and a delta existing package.
        // If ExistingPackageBuilderItems are counted properly, this should not throw.
        using var existingPackageOne = new TemporaryFolderAllocation();
        await Package<Empty>.CreateAsync(Assets.ManyFileFolderOriginal, existingPackageOne.FolderPath, "1.0.0");

        builder.AddExistingPackage(new ExistingPackageBuilderItem()
        {
            Path = existingPackageOne.FolderPath
        });

        using var existingDeltaPackageOne = new TemporaryFolderAllocation();
        await Package<Empty>.CreateDeltaAsync(Assets.ManyFileFolderOriginal, Assets.ManyFileFolderTarget, existingDeltaPackageOne.FolderPath, "1.0.0", "1.0.1");

        builder.AddExistingPackage(new ExistingPackageBuilderItem()
        {
            Path = existingDeltaPackageOne.FolderPath
        });

        // Doesn't throw.
        await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder,
            DontAppendVersionToPackages = true
        });

        // Add another copy/regular package
        using var existingPackageTwo = new TemporaryFolderAllocation();
        await Package<Empty>.CreateAsync(Assets.ManyFileFolderTarget, existingPackageTwo.FolderPath, "1.0.1");
        builder.AddExistingPackage(new ExistingPackageBuilderItem()
        {
            Path = existingPackageTwo.FolderPath
        });

        // It should throw now!
        await Assert.ThrowsAsync<BuilderValidationFailedException>(() => builder.BuildAsync(new BuildArgs()
        {
            FileName = "PackageThatThrows",
            OutputFolder = this.OutputFolder,
            DontAppendVersionToPackages = true
        }));
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

    [Fact]
    public async Task BuildRelease_CanIgnoreFile()
    {
        var extractor = new ZipPackageExtractor();
        using var packageAssets = new TemporaryFolderAllocation();
        using var extractedAssets = new TemporaryFolderAllocation();
        IOEx.CopyDirectory(Assets.ManyFileFolderOriginal, packageAssets.FolderPath);

        var textFileName = "text.json";
        await using var textFile = File.Create(Path.Combine(packageAssets.FolderPath, textFileName));
        await textFile.DisposeAsync();

        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = packageAssets.FolderPath,
            Version = "1.0",
            IgnoreRegexes = new List<string>()
            {
                @".*\.json"
            }
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        // Act
        var releaseItem = metadata.GetRelease("1.0", new ReleaseMetadataVerificationInfo()
        {
            FolderPath = OutputFolder
        });

        await extractor.ExtractPackageAsync(Path.Combine(OutputFolder, releaseItem.FileName), extractedAssets.FolderPath);
        var packageMetadata = await PackageMetadata<Empty>.ReadFromDirectoryAsync(extractedAssets.FolderPath);

        // Assert
        var filePath = Path.Combine(extractedAssets.FolderPath, textFileName);
        Assert.False(File.Exists(filePath));
        Assert.Equal(-1, packageMetadata.Hashes.Files.FindIndex(entry => Path.GetFileName(entry.RelativePath).Equals(textFileName, StringComparison.OrdinalIgnoreCase)));
        Assert.True(packageMetadata.Verify(out var _, out var _));
    }

    [Fact]
    public async Task BuildRelease_CanUnignoreFile()
    {
        var extractor = new ZipPackageExtractor();
        using var packageAssets = new TemporaryFolderAllocation();
        using var extractedAssets = new TemporaryFolderAllocation();
        IOEx.CopyDirectory(Assets.ManyFileFolderOriginal, packageAssets.FolderPath);

        var textFileName = "Cool.json";
        await using var textFile = File.Create(Path.Combine(packageAssets.FolderPath, textFileName));
        await textFile.DisposeAsync();

        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = packageAssets.FolderPath,
            Version = "1.0",
            IgnoreRegexes = new List<string>()
            {
                @".*\.json"
            },
            IncludeRegexes = new List<string>()
            {
                @"Cool\.json"
            }
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        // Act
        var releaseItem = metadata.GetRelease("1.0", new ReleaseMetadataVerificationInfo()
        {
            FolderPath = OutputFolder
        });

        await extractor.ExtractPackageAsync(Path.Combine(OutputFolder, releaseItem.FileName), extractedAssets.FolderPath);
        var packageMetadata = await PackageMetadata<Empty>.ReadFromDirectoryAsync(extractedAssets.FolderPath);

        // Assert
        var filePath = Path.Combine(extractedAssets.FolderPath, textFileName);
        Assert.True(File.Exists(filePath));
        Assert.NotEqual(-1, packageMetadata.Hashes.Files.FindIndex(entry => Path.GetFileName(entry.RelativePath).Equals(textFileName, StringComparison.OrdinalIgnoreCase)));
        Assert.True(packageMetadata.Verify(out var _, out var _));
    }

    [Fact]
    public async Task Build_CanAddToExistingPackage()
    {
        // Arrange
        // Build Initial Release
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderOriginal,
            Version = "1.0"
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        // Act
        // Add another release.
        builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderTarget,
            Version = "1.0.1" // Oh no, Monika messed something up!
        });

        metadata = await builder.BuildAsync(new BuildArgs()
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
    public async Task Build_CanAddToExistingPackage_WithAutoGenerateDelta()
    {
        // Arrange
        // Build Initial Release
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderOriginal,
            Version = "1.0"
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        // Act
        // Add another release.
        builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderTarget,
            Version = "1.0.1" // Oh no, Monika messed something up!
        });

        metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder,
            AutoGenerateDelta = true
        });

        // Assert
        Assert.Equal(3, metadata.Releases.Count);
        Assert.Equal(3, metadata.Releases.Select(x => x.FileName).ToHashSet().Count); // All files are unique.
        foreach (var release in metadata.Releases)
        {
            Assert.True(File.Exists(Path.Combine(OutputFolder, release.FileName)));
        }
    }


    [Fact]
    public async Task Build_CanAddToExistingPackage_WithAutoGenerateDelta_AndExistingPackage()
    {
        // Arrange
        // Build Initial Release
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderOriginal,
            Version = "1.0"
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        // Act
        // Add another release.
        builder = new ReleaseBuilder<Empty>();
        using var newPackageAllocation = new TemporaryFolderAllocation();
        await Package<Empty>.CreateAsync(Assets.ManyFileFolderTarget, newPackageAllocation.FolderPath, "1.0.1");

        builder.AddExistingPackage(new ExistingPackageBuilderItem()
        {
            Path = newPackageAllocation.FolderPath
        });

        metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder,
            AutoGenerateDelta = true
        });

        // Assert
        Assert.Equal(3, metadata.Releases.Count);
        Assert.Equal(3, metadata.Releases.Select(x => x.FileName).ToHashSet().Count); // All files are unique.
        foreach (var release in metadata.Releases)
        {
            Assert.True(File.Exists(Path.Combine(OutputFolder, release.FileName)));
        }
    }
}