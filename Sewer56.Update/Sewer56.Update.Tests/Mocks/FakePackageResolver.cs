using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Tests.Mocks;

public class FakePackageResolver : IPackageResolver
{
    private readonly List<NuGetVersion> _versions;

    public FakePackageResolver(List<NuGetVersion> versions) => _versions = versions;

    public Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(_versions);

    public Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double> progress = null, CancellationToken cancellationToken = default)
    {
        File.WriteAllText(destFilePath, version.ToString());
        return Task.CompletedTask;
    }
}