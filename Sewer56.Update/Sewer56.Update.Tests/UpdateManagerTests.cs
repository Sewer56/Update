using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Structures;
using Sewer56.Update.Tests.Dummy;
using Sewer56.Update.Tests.Internal;
using Sewer56.Update.Tests.Mocks;
using Xunit;

namespace Sewer56.Update.Tests;

public class UpdateManagerTests : IDisposable
{
    private string TempDirPath { get; } = Utilities.MakeUniqueFolder(Directory.GetCurrentDirectory());

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

#if !DEBUG
    [Theory(Timeout = 10000)]
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
}