using System.IO;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Resolvers.NuGet;
using Sewer56.Update.Resolvers.NuGet.Utilities;
using Sewer56.Update.Structures;
using Xunit;

namespace Sewer56.Update.Tests.Resolvers;

public class NuGetResolverTests
{
    public string PackageFolder = Path.Combine(Assets.TempFolder, "Package");
    public string OutputFolder = Path.Combine(Assets.TempFolder, "Output");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public NuGetUpdateResolverSettings ResolverConfiguration = new NuGetUpdateResolverSettings()
    {
        NugetRepository = new NugetRepository("https://api.nuget.org/v3/index.json"),
        PackageId = "Newtonsoft.Json"
    };

    public NuGetResolverTests()
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
        var resolver = new NuGetUpdateResolver(ResolverConfiguration, commonResolverSettings);
        var versions = await resolver.GetPackageVersionsAsync();

        // Assert
        Assert.Contains(new NuGetVersion("3.5.8"), versions);
        Assert.Contains(new NuGetVersion("4.0.1"), versions);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_CanDownloadItem()
    {
        var packageFilePath = Path.Combine(PackageFolder, "Package.pkg");

        var commonResolverSettings = new CommonPackageResolverSettings()
        {
            AllowPrereleases = true
        };

        // Act
        var resolver = new NuGetUpdateResolver(ResolverConfiguration, commonResolverSettings);
        var versions = await resolver.GetPackageVersionsAsync();

        await resolver.DownloadPackageAsync(versions[0], packageFilePath, new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });

        // Assert
        Assert.True(File.Exists(packageFilePath));
    }


    [Fact]
    public async Task GetPackageVersionsAsync_CanGetFileSize()
    {
        var commonResolverSettings = new CommonPackageResolverSettings()
        {
            AllowPrereleases = true
        };

        // Act
        var resolver = new NuGetUpdateResolver(ResolverConfiguration, commonResolverSettings);
        var versions = await resolver.GetPackageVersionsAsync();

        var downloadSize = await resolver.GetDownloadFileSizeAsync(versions[0], new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });

        // Assert
        Assert.True(downloadSize > 0);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_CanGetDownloadUrl()
    {
        var commonResolverSettings = new CommonPackageResolverSettings()
        {
            AllowPrereleases = true
        };

        // Act
        var resolver = new NuGetUpdateResolver(ResolverConfiguration, commonResolverSettings);
        var versions = await resolver.GetPackageVersionsAsync();

        var downloadUrl = await resolver.GetDownloadUrlAsync(versions[0], new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });

        // Assert
        Assert.True(!string.IsNullOrEmpty(downloadUrl));
    }

}