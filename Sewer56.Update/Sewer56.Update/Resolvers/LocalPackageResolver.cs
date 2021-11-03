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
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Resolvers;

/// <summary>
/// Resolves packages from a local folder.
/// Local folder must contain manifest for each package.
/// </summary>
public class LocalPackageResolver : IPackageResolver
{
    private ReleaseMetadata? _releases;
    private string _repositoryFolder;

    /// <summary/>
    /// <param name="repositoryFolder">Folder containing packages and package manifest.</param>
    public LocalPackageResolver(string repositoryFolder)
    {
        _repositoryFolder = repositoryFolder;
    }

    /// <inheritdoc />
    public async Task InitializeAsync() => _releases = await Singleton<ReleaseMetadata>.Instance.ReadFromDirectoryAsync(_repositoryFolder);

    /// <inheritdoc />
    public Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(_releases!.GetNuGetVersionsFromReleaseMetadata());

    /// <inheritdoc />
    public async Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var releaseItem = _releases!.GetRelease(version.ToString(), verificationInfo);
        await using var sourceFile = File.Open(Path.Combine(_repositoryFolder, releaseItem!.FileName), FileMode.Open);
        await using var targetFile = File.Open(destFilePath, FileMode.Create);
        await sourceFile.CopyToAsyncEx(targetFile, 262144, progress, cancellationToken);
    }
}