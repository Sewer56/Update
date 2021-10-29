using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.Update.Extensions;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Interfaces;

/// <summary>
/// Provider for resolving packages.
/// </summary>
public interface IPackageResolver
{
    /// <summary>
    /// Called only once.
    /// 
    /// Use this for performing any asynchronous initialization (that you cannot do in the constructor) such as
    /// - Reading from cache.
    /// - Fetching release metadata.
    /// - Fetching resources from a remote server.
    /// Use this for any asynchronous operations that would be required in your constructor.
    /// </summary>
    Task InitializeAsync() { return Task.CompletedTask; }

    /// <summary>
    /// Returns all available package versions.
    /// </summary>
    /// <remarks>
    ///     If you have release metadata, available consider using <see cref="NuGetExtensions.GetNuGetVersionsFromReleaseMetadata(ReleaseMetadata)"/>.
    ///     If the source provides its own release system with versions (e.g. GitHub API), use the versions returned from the API call.
    /// </remarks>
    Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads given package version.
    /// </summary>
    /// <remarks>
    ///     If source uses its own release system e.g. GitHub, download the release metadata here first.
    ///     Pass <see cref="ReleaseMetadataVerificationInfo"/> to <see cref="ReleaseMetadata.GetRelease"/>.
    ///     Get file name from returned item, and download using that file name.
    /// </remarks>
    /// <param name="progress">Reports progress about the package download.</param>
    /// <param name="cancellationToken">Allows for cancellation of the operation.</param>
    /// <param name="version">The version to be downloaded.</param>
    /// <param name="destFilePath">File path where the item should be downloaded to.</param>
    /// <param name="verificationInfo">
    ///     Pass this to <see cref="ReleaseMetadata.GetRelease"/>.
    ///     This is the information required to verify whether some package types, e.g. Delta Packages can be applied.
    /// </param>
    Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
