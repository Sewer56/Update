using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Interfaces.Extensions;

/// <summary>
/// Allows you to obtain the download size if supported by the package resolver.
/// </summary>
public interface IPackageResolverDownloadSize
{
    /// <summary>
    /// Obtains the download file size for a package to be downloaded.
    /// Ideally this should be called before <see cref="IPackageResolver.DownloadPackageAsync"/>.
    /// </summary>
    /// <param name="version">Version of the package to download.</param>
    /// <param name="verificationInfo">
    ///     Pass this to <see cref="ReleaseMetadata.GetRelease"/>.
    ///     This is the information required to verify whether some package types, e.g. Delta Packages can be applied.
    /// </param>
    /// <param name="token">Can be used to cancel the operation.</param>
    /// <returns>The download file size in bytes.</returns>
    public Task<long> GetDownloadFileSizeAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default);
}