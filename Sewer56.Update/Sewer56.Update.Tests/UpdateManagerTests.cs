using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Extensions;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Extractors;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Packaging.Structures.ReleaseBuilder;
using Sewer56.Update.Resolvers;
using Sewer56.Update.Structures;
using Sewer56.Update.Tests.Dummy;
using Sewer56.Update.Tests.Internal;
using Sewer56.Update.Tests.Mocks;
using Xunit;

namespace Sewer56.Update.Tests;

public class UpdateManagerTests : IDisposable
{
    private static string TempDirPath  = Utilities.MakeUniqueFolder(Directory.GetCurrentDirectory());
    private string OutputFolder = Path.Combine(TempDirPath, "Output");

    public UpdateManagerTests() => IOEx.TryEmptyDirectory(TempDirPath);

    public void Dispose() => IOEx.TryDeleteDirectory(TempDirPath);

    [Fact]
    public async Task CheckForUpdates_ReturnsHigherVersionIfAvailable()
    {
        // Arrange
        var updatee = new ItemMetadata(NuGetVersion.Parse("1.0"), Directory.GetCurrentDirectory());
        var availableVersions = new List<NuGetVersion>
        {
            NuGetVersion.Parse("1.0"),
            NuGetVersion.Parse("2.0"),
            NuGetVersion.Parse("3.0")
        };

        using var updateManager = await UpdateManager<Empty>.CreateAsync(updatee, new FakePackageResolver(availableVersions), new FakePackageExtractor());

        // Act
        var result = await updateManager.CheckForUpdatesAsync();

        // Assert
        result.CanUpdate.Should().BeTrue();
        result.Versions.Should().BeEquivalentTo(availableVersions);
        result.LastVersion.Should().Be(NuGetVersion.Parse("3.0"));
    }

    [Fact]
    public async Task CheckForUpdates_ReturnsNothingIfHigherVersionUnavailable()
    {
        // Arrange
        var updatee = new ItemMetadata(NuGetVersion.Parse("3.0"), Directory.GetCurrentDirectory());
        var availableVersions = new List<NuGetVersion>
        {
            NuGetVersion.Parse("1.0"),
            NuGetVersion.Parse("2.0"),
            NuGetVersion.Parse("3.0")
        };

        using var updateManager = await UpdateManager<Empty>.CreateAsync(updatee, new FakePackageResolver(availableVersions), new FakePackageExtractor());

        // Act
        var result = await updateManager.CheckForUpdatesAsync();

        // Assert
        result.CanUpdate.Should().BeFalse();
        result.Versions.Should().BeEquivalentTo(availableVersions);
        result.LastVersion.Should().Be(updatee.Version);
    }

    [Fact]
    public async Task CheckForUpdates_ReturnsNothingIfPackageSourceHasNoPackages()
    {
        // Arrange
        var updatee = new ItemMetadata(NuGetVersion.Parse("3.0"), Directory.GetCurrentDirectory());
        var availableVersions = new List<NuGetVersion>();

        using var updateManager = await UpdateManager<Empty>.CreateAsync(updatee, new FakePackageResolver(availableVersions), new FakePackageExtractor());

        // Act
        var result = await updateManager.CheckForUpdatesAsync();

        // Assert
        result.CanUpdate.Should().BeFalse();
        result.Versions.Should().BeEmpty();
        result.LastVersion.Should().BeNull();
    }

    [Fact]
    public async Task PrepareUpdate_PerformsSuccessfully()
    {
        // Arrange
        var updatee = new ItemMetadata(NuGetVersion.Parse("1.0"), Directory.GetCurrentDirectory());
        var availableVersions = new List<NuGetVersion>()
        {
            NuGetVersion.Parse("1.0"),
            NuGetVersion.Parse("2.0"),
            NuGetVersion.Parse("3.0")
        };

        using var updateManager = await UpdateManager<Empty>.CreateAsync(updatee, new FakePackageResolver(availableVersions), new FakePackageExtractor());

        var expectedPreparedUpdateVersions = new[]
        {
            NuGetVersion.Parse("1.0"),
            NuGetVersion.Parse("3.0")
        };

        foreach (var version in expectedPreparedUpdateVersions)
            await updateManager.PrepareUpdateAsync(version);

        // Act
        var preparedUpdateVersions = updateManager.GetPreparedUpdates();

        // Assert
        preparedUpdateVersions.Should().BeEquivalentTo(expectedPreparedUpdateVersions);
    }

