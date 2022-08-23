using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Interfaces.Extensions;
using Sewer56.Update.Packaging.Structures;
#pragma warning disable CS1998

namespace Sewer56.Update.Tests.Mocks;

[ExcludeFromCodeCoverage]
internal class ExceptionPackageResolver : IPackageResolver, IPackageResolverDownloadSize, IPackageResolverDownloadUrl, IPackageResolverGetLatestReleaseMetadata
{
    private readonly bool _throwOnInitialize;
    private readonly bool _throwOnGetPackageVersions;
    private readonly bool _throwOnDownloadPackage;
    private readonly bool _throwOnGetDownloadFileSize;
    private readonly bool _throwOnGetDownloadUrl;
    private readonly bool _throwOnGetLatestReleaseMetadata;
    private readonly List<NuGetVersion> _versions = new List<NuGetVersion>();

    public ExceptionPackageResolver(bool throwOnInitialize, bool throwOnGetPackageVersions, bool throwOnDownloadPackage, bool throwOnGetDownloadFileSize = false, List<NuGetVersion> versions = null, bool throwOnGetDownloadUrl = false, bool throwOnGetLatestReleaseMetadata = false)
    {
        _throwOnInitialize = throwOnInitialize;
        _throwOnGetPackageVersions = throwOnGetPackageVersions;
        _throwOnDownloadPackage = throwOnDownloadPackage;
        _throwOnGetDownloadFileSize = throwOnGetDownloadFileSize;
        _throwOnGetDownloadUrl = throwOnGetDownloadUrl;
        _throwOnGetLatestReleaseMetadata = throwOnGetLatestReleaseMetadata;
        if (versions != null)
            _versions = versions;
    }

    public async Task InitializeAsync()
    {
        if (_throwOnInitialize)
            throw new NotImplementedException();
    }

    public async Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default)
    {
        if (_throwOnGetPackageVersions)
            throw new NotImplementedException();

        return _versions;
    }

    public async Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo,
        IProgress<double> progress = null, CancellationToken cancellationToken = default)
    {
        if (_throwOnDownloadPackage)
            throw new NotImplementedException();
    }

    public async Task<long> GetDownloadFileSizeAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo,
        CancellationToken token = default)
    {
        if (_throwOnGetDownloadFileSize)
            throw new NotImplementedException();

        return -1;
    }

    public async ValueTask<string> GetDownloadUrlAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo,
        CancellationToken token = default)
    {
        if (_throwOnGetDownloadUrl)
            throw new NotImplementedException();

        return "";
    }

    public ValueTask<ReleaseMetadata> GetReleaseMetadataAsync(CancellationToken token)
    {
        if (_throwOnGetLatestReleaseMetadata)
            throw new NotImplementedException();

        return new ValueTask<ReleaseMetadata>((ReleaseMetadata)null);
    }
}