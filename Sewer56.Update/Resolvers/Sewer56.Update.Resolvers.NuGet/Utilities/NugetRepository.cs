using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Sewer56.Update.Resolvers.NuGet.Utilities;

/// <summary>
/// A wrapper around an individual NuGet repository.
/// </summary>
public class NugetRepository
{
    private static NullLogger _nullLogger = new NullLogger();
    private static SourceCacheContext _sourceCacheContext = new SourceCacheContext();

    private PackageSource _packageSource = null!;
    private SourceRepository _sourceRepository = null!;

    private AsyncLazy<DownloadResource> _downloadResource = null!;
    private AsyncLazy<PackageMetadataResource> _packageMetadataResource = null!;
    private AsyncLazy<PackageSearchResource> _packageSearchResource = null!;

    /// <summary/>
    /// <param name="nugetSourceUrl">The source URL of a NuGet V3 Feed, e.g. https://api.nuget.org/v3/index.json</param>
    public NugetRepository(string nugetSourceUrl)
    {
        _packageSource = new PackageSource(nugetSourceUrl);
        _sourceRepository = new SourceRepository(_packageSource, Repository.Provider.GetCoreV3());

        _downloadResource = new AsyncLazy<DownloadResource>(async () => await _sourceRepository.GetResourceAsync<DownloadResource>());
        _packageMetadataResource = new AsyncLazy<PackageMetadataResource>(async () => await _sourceRepository.GetResourceAsync<PackageMetadataResource>());
        _packageSearchResource = new AsyncLazy<PackageSearchResource>(async () => await _sourceRepository.GetResourceAsync<PackageSearchResource>());
    }

    /// <summary>
    /// Downloads a specified NuGet package.
    /// </summary>
    /// <param name="packageIdentity">Information about the package to download.</param>
    /// <param name="token">A cancellation token to allow cancellation of the task.</param>
    public async Task<DownloadResourceResult> DownloadPackageAsync(PackageIdentity packageIdentity, CancellationToken token = default)
    {
        var downloadContext = new PackageDownloadContext(new SourceCacheContext(), Path.GetTempPath(), false);

        try
        {
            var downloadResource = await _downloadResource;
            return await downloadResource.GetDownloadResourceResultAsync(packageIdentity, downloadContext, Path.GetTempPath(), _nullLogger, token);
        }
        catch (Exception)
        {
            return new DownloadResourceResult(DownloadResourceResultStatus.NotFound);
        }
    }

    /// <summary>
    /// Retrieves the details of an individual package.
    /// </summary>
    /// <param name="includeUnlisted">Include unlisted packages.</param>
    /// <param name="packageId">The unique ID of the package.</param>
    /// <param name="includePrerelease">Include pre-release packages.</param>
    /// <param name="token">A cancellation token to allow cancellation of the task.</param>
    /// <returns>Return contains an array of versions for this package.</returns>
    public async Task<IEnumerable<IPackageSearchMetadata>> GetPackageDetailsAsync(string packageId, bool includePrerelease, bool includeUnlisted, CancellationToken token = default)
    {
        try
        {
            var metadataResource = await _packageMetadataResource;
            return await metadataResource.GetMetadataAsync(packageId, includePrerelease, includeUnlisted, _sourceCacheContext, _nullLogger, token);
        }
        catch (Exception) { return new IPackageSearchMetadata[0]; }
    }
}