    [Fact]
    public async Task PrepareUpdate_AllowsToReturnUpdateFolder()
    {
        using var dummyUpdatee = new TemporaryFolderAllocation();
        IOEx.CopyDirectory(Assets.AddMissingFileFolderOriginal, dummyUpdatee.FolderPath);

        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.AddMissingFileFolderOriginal,
            Version = "1.0"
        });

        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.AddMissingFileFolderTarget,
            Version = "2.0"
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });
        
        // Act
        var updateeMetadata = new ItemMetadata(NuGetVersion.Parse("1.0"), dummyUpdatee.FolderPath);
        using var updateManager = await UpdateManager<Empty>.CreateAsync(updateeMetadata, new LocalPackageResolver(this.OutputFolder), new ZipPackageExtractor());
        
        // Assert
        Assert.False(updateManager.TryGetPackageContentDirPath(NuGetVersion.Parse("2.0"), out _));
        await updateManager.PrepareUpdateAsync(NuGetVersion.Parse("2.0"));
        Assert.True(updateManager.TryGetPackageContentDirPath(NuGetVersion.Parse("2.0"), out var versionPath));
        Assert.True(Directory.Exists(versionPath));
    }

    [Fact]
    public async Task TryGetPackageMetadata_ReturnsMetadataIfAvailable()
    {
        const string version = "1.0";

        // Arrange
        await PrepareDummyPackage();
        var dummyUpdatee = new ItemMetadata(NuGetVersion.Parse("0.0"), Directory.GetCurrentDirectory());
        using var updateManager = await UpdateManager<Empty>.CreateAsync(dummyUpdatee, new LocalPackageResolver(this.OutputFolder), new ZipPackageExtractor());
        await updateManager.PrepareUpdateAsync(NuGetVersion.Parse(version));

        // Act
        var metadata = await updateManager.TryGetPackageMetadataAsync(NuGetVersion.Parse(version));

        // Arrange
        metadata.Should().NotBeNull();
        version.Should().BeEquivalentTo(metadata.Version);
    }

    [Fact]
    public async Task TryGetPackageMetadata_DoesNotReturnIfNotPrepared()
    {
        const string version = "1.0";

        // Arrange
        await PrepareDummyPackage();
        var dummyUpdatee = new ItemMetadata(NuGetVersion.Parse("0.0"), Directory.GetCurrentDirectory());
        using var updateManager = await UpdateManager<Empty>.CreateAsync(dummyUpdatee, new LocalPackageResolver(this.OutputFolder), new ZipPackageExtractor());

        // Act
        var metadata = await updateManager.TryGetPackageMetadataAsync(NuGetVersion.Parse(version));

        // Arrange
        metadata.Should().BeNull();
    }

    [Fact]
    public async Task StartUpdate_ForCopyPackage_ForExternalComponent_WhenAddingFiles()
    {
        using var dummyUpdatee = new TemporaryFolderAllocation();
        IOEx.CopyDirectory(Assets.AddMissingFileFolderOriginal, dummyUpdatee.FolderPath);

        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.AddMissingFileFolderOriginal,
            Version = "1.0"
        });

        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.AddMissingFileFolderTarget,
            Version = "2.0"
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        var expectedHashes = HashSet.Generate(Assets.AddMissingFileFolderTarget);

        // Act
        var updateeMetadata = new ItemMetadata(NuGetVersion.Parse("1.0"), dummyUpdatee.FolderPath);
        using var updateManager = await UpdateManager<Empty>.CreateAsync(updateeMetadata, new LocalPackageResolver(this.OutputFolder), new ZipPackageExtractor());
        await updateManager.CheckPerformUpdateAsync();

        // Assert
        Assert.True(HashSet.Verify(expectedHashes, dummyUpdatee.FolderPath, out _, out _));
    }

    [Fact]
    public async Task StartUpdate_ForDeltaPackage_ForExternalComponent_WhenAddingFiles()
    {
        using var dummyUpdatee = new TemporaryFolderAllocation();
        IOEx.CopyDirectory(Assets.AddMissingFileFolderOriginal, dummyUpdatee.FolderPath);

        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        builder.AddDeltaPackage(new DeltaBuilderItem<Empty>()
        {
            FolderPath = Assets.AddMissingFileFolderTarget,
            Version    = "2.0",
            PreviousVersion = "1.0",
            PreviousVersionFolder = Assets.AddMissingFileFolderOriginal
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        var expectedHashes = HashSet.Generate(Assets.AddMissingFileFolderTarget);

        // Act
        var updateeMetadata = new ItemMetadata(NuGetVersion.Parse("1.0"), dummyUpdatee.FolderPath);
        using var updateManager = await UpdateManager<Empty>.CreateAsync(updateeMetadata, new LocalPackageResolver(this.OutputFolder), new ZipPackageExtractor());
        await updateManager.CheckPerformUpdateAsync();

        // Assert
        Assert.True(HashSet.Verify(expectedHashes, dummyUpdatee.FolderPath, out _, out _));
    }

    [Fact]
    public async Task StartUpdate_ForDeltaPackage_ForExternalComponent_WhenPatchingFiles()
    {
        using var dummyUpdatee = new TemporaryFolderAllocation();
        IOEx.CopyDirectory(Assets.ManyFileFolderOriginal, dummyUpdatee.FolderPath);

        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        builder.AddDeltaPackage(new DeltaBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderTarget,
            Version = "2.0",
            PreviousVersion = "1.0",
            PreviousVersionFolder = Assets.ManyFileFolderOriginal
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        var expectedHashes = HashSet.Generate(Assets.ManyFileFolderTarget);

        // Act
        var updateeMetadata = new ItemMetadata(NuGetVersion.Parse("1.0"), dummyUpdatee.FolderPath);
        using var updateManager = await UpdateManager<Empty>.CreateAsync(updateeMetadata, new LocalPackageResolver(this.OutputFolder), new ZipPackageExtractor());
        await updateManager.CheckPerformUpdateAsync();

        // Assert
        Assert.True(HashSet.Verify(expectedHashes, dummyUpdatee.FolderPath, out _, out _));
    }

#if !DEBUG
    [Theory(Timeout = 30000)]
#else
    [Theory()]
#endif
    [InlineData(PackageType.Copy)]
    [InlineData(PackageType.Delta)]
    public async Task StartUpdate_SuccessfullyUpdates(PackageType type)
    {
        // Arrange
        using var dummy = new DummyEnvironment(Path.Combine(TempDirPath, "Dummy"));

        var baseVersion = Version.Parse("1.0.0.0");
        var availableVersions = new[]
        {
            Version.Parse("1.0.0.0"),
            Version.Parse("2.0.0.0"),
            Version.Parse("3.0.0.0")
        };

        var expectedFinalVersion = Version.Parse("3.0.0.0");
        await dummy.SetupAsync(baseVersion, availableVersions, type);

        // Assert (current version)
        var oldVersion = Version.Parse(await dummy.RunDummyAsync(Program.Command_Version));
        oldVersion.Should().Be(baseVersion);

        // Act
        await dummy.RunDummyAsync(Program.Command_Update);

        // Assert (version after update)
        var newVersion = Version.Parse(await dummy.RunDummyAsync(Program.Command_Version));
        newVersion.Should().Be(expectedFinalVersion);
    }

    private async Task PrepareDummyPackage()
    {
        var builder = new ReleaseBuilder<Empty>();
        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderOriginal,
            Version = "1.0"
        });

        await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });
    }
}