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

namespace Sewer56.Update.Tests.Mocks;

[ExcludeFromCodeCoverage]
internal class ExceptionPackageResolver : IPackageResolver, IPackageResolverDownloadSize
{
    private readonly bool _throwOnInitialize;
    private readonly bool _throwOnGetPackageVersions;
    private readonly bool _throwOnDownloadPackage;
    private readonly bool _throwOnGetDownloadFileSize;
    private readonly List<NuGetVersion> _versions = new List<NuGetVersion>();

    public ExceptionPackageResolver(bool throwOnInitialize, bool throwOnGetPackageVersions, bool throwOnDownloadPackage, bool throwOnGetDownloadFileSize = false, List<NuGetVersion> versions = null)
    {
        _throwOnInitialize = throwOnInitialize;
        _throwOnGetPackageVersions = throwOnGetPackageVersions;
        _throwOnDownloadPackage = throwOnDownloadPackage;
        _throwOnGetDownloadFileSize = throwOnGetDownloadFileSize;
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
        IProgress<double>? progress = null, CancellationToken cancellationToken = default)
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
}