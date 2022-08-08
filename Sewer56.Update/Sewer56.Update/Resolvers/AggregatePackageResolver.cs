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
public class AggregatePackageResolver : IPackageResolver, IPackageResolverDownloadSize, IPackageResolverDownloadUrl
{
    /// <summary>
    /// Number of resolvers internally in this aggregate resolver.
    /// </summary>
    public int Count => _resolverItems.Length;

    /// <summary>
    /// Executed upon a successful download of a package using a given resolver.
    /// </summary>
    public Action<GetResolverResult>? OnSuccessfulDownload;

    private AggregatePackageResolverItem[] _resolverItems;
    private Dictionary<(NuGetVersion, ReleaseMetadataVerificationInfo), List<GetResolverResult>> _getResolversCache = new();
    private bool _hasAcquiredPackages;
    private bool _hasInitialised;

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
        if (_hasInitialised)
            return;

        _hasInitialised = true;
        var tasks = new Task[_resolverItems.Length];
        for (var x = 0; x < _resolverItems.Length; x++)
        {
            var resolver = _resolverItems[x];
            tasks[x] = resolver.Resolver.InitializeAsync();
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception)
        {
            ThrowOnAllTasksFaulted(tasks);
        }
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
        var resolverResults = (await GetResolversForVersionAsync(version, verificationInfo, cancellationToken));
        bool success = false;
        var exceptions = new List<Exception>();

        foreach (var result in resolverResults)
        {
            try
            {
                await result.Resolver.DownloadPackageAsync(version, destFilePath, verificationInfo, progress, cancellationToken);
                OnSuccessfulDownload?.Invoke(result);
                success = true;
                break;
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        if (!success)
            throw new AggregateException(exceptions);
    }

    /// <inheritdoc />
    public async Task<long> GetDownloadFileSizeAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default)
    {
        var resolverResults = (await GetResolversForVersionAsync(version, verificationInfo, token));
        foreach (var result in resolverResults)
        {
            if (result.HasDownloadSize)
                return result.DownloadSize;
        }

        return -1;
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetDownloadUrlAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default)
    {
        var resolverResults = (await GetResolversForVersionAsync(version, verificationInfo, token));
        foreach (var result in resolverResults)
        {
            if (result.DownloadUrl != null)
                return result.DownloadUrl;
        }

        return null;
    }

    /// <summary>
    /// Returns the available update resolvers that would be used to update to a given version.
    /// </summary>
    /// <param name="version">The version to be used for updating.</param>
    /// <param name="verificationInfo">Context used to determine what kind of update packages can be applied.</param>
    /// <param name="token">Token used to cancel the operation used to acquire packages (if first use).</param>
    /// <returns>Details of the resolver used for updating.</returns>
    public async Task<List<GetResolverResult>> GetResolversForVersionAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token)
    {
        // Check cache.
        var versionVerificationTuple = (version, verificationInfo);
        if (_getResolversCache.TryGetValue(versionVerificationTuple, out var result))
            return result;

        await AcquirePackagesIfNecessaryAsync(token);
        result = new List<GetResolverResult>();
        for (var x = 0; x < _resolverItems.Length; x++)
        {
            var resolver = _resolverItems[x];
            if (resolver.Versions.Find(x => x.Equals(version)) != default)
            {
                try
                {
                    long downloadSize   = GetResolverResult.NoDownloadSize;
                    string? downloadUrl = null;

                    if (resolver.Resolver is IPackageResolverDownloadSize downloadSizeResolver)
                        downloadSize = await downloadSizeResolver.GetDownloadFileSizeAsync(version, verificationInfo, token);

                    if (resolver.Resolver is IPackageResolverDownloadUrl downloadUrlResolver)
                        downloadUrl = await downloadUrlResolver.GetDownloadUrlAsync(version, verificationInfo, token);

                    result.Add(new GetResolverResult(resolver.Resolver, x, downloadSize, downloadUrl));
                }
                catch (Exception)
                {
                    result.Add(new GetResolverResult(resolver.Resolver, x));
                }
            }
        }

        // Sort by download size
        result.Sort((a, b) => a.DownloadSize.CompareTo(b.DownloadSize));

        if (result.Count <= 0)
            throw new Exception("Resolver with a specified version was not found.");

        _getResolversCache[versionVerificationTuple] = result;
        return result;
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

    private async Task AcquirePackagesIfNecessaryAsync(CancellationToken token)
    {
        if (_hasAcquiredPackages)
            return;

        var tasks = new Task<List<NuGetVersion>>[_resolverItems.Length];
        for (var x = 0; x < _resolverItems.Length; x++)
            tasks[x] = _resolverItems[x].Resolver.GetPackageVersionsAsync(token);

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception)
        {
            ThrowOnAllTasksFaulted(tasks);
        }

        for (int x = 0; x < _resolverItems.Length; x++)
            _resolverItems[x].Versions = !tasks[x].IsFaulted ? tasks[x].Result : new List<NuGetVersion>();

        _hasAcquiredPackages = true;
    }
    private static void ThrowOnAllTasksFaulted(Task[] tasks)
    {
        if (tasks.All(x => x.IsFaulted))
            throw new AggregateException(tasks.Select(x => x.Exception)!);
    }

    private static void ThrowOnAllTasksFaulted<T>(Task<T>[] tasks)
    {
        if (tasks.All(x => x.IsFaulted))
            throw new AggregateException(tasks.Select(x => x.Exception)!);
    }

    /// <summary>
    /// Encapsulates the result of searching for a resolver supporting a given package version.
    /// </summary>
    public struct GetResolverResult
    {
        internal const long NoDownloadSize = Int64.MaxValue;

        /// <summary/>
        public GetResolverResult(IPackageResolver resolver, int index)
        {
            Resolver = resolver;
            Index = index;
            DownloadSize = NoDownloadSize;
            DownloadUrl = null;
        }

        /// <summary/>
        public GetResolverResult(IPackageResolver resolver, int index, long downloadSize) : this(resolver, index)
        {
            DownloadSize = downloadSize;
        }

        /// <summary/>
        public GetResolverResult(IPackageResolver resolver, int index, long downloadSize, string? url) : this(resolver, index, downloadSize)
        {
            DownloadUrl = url;
        }

        /// <summary>
        /// The resolver to be used for updating.
        /// </summary>
        public IPackageResolver Resolver { get; internal set; }

        /// <summary>
        /// Size of the package to be downloaded.
        /// </summary>
        public long DownloadSize { get; internal set; }

        /// <summary>
        /// URL of the package to be downloaded.
        /// </summary>
        public string? DownloadUrl { get; internal set; }

        /// <summary>
        /// Index of the resolver in the originally supplied array of resolvers.
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        /// True if download size is known, else false.
        /// </summary>
        public bool HasDownloadSize => DownloadSize != NoDownloadSize;
    }

    private struct AggregatePackageResolverItem
    {
        public IPackageResolver Resolver = default!;
        public List<NuGetVersion> Versions = default!;

        public AggregatePackageResolverItem() { }
    }
}