using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Interfaces.Extensions;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Structures;

namespace Sewer56.Update.Resolvers.NuGet;

/// <summary>
/// Allows for retrieval of updates from a NuGet V3 Source.
/// </summary>
public class NuGetUpdateResolver : IPackageResolver, IPackageResolverDownloadSize, IPackageResolverDownloadUrl
{
    private NuGetUpdateResolverSettings _resolverSettings;
    private CommonPackageResolverSettings _commonPackageResolverSettings;

    /// <summary>
    /// Retrieves updates from a NuGet source.
    /// </summary>
    /// <param name="resolverSettings">Encapsulates the repository on which operations will be made.</param>
    /// <param name="commonResolverSettings">Common package resolver settings.</param>
    public NuGetUpdateResolver(NuGetUpdateResolverSettings resolverSettings, CommonPackageResolverSettings commonResolverSettings)
    {
        _resolverSettings = resolverSettings;
        _commonPackageResolverSettings = commonResolverSettings;
    }

    /// <inheritdoc />
    public async Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default)
    {
        var packageDetails = await _resolverSettings.NugetRepository!.GetPackageDetailsAsync(_resolverSettings.PackageId!, _commonPackageResolverSettings.AllowPrereleases, _resolverSettings.AllowUnlisted, cancellationToken);
        var versions = packageDetails.Select(x => x.Identity.Version).ToList();
        versions.Sort((x, y) => x.CompareTo(y));
        return versions;
    }

    /// <inheritdoc />
    public async Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var identity = new PackageIdentity(_resolverSettings.PackageId, version);
        using var result = await _resolverSettings.NugetRepository!.DownloadPackageAsync(identity, cancellationToken);
        await using var fileStream = File.Open(destFilePath, FileMode.Create);
        await result.PackageStream.CopyToAsyncEx(fileStream, 131072, progress, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> GetDownloadFileSizeAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default)
    {
        var downloadUrl = await GetDownloadUrlAsync(version, verificationInfo, token);
        var fileReq = WebRequest.CreateHttp(downloadUrl);
        return (await fileReq.GetResponseAsync()).ContentLength;
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetDownloadUrlAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default)
    {
        var identity = new PackageIdentity(_resolverSettings.PackageId, version);
        return (await _resolverSettings.NugetRepository!.GetDownloadUrlUnsafeAsync(identity, token))!;
    }

    /// <summary>
    /// Gets the full package details for a given version of the package.
    /// </summary>
    /// <param name="version">Version to get the details for.</param>
    /// <param name="token">Token to cancel the fetch operation.</param>
    public async Task<IPackageSearchMetadata?> GetPackageDetailsAsync(NuGetVersion version, CancellationToken token = default)
    {
        return await _resolverSettings.NugetRepository!.GetPackageDetailsAsync(new PackageIdentity(_resolverSettings.PackageId, version), token);
    }
}