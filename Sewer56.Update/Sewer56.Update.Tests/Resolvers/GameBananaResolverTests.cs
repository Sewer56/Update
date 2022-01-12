using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Resolvers.GameBanana;
using Sewer56.Update.Resolvers.GitHub;
using Sewer56.Update.Structures;
using Xunit;

namespace Sewer56.Update.Tests.Resolvers;

public class GameBananaResolverTests
{
    public string PackageFolder = Path.Combine(Assets.TempFolder, "Package");
    public string OutputFolder = Path.Combine(Assets.TempFolder, "Output");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public GameBananaResolverConfiguration ResolverConfiguration = new GameBananaResolverConfiguration()
    {
        ItemId = 333681
    };

    public GameBananaResolverTests()
    {
        IOEx.TryDeleteDirectory(Assets.TempFolder);
        Directory.CreateDirectory(PackageFolder);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_CanFindMultipleItems()
    {
        var commonResolverSettings = new CommonPackageResolverSettings()
        {
            AllowPrereleases = true
        };

        // Act
        var resolver = new GameBananaUpdateResolver(ResolverConfiguration, commonResolverSettings);
        await resolver.InitializeAsync();
        var versions = await resolver.GetPackageVersionsAsync();

        // Assert
        Assert.Equal(new NuGetVersion("3.0"), versions[2]);
        Assert.Equal(new NuGetVersion("2.0"), versions[1]);
        Assert.Equal(new NuGetVersion("1.0"), versions[0]);
        Assert.Equal(3, versions.Count);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_CanDownloadItem()
    {
        var packageFilePath = Path.Combine(PackageFolder, "Package.pkg");

        // Act
        var resolver = new GameBananaUpdateResolver(ResolverConfiguration);
        await resolver.InitializeAsync();
        var versions = await resolver.GetPackageVersionsAsync();

        await resolver.DownloadPackageAsync(versions[0], packageFilePath, new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });

        // Assert
        Assert.True(File.Exists(packageFilePath));
    }

    [Fact]
    public async Task GetPackageVersionsAsync_CanGetFileSize()
    {
        var packageFilePath = Path.Combine(PackageFolder, "Package.pkg");

        // Act
        var resolver = new GameBananaUpdateResolver(ResolverConfiguration);
        await resolver.InitializeAsync();
        var versions = await resolver.GetPackageVersionsAsync();

        var fileSize = await resolver.GetDownloadFileSizeAsync(versions[0], new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });

        // Assert
        Assert.True(fileSize > 0);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_SupportsBrotli()
    {
        var commonResolverSettings = new CommonPackageResolverSettings()
        {
            AllowPrereleases = true,
            MetadataFileName = "Sewer56.Update.BrotliMetadata.json"
        };

        // Act
        var resolver = new GameBananaUpdateResolver(ResolverConfiguration, commonResolverSettings);
        await resolver.InitializeAsync();
        var versions = await resolver.GetPackageVersionsAsync();

        // Assert
        Assert.Equal(new NuGetVersion("3.0"), versions[2]);
        Assert.Equal(new NuGetVersion("2.0"), versions[1]);
        Assert.Equal(new NuGetVersion("1.0"), versions[0]);
        Assert.Equal(3, versions.Count);
    }
}