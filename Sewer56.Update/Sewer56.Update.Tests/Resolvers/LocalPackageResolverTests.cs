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
using Xunit;

namespace Sewer56.Update.Tests.Resolvers;

public class LocalPackageResolverTests
{
    public string PackageFolder  = Path.Combine(Assets.TempFolder, "Package");
    public string OutputFolder   = Path.Combine(Assets.TempFolder, "Output");
    public string MetadataFolder = Path.Combine(Assets.TempFolder, "Metadata");

    public LocalPackageResolverTests()
    {
        IOEx.TryDeleteDirectory(Assets.TempFolder);
        Directory.CreateDirectory(PackageFolder);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_CanFindMultipleItems()
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
            Version = "2.0"
        });

        builder.AddCopyPackage(new CopyBuilderItem<Empty>()
        {
            FolderPath = Assets.ManyFileFolderTarget,
            Version = "3.0"
        });

        var metadata = await builder.BuildAsync(new BuildArgs()
        {
            FileName = "Package",
            OutputFolder = this.OutputFolder
        });

        // Act
        var resolver = new LocalPackageResolver(OutputFolder);
        await resolver.InitializeAsync();
        var versions = await resolver.GetPackageVersionsAsync();

        // Assert
        Assert.Equal(new NuGetVersion("3.0"), versions[2]);
        Assert.Equal(new NuGetVersion("2.0"), versions[1]);
        Assert.Equal(new NuGetVersion("1.0"), versions[0]);
    }

    [Fact]
    public async Task GetPackageVersionsAsync_CanDownloadItem()
    {
        var packageFilePath = Path.Combine(PackageFolder, "Package.pkg");

        // Arrange
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
        var resolver = new LocalPackageResolver(OutputFolder);
        await resolver.InitializeAsync();

        var versions = await resolver.GetPackageVersionsAsync();
        await resolver.DownloadPackageAsync(versions[0], packageFilePath, new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });

        // Assert
        Assert.True(File.Exists(packageFilePath));
    }
}