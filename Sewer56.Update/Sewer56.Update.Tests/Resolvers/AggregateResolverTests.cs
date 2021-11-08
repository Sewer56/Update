using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Resolvers;
using Sewer56.Update.Resolvers.GameBanana;
using Sewer56.Update.Resolvers.GitHub;
using Sewer56.Update.Structures;
using Xunit;

namespace Sewer56.Update.Tests.Resolvers;

public class AggregateResolverTests
{
    public string PackageFolder = Path.Combine(Assets.TempFolder, "Package");
    public string OutputFolder = Path.Combine(Assets.TempFolder, "Output");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public GameBananaResolverConfiguration GameBananaConfig = new GameBananaResolverConfiguration()
    {
        ItemId = 333681
    };

    public GitHubResolverConfiguration GitHubConfig = new GitHubResolverConfiguration()
    {
        UserName = "Sewer56",
        RepositoryName = "Update.Test.Repo"
    };

    public AggregateResolverTests()
    {
        IOEx.TryDeleteDirectory(Assets.TempFolder);
        Directory.CreateDirectory(PackageFolder);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_FindsVersionsOnlyInOneSource()
    {
        var commonResolverSettings = new CommonPackageResolverSettings() { AllowPrereleases = true };

        // Act
        var resolver = new AggregatePackageResolver(new List<IPackageResolver>()
        {
            new GameBananaUpdateResolver(GameBananaConfig, commonResolverSettings),
            new GitHubReleaseResolver(GitHubConfig, commonResolverSettings),
        });

        await resolver.InitializeAsync();
        var versions = await resolver.GetPackageVersionsAsync();

        // Assert (Shared)
        Assert.Contains(new NuGetVersion("3.0"), versions); 
        Assert.Contains(new NuGetVersion("2.0"), versions);
        Assert.Contains(new NuGetVersion("1.0"), versions);

        // Assert (GitHub Only)
        Assert.Contains(new NuGetVersion("3.0-pre"), versions);
    }

    [Fact]
    public async Task DownloadPackageAsync_CanDownloadItemFromNonDefaultSource()
    {
        var packageFilePath = Path.Combine(PackageFolder, "Package.pkg");
        var commonResolverSettings = new CommonPackageResolverSettings() { AllowPrereleases = true };

        // Act
        var resolver = new AggregatePackageResolver(new List<IPackageResolver>()
        {
            new GameBananaUpdateResolver(GameBananaConfig, commonResolverSettings),
            new GitHubReleaseResolver(GitHubConfig, commonResolverSettings),
        });

        await resolver.InitializeAsync();

        // GitHub Only Package
        await resolver.DownloadPackageAsync(new NuGetVersion("3.0-pre"), packageFilePath, new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });

        // Assert
        Assert.True(File.Exists(packageFilePath));
    }

}