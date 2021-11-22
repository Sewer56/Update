using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Structures;
using Xunit;
using Xunit.Abstractions;

namespace Sewer56.Update.Tests;

public class PackagingTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    public string PackageFolder  = Path.Combine(Assets.TempFolder, "Package");
    public string ResultFolder   = Path.Combine(Assets.TempFolder, "Result");

    public PackagingTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        IOEx.TryDeleteDirectory(Assets.TempFolder);
    }

    [Fact]
    public async Task Create_CreatesPackageMetadata()
    {
        // Act
        await Package<Empty>.CreateAsync(Assets.ManyFileFolderOriginal, PackageFolder, "1.0");

        // Assert
        Assert.True(Package<Empty>.HasMetadata(PackageFolder));
    }

    [Fact]
    public async Task CreateDelta_CreatesPackageMetadata()
    {
        // Act
        await Package<Empty>.CreateDeltaAsync(Assets.ManyFileFolderOriginal, Assets.ManyFileFolderTarget, PackageFolder, "1.0", "1.0.1");

        // Assert
        Assert.True(Package<Empty>.HasMetadata(PackageFolder));
    }

    [Fact]
    public async Task Create_CanDeserialize()
    {
        // Act
        var metadata     = await Package<Empty>.CreateAsync(Assets.ManyFileFolderOriginal, PackageFolder, "1.0");
        var metadataCopy = await Package<Empty>.ReadMetadataFromDirectoryAsync(PackageFolder);

        // Assert
        Assert.Equal(PackageType.Copy, metadataCopy.Type);

        Assert.Equal(metadata.Version, metadataCopy.Version);
        Assert.Equal(metadata.Hashes.Files.Count, metadataCopy.Hashes.Files.Count);
        Assert.Equal(metadata.ExtraData, metadataCopy.ExtraData);
        Assert.Equal(metadata.Type, metadataCopy.Type);
        Assert.Equal(metadata.FolderPath, metadataCopy.FolderPath);
        Assert.Equal(metadata.DeltaData, metadataCopy.DeltaData);
        Assert.Equal(metadata.IgnoreRegexes, metadataCopy.IgnoreRegexes);
        Assert.Equal(metadata.IncludeRegexes, metadataCopy.IncludeRegexes);
    }

    [Fact]
    public async Task CreateDelta_CanDeserialize()
    {
        // Act
        var metadata = await Package<Empty>.CreateDeltaAsync(Assets.ManyFileFolderOriginal, Assets.ManyFileFolderTarget, PackageFolder, "1.0", "1.0.1");
        var metadataCopy = await Package<Empty>.ReadMetadataFromDirectoryAsync(PackageFolder);

        // Assert
        Assert.Equal(PackageType.Delta, metadataCopy.Type);

        Assert.Equal(metadata.Version, metadataCopy.Version);
        Assert.Equal(metadata.Hashes.Files.Count, metadataCopy.Hashes.Files.Count);
        Assert.Equal(metadata.ExtraData, metadataCopy.ExtraData);
        Assert.Equal(metadata.Type, metadataCopy.Type);
        Assert.Equal(metadata.FolderPath, metadataCopy.FolderPath);
        Assert.Equal(metadata.DeltaData.PatchData.FilePathSet.Count, metadataCopy.DeltaData.PatchData.FilePathSet.Count);
        Assert.Equal(metadata.DeltaData.PatchData.AddedFilesSet.Count, metadataCopy.DeltaData.PatchData.AddedFilesSet.Count);
        Assert.Equal(metadata.IgnoreRegexes, metadataCopy.IgnoreRegexes);
        Assert.Equal(metadata.IncludeRegexes, metadataCopy.IncludeRegexes);
    }

    [Fact]
    public async Task Create_SerializesExtraData()
    {
        // Act
        var metadata     = await Package<string>.CreateAsync(Assets.ManyFileFolderOriginal, PackageFolder, "1.0", "Sayonara");
        var metadataCopy = await Package<string>.ReadMetadataFromDirectoryAsync(PackageFolder);

        // Assert
        Assert.NotNull(metadata.ExtraData);
        Assert.Equal(metadata.ExtraData, metadataCopy.ExtraData);
    }

    [Fact]
    public async Task Create_FilesAreCopied()
    {
        // Act
        var metadata = await Package<Empty>.CreateAsync(Assets.ManyFileFolderOriginal, PackageFolder, "1.0");

        // Assert
        foreach (var file in metadata.Hashes.Files)
        {
            var newPath = Path.Combine(PackageFolder, file.RelativePath);
            Assert.True(File.Exists(newPath));
        }
    }

    [Fact]
    public async Task Create_CanIgnoreFilesViaRegex()
    {
        var regexToTest = new List<(string regex, int numFiles, string fileToTest)>()
        {
            ("Sayori", 2, "Sayori/Poem.txt"),    // Remove Sayori Only (Poor Sayori)
            (".*yori", 2, "Sayori/Poem.txt"),    // Remove Sayori Only (Poor Sayori)
            ("Sayo.*", 2, "Sayori/Poem.txt"),    // Remove Sayori Only (Poor Sayori)

            ("Poem.txt", 0, "Natsuki/Poem.txt"), // Remove poems
            (".*\\.txt", 0, "Yuri/Poem.txt"),    // Remove all text files
        };

        foreach (var testItem in regexToTest)
        {
            // Act
            var metadata = await Package<Empty>.CreateAsync(Assets.ManyFileFolderOriginal, PackageFolder, "1.0", null, new List<string>() { testItem.regex });

            // Assert
            Assert.Equal(-1, metadata.Hashes.Files.FindIndex(entry => entry.RelativePath.Contains(testItem.fileToTest)));
            Assert.Equal(testItem.numFiles, metadata.Hashes.Files.Count);
            Assert.False(File.Exists(Path.Combine(PackageFolder, testItem.fileToTest)));

            // Cleanup
            Directory.Delete(PackageFolder, true);
        }
    }

    [Fact]
    public async Task CopyPackage_CanApplyToFolder()
    {
        // Act
        await Package<Empty>.CreateAsync(Assets.ManyFileFolderOriginal, PackageFolder, "1.0");

        var metadata = await Package<Empty>.ReadMetadataFromDirectoryAsync(PackageFolder);
        metadata.Apply(ResultFolder);

        // Assert
        Assert.True(metadata.Verify(out _, out _, ResultFolder));
    }

    [Fact]
    public async Task LegacyPackage_CanApplyToFolder()
    {
        // Act
        IOEx.CopyDirectory(Assets.ManyFileFolderOriginal, PackageFolder); // Manifest-less

        var metadata = await Package<Empty>.ReadOrCreateLegacyMetadataFromDirectoryAsync(PackageFolder);
        metadata.Apply(ResultFolder);

        // Assert
        Assert.True(metadata.Verify(out _, out _, ResultFolder));
    }

    [Fact]
    public async Task DeltaPackage_CanApplyToFolder()
    {
        // Act
        await Package<Empty>.CreateDeltaAsync(Assets.ManyFileFolderOriginal, Assets.ManyFileFolderTarget, PackageFolder, "1.0", "1.0.1");
        var metadata = await Package<Empty>.ReadMetadataFromDirectoryAsync(PackageFolder);
        metadata.Apply(ResultFolder, null, Assets.ManyFileFolderOriginal);

        // Assert
        Assert.True(metadata.Verify(out _, out _, ResultFolder));
    }

    [Fact]
    public async Task CopyPackage_CanAddNewFiles()
    {
        // Act
        await Package<Empty>.CreateAsync(Assets.AddMissingFileFolderTarget, PackageFolder, "1.0.1");
        var metadata = await Package<Empty>.ReadMetadataFromDirectoryAsync(PackageFolder);
        metadata.Apply(ResultFolder);

        // Assert
        var valid = metadata.Verify(out var missingFiles, out var mismatchFiles, ResultFolder);
        Assert.True(valid);
    }

    [Fact]
    public async Task DeltaPackage_CanAddNewFiles()
    {
        // Act
        await Package<Empty>.CreateDeltaAsync(Assets.AddMissingFileFolderOriginal, Assets.AddMissingFileFolderTarget, PackageFolder, "1.0", "1.0.1");
        var metadata = await Package<Empty>.ReadMetadataFromDirectoryAsync(PackageFolder);
        metadata.Apply(ResultFolder, null, Assets.AddMissingFileFolderOriginal);

        // Assert
        var valid = metadata.Verify(out var missingFiles, out var mismatchFiles, ResultFolder);
        Assert.True(valid);
    }
}