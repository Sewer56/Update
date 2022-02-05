using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Tests.Mocks;

[ExcludeFromCodeCoverage]
internal class ExceptionPackageResolver : IPackageResolver
{
    private readonly bool _throwOnInitialize;
    private readonly bool _throwOnGetPackageVersions;
    private readonly bool _throwOnDownloadPackage;

    public ExceptionPackageResolver(bool throwOnInitialize, bool throwOnGetPackageVersions, bool throwOnDownloadPackage)
    {
        _throwOnInitialize = throwOnInitialize;
        _throwOnGetPackageVersions = throwOnGetPackageVersions;
        _throwOnDownloadPackage = throwOnDownloadPackage;
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

        return new List<NuGetVersion>();
    }

    public async Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo,
        IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_throwOnDownloadPackage)
            throw new NotImplementedException();
    }
}