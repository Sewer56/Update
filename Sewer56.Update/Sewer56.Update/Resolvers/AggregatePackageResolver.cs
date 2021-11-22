using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Interfaces.Extensions;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Resolvers;

/// <summary>
/// A package resolver that supports downloading of packages from multiple sources.
/// </summary>
public class AggregatePackageResolver : IPackageResolver, IPackageResolverDownloadSize
{
    private AggregatePackageResolverItem[] _resolverItems;
    private bool _hasAcquiredPackages;

    /// <summary/>
    /// <param name="resolvers">A list of existing resolvers.</param>
    public AggregatePackageResolver(List<IPackageResolver> resolvers)
    {
        _resolverItems = new AggregatePackageResolverItem[resolvers.Count];
        for (int x = 0; x < _resolverItems.Length; x++)
            _resolverItems[x].Resolver = resolvers[x];
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        foreach (var resolver in _resolverItems)
            await resolver.Resolver.InitializeAsync();
    }

    /// <inheritdoc />
    public async Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default)
    {
        await AcquirePackagesIfNecessaryAsync(cancellationToken);
        return FlattenVersions();
    }

    /// <inheritdoc />
    public async Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        await AcquirePackagesIfNecessaryAsync(cancellationToken);
        var resolver = GetResolverForVersion(version);
        await resolver.DownloadPackageAsync(version, destFilePath, verificationInfo, progress, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> GetDownloadFileSizeAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default)
    {
        await AcquirePackagesIfNecessaryAsync(default);
        var resolver = GetResolverForVersion(version);
        if (resolver is IPackageResolverDownloadSize downloadSizeProvider)
            return await downloadSizeProvider.GetDownloadFileSizeAsync(version, verificationInfo, token);
            
        return -1;
    }

    private List<NuGetVersion> FlattenVersions()
    {
        var hashSet = new HashSet<NuGetVersion>();
        foreach (var resolver in _resolverItems)
        foreach (var version in resolver.Versions)
            hashSet.Add(version);

        var result = hashSet.ToList();
        result.Sort((a, b) => a.CompareTo(b));
        return result;
    }

    private IPackageResolver GetResolverForVersion(NuGetVersion version)
    {
        foreach (var resolver in _resolverItems)
        {
            if (resolver.Versions.Find(x => x.Equals(version)) != default)
                return resolver.Resolver;
        }

        throw new Exception("Resolver with a specified version was not found.");
    }

    private async Task AcquirePackagesIfNecessaryAsync(CancellationToken token)
    {
        if (_hasAcquiredPackages)
            return;

        for (var x = 0; x < _resolverItems.Length; x++)
            _resolverItems[x].Versions = await _resolverItems[x].Resolver.GetPackageVersionsAsync(token);

        _hasAcquiredPackages = true;
    }

    private struct AggregatePackageResolverItem
    {
        public IPackageResolver Resolver = default!;
        public List<NuGetVersion> Versions = default!;
    }
}