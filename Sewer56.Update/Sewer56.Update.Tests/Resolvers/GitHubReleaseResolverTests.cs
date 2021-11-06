using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Packaging.Structures.ReleaseBuilder;
using Sewer56.Update.Resolvers;
using Sewer56.Update.Resolvers.GitHub;
using Sewer56.Update.Structures;
using Xunit;

namespace Sewer56.Update.Tests.Resolvers;

public class GitHubReleaseResolverTests
{
    public string PackageFolder = Path.Combine(Assets.TempFolder, "Package");
    public string OutputFolder = Path.Combine(Assets.TempFolder, "Output");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public GitHubResolverConfiguration ResolverConfiguration = new GitHubResolverConfiguration()
    {
        UserName = "Sewer56",
        RepositoryName = "Update.Test.Repo"
    };

    public GitHubReleaseResolverTests()
    {
        IOEx.TryDeleteDirectory(Assets.TempFolder);
        Directory.CreateDirectory(PackageFolder);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_CanFindMultipleItems()
    {
        for (int x = 0; x < 2; x++)
        {
            // Act
            ResolverConfiguration.AllowPrereleases = false;
            var resolver = new GitHubReleaseResolver(ResolverConfiguration);
            var versions = await resolver.GetPackageVersionsAsync();

            // Assert
            Assert.Equal(new NuGetVersion("3.0"), versions[2]);
            Assert.Equal(new NuGetVersion("2.0"), versions[1]);
            Assert.Equal(new NuGetVersion("1.0"), versions[0]);

            // Act for Prerelease
            ResolverConfiguration.AllowPrereleases = true;
            var versionsWithPrereleases = await resolver.GetPackageVersionsAsync();
            Assert.Equal(5, versionsWithPrereleases.Count);
        }
    }

    [Fact]
    public async Task GetPackageVersionsAsync_CanDownloadItem()
    {
        var packageFilePath = Path.Combine(PackageFolder, "Package.pkg");

        // Act
        var resolver = new GitHubReleaseResolver(ResolverConfiguration);
        var versions = await resolver.GetPackageVersionsAsync();
        
        await resolver.DownloadPackageAsync(versions[0], packageFilePath, new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });

        // Assert
        Assert.True(File.Exists(packageFilePath));
    }

    [Fact]
    public async Task GetPackageVersionsAsync_CanDownloadItem_WithCustomMetadataFile()
    {
        var packageFilePath = Path.Combine(PackageFolder, "Package.pkg");

        // Act
        ResolverConfiguration.AllowPrereleases = true;
        var resolver = new GitHubReleaseResolver(ResolverConfiguration, new CommonPackageResolverSettings()
        {
            MetadataFileName = "Package.json"
        });

        var versions = await resolver.GetPackageVersionsAsync();
        var version = versions.Find(x => x.Equals(new NuGetVersion("3.0-meta")));
        await resolver.DownloadPackageAsync(version, packageFilePath, new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });

        // Assert
        Assert.True(File.Exists(packageFilePath));
    }
}