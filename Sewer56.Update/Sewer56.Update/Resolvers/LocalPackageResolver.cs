using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.Update.Extensions;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Interfaces.Extensions;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Structures;

namespace Sewer56.Update.Resolvers;

/// <summary>
/// Resolves packages from a local folder.
/// Local folder must contain manifest for each package.
/// </summary>
public class LocalPackageResolver : IPackageResolver, IPackageResolverDownloadSize
{
    /// <summary>
    /// The local filesystem folder associated with this repository.
    /// </summary>
    public string RepositoryFolder { get; }

    private ReleaseMetadata? _releases;
    private CommonPackageResolverSettings _commonResolverSettings;
    private bool _hasInitialised;

    /// <summary/>
    /// <param name="repositoryFolder">Folder containing packages and package manifest.</param>
    /// <param name="resolverSettings">Settings that override how most package resolvers work.</param>
    public LocalPackageResolver(string repositoryFolder, CommonPackageResolverSettings? resolverSettings = null)
    {
        RepositoryFolder = repositoryFolder;
        _commonResolverSettings = resolverSettings ?? new CommonPackageResolverSettings();
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_hasInitialised)
            return;

        _hasInitialised = true;
        _releases = await Singleton<ReleaseMetadata>.Instance.ReadFromDirectoryOrDefaultAsync(RepositoryFolder, _commonResolverSettings.MetadataFileName);
    }

    /// <inheritdoc />
    public Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(_releases!.GetNuGetVersionsFromReleaseMetadata(_commonResolverSettings.AllowPrereleases));

    /// <inheritdoc />
    public async Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        await using var sourceFile = File.Open(GetReleaseFilePath(version, verificationInfo), FileMode.Open);
        await using var targetFile = File.Open(destFilePath, FileMode.Create);
        await sourceFile.CopyToAsyncEx(targetFile, 262144, progress, cancellationToken);
    }

    /// <inheritdoc />
    public Task<long> GetDownloadFileSizeAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default)
    {
        return Task.FromResult(new FileInfo(GetReleaseFilePath(version, verificationInfo)).Length);
    }

    private string GetReleaseFilePath(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo)
    {
        var releaseItem = _releases!.GetRelease(version.ToString(), verificationInfo);
        if (releaseItem == null)
            throw new ArgumentException($"Unable to find Release for the specified NuGet Version `{nameof(version)}` ({version})");

        return Path.Combine(RepositoryFolder, releaseItem.FileName);
    }
}