using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    /// <summary>
    /// URL with which this repository was created with.
    /// </summary>
    public string SourceUrl { get; }

    private static NullLogger _nullLogger = new NullLogger();
    private static SourceCacheContext _sourceCacheContext = new SourceCacheContext();

    private PackageSource _packageSource = null!;
    private SourceRepository _sourceRepository = null!;

    private AsyncLazy<DownloadResource> _downloadResource = null!;
    private AsyncLazy<PackageMetadataResource> _packageMetadataResource = null!;

    /// <summary/>
    /// <param name="nugetSourceUrl">The source URL of a NuGet V3 Feed, e.g. https://api.nuget.org/v3/index.json</param>
    public NugetRepository(string nugetSourceUrl)
    {
        SourceUrl = nugetSourceUrl;
        _packageSource = new PackageSource(nugetSourceUrl);
        _sourceRepository = new SourceRepository(_packageSource, Repository.Provider.GetCoreV3());

        _downloadResource = new AsyncLazy<DownloadResource>(async () => await _sourceRepository.GetResourceAsync<DownloadResource>());
        _packageMetadataResource = new AsyncLazy<PackageMetadataResource>(async () => await _sourceRepository.GetResourceAsync<PackageMetadataResource>());
        new AsyncLazy<PackageSearchResource>(async () => await _sourceRepository.GetResourceAsync<PackageSearchResource>());
    }

    /// <summary>
    /// Downloads a specified NuGet package.
    /// </summary>
    /// <param name="packageIdentity">Information about the package to download.</param>
    /// <param name="token">A cancellation token to allow cancellation of the task.</param>
    public async Task<DownloadResourceResult> DownloadPackageAsync(PackageIdentity packageIdentity, CancellationToken token = default)
    {
        var downloadContext = new PackageDownloadContext(new SourceCacheContext(), Path.GetTempPath(), true);

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

    /// <summary>
    /// Retrieves the details of an individual package.
    /// </summary>
    /// <param name="identity">Uniquely identifies the package.</param>
    /// <param name="token">A cancellation token to allow cancellation of the task.</param>
    /// <returns>Return contains an array of versions for this package.</returns>
    public async Task<IPackageSearchMetadata?> GetPackageDetailsAsync(PackageIdentity identity, CancellationToken token = default)
    {
        try
        {
            var metadataResource = await _packageMetadataResource;
            return await metadataResource.GetMetadataAsync(identity, _sourceCacheContext, _nullLogger, token).ConfigureAwait(false);
        }
        catch (Exception) { return null; }
    }

    /// <summary>
    /// [WARNING: REFLECTION]
    /// Uses reflection to get a URL to the package download.
    /// </summary>
    /// <param name="packageIdentity">Information about the package to download.</param>
    /// <param name="token">A cancellation token to allow cancellation of the task.</param>
    public async Task<string?> GetDownloadUrlUnsafeAsync(PackageIdentity packageIdentity, CancellationToken token = default)
    {
        try
        {
            var downloadResource = await _downloadResource;

            // No public API for this, ah shit, here we go again.
            // Original definition:

            /*
                /// <summary>
                /// Get the download url of the package.
                /// 1. If the identity is a SourcePackageDependencyInfo the SourcePackageDependencyInfo.DownloadUri is used.
                /// 2. A url will be constructed for the flat container location if the source has that resource.
                /// 3. The download url will be found in the registration blob as a fallback.
                /// </summary>
                private async Task<Uri> GetDownloadUrl(PackageIdentity identity, ILogger log, CancellationToken token)
            */

            var dynMethod = downloadResource.GetType().GetMethod("GetDownloadUrl", BindingFlags.NonPublic | BindingFlags.Instance);
            var result = await (Task<Uri>) dynMethod!.Invoke(downloadResource, new object[] { packageIdentity, null!, token })!;
            return result.AbsoluteUri;
        }
        catch (Exception)
        {
            return null;
        }
    }
}