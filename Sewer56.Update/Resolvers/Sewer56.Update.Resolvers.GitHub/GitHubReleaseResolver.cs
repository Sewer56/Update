using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Octokit;
using Sewer56.Update.Extensions;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Interfaces.Extensions;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.IO;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Structures;

namespace Sewer56.Update.Resolvers.GitHub;

/// <summary>
/// A package resolver that downloads packages from GitHub releases, with support for response caching.
/// </summary>
public class GitHubReleaseResolver : IPackageResolver, IPackageResolverDownloadSize, IPackageResolverDownloadUrl, IPackageResolverGetLatestReleaseMetadata
{
    private GitHubClient? _client;
    private GitHubResolverConfiguration _configuration;
    private CommonPackageResolverSettings _commonResolverSettings;

    private List<NuGetVersion> _versions = new List<NuGetVersion>();
    
    // For JSON based releases.
    private ReleaseMetadata? _releaseMetadataForNonTag;
    private Release? _gitHubReleaseForNonTag;

    private bool _hasInitialised;

    /// <summary>
    /// A package resolver that uses GitHub as the source, with support for response caching.
    /// </summary>
    /// <param name="configuration">Configuration for the GitHub Resolver.</param>
    /// <param name="resolverSettings">Settings that override how most package resolvers work.</param>
    public GitHubReleaseResolver(GitHubResolverConfiguration configuration, CommonPackageResolverSettings? resolverSettings = null)
    {
        _commonResolverSettings = resolverSettings ?? new CommonPackageResolverSettings();
        _client = GitHubClientInstance.TryGet(out _);
        _configuration = configuration;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (_hasInitialised)
            return;

        _hasInitialised = true;

        // If we failed to obtain GitHub client, we do nothing.
        if (_client == null)
            return;

        // Initialise, from tag or not.
        try
        {
            var releases = await _client.Repository.Release.GetAll(_configuration.UserName, _configuration.RepositoryName);
            var releasesEnumerable = releases.AsEnumerable();
            if (!_commonResolverSettings.AllowPrereleases)
                releasesEnumerable = releases.Where(x => x.Prerelease == false);

            if (_configuration.InheritVersionFromTag)
            {
                var result = releasesEnumerable.Select(x => new NuGetVersion(x.TagName)).ToList();
                result.Sort((a, b) => a.CompareTo(b));
                _versions = result;
            }
            else
            {
                _gitHubReleaseForNonTag = releasesEnumerable.OrderByDescending(x => x.PublishedAt).First();
                _releaseMetadataForNonTag = await TryGetReleaseMetadataAsync(_gitHubReleaseForNonTag);
                if (_releaseMetadataForNonTag != null)
                    _versions = _releaseMetadataForNonTag.GetNuGetVersionsFromReleaseMetadata(_commonResolverSettings.AllowPrereleases);
            }
        }
        catch (Exception) { /* Ignored */ }
    }

    /// <inheritdoc />
    public Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(_versions);

    /// <inheritdoc />
    public async Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var downloadUrl  = await GetVersionDownloadUrl(version, verificationInfo);

        // Create a WebRequest to get the file & create a response. 
        var fileReq  = WebRequest.CreateHttp(downloadUrl);
        var fileResp = await fileReq.GetResponseAsync();
        await using var responseStream = fileResp.GetResponseStream();
        await using var targetFile = File.Open(destFilePath, System.IO.FileMode.Create);
        await responseStream!.CopyToAsyncEx(targetFile, 262144, progress, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> GetDownloadFileSizeAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default)
    {
        var url = await GetVersionDownloadUrl(version, verificationInfo);
        var fileReq = WebRequest.CreateHttp(url);
        return (await fileReq.GetResponseAsync()).ContentLength;
    }

    private async Task<string> GetVersionDownloadUrl(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo)
    {
        Release release;
        if (_configuration.InheritVersionFromTag)
        {
            var releases = await _client!.Repository.Release.GetAll(_configuration.UserName, _configuration.RepositoryName);
            release = releases.FirstOrDefault(x => new NuGetVersion(x.TagName).Equals(version))!;
            if (release == null)
                throw new GitHubResolverException($"No suitable GitHub Release found at {_configuration.UserName}/{_configuration.RepositoryName}");
        }
        else
        {
            release = _gitHubReleaseForNonTag!;
        }

        var releaseItemName = await GetReleaseItemName(version, verificationInfo, release);
        if (string.IsNullOrEmpty(releaseItemName))
            throw new GitHubResolverException($"Failed to find a download at {_configuration.UserName}/{_configuration.RepositoryName}");

        var packageAsset = release.Assets.First(x => x.Name == releaseItemName);
        return packageAsset.BrowserDownloadUrl;
    }

    /// <summary>
    /// Tries to get the name of the item to download.
    /// </summary>
    /// <param name="version">The release version to get asset name of.</param>
    /// <param name="verificationInfo">Used for delta updates.</param>
    /// <param name="release">The GitHub release associated with the item.</param>
    private async Task<string?> GetReleaseItemName(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, Release release)
    {
        string? releaseItemName = null;
        var releaseMetadata = await TryGetReleaseMetadataAsync(release);
        if (releaseMetadata != null)
        {
            releaseItemName = releaseMetadata.GetRelease(version.ToString(), verificationInfo)!.FileName;
        }
        else
        {
            if (!string.IsNullOrEmpty(_configuration.LegacyFallbackPattern))
                releaseItemName = release.Assets.FirstOrDefault(asset => WildcardPattern.IsMatch(asset.Name, _configuration.LegacyFallbackPattern))?.Name;
        }

        return releaseItemName;
    }

    /// <summary>
    /// Tries to get the release metadata from a given GitHub release.
    /// </summary>
    /// <param name="release">The release for which to get the metadata for.</param>
    /// <returns>The release metadata, if it can be found, else null.</returns>
    private async Task<ReleaseMetadata?> TryGetReleaseMetadataAsync(Release? release)
    {
        if (_releaseMetadataForNonTag != null && !_configuration.InheritVersionFromTag)
            return _releaseMetadataForNonTag;

        var possibleMetadataNames = JsonCompressionExtensions.GetPossibleFilePaths(_commonResolverSettings.MetadataFileName);
        var releaseMetadataAsset = release!.Assets.FirstOrDefault(x => possibleMetadataNames.Contains(x.Name));

        // Find Release File
        if (releaseMetadataAsset != null)
        {
            using var webClient = new WebClient();
            var compressionScheme = JsonCompressionExtensions.GetCompressionFromFileName(releaseMetadataAsset.Name);
            var releaseMetadataBytes = await webClient.DownloadDataTaskAsync(releaseMetadataAsset.BrowserDownloadUrl);
            return await Singleton<ReleaseMetadata>.Instance.ReadFromDataAsync(releaseMetadataBytes, compressionScheme);
        }

        return null;
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetDownloadUrlAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default) => await GetVersionDownloadUrl(version, verificationInfo);

    /// <inheritdoc />
    public async ValueTask<ReleaseMetadata?> GetReleaseMetadataAsync(CancellationToken token)
    {
        Release release;
        if (_configuration.InheritVersionFromTag)
        {
            var releases = await _client!.Repository.Release.GetAll(_configuration.UserName, _configuration.RepositoryName);
            release = releases.FirstOrDefault(x => new NuGetVersion(x.TagName).Equals(_versions.Last()))!;
            if (release == null)
                return null;
        }
        else
        {
            release = _gitHubReleaseForNonTag!;
        }

        return await TryGetReleaseMetadataAsync(release);
    }
}