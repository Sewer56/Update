using NuGet.Versioning;
using System.Threading.Tasks;
using System.Threading;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Interfaces.Extensions;

/// <summary>
/// Allows you to obtain the direct download URL if supported by the package resolver.
/// </summary>
public interface IPackageResolverDownloadUrl
{
    /// <summary>
    /// Obtains the direct URL to download package.
    /// </summary>
    /// <param name="version">Version of the package to download.</param>
    /// <param name="verificationInfo">
    ///     Pass this to <see cref="ReleaseMetadata.GetRelease"/>.
    ///     This is the information required to verify whether some package types, e.g. Delta Packages can be applied.
    /// </param>
    /// <param name="token">Can be used to cancel the operation.</param>
    /// <returns>The direct download URL.</returns>
    public ValueTask<string?> GetDownloadUrlAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default);
}