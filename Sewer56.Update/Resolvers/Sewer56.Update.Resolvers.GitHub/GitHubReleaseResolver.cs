using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Octokit;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Structures;
using FileMode = Octokit.FileMode;

namespace Sewer56.Update.Resolvers.GitHub;

/// <summary>
/// A package resolver that downloads packages from GitHub releases, with support for response caching.
/// </summary>
public class GitHubReleaseResolver : IPackageResolver
{
    private GitHubClient? _client;
    private GitHubResolverConfiguration _configuration;
    private CommonPackageResolverSettings _commonResolverSettings;

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
    public async Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default)
    {
        if (_client == null)
            return new List<NuGetVersion>();

        try
        {
            var releases = await _client.Repository.Release.GetAll(_configuration.UserName, _configuration.RepositoryName);
            var releasesEnumerable = releases.AsEnumerable();
            if (!_configuration.AllowPrereleases)
                releasesEnumerable = releases.Where(x => x.Prerelease == false);

            var result = releasesEnumerable.Select(x => new NuGetVersion(x.TagName)).ToList();
            result.Sort((a, b) => a.CompareTo(b));
            return result;
        }
        catch (Exception ex)
        {
            return new List<NuGetVersion>();
        }
    }

    /// <inheritdoc />
    public async Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var releases   = await _client.Repository.Release.GetAll(_configuration.UserName, _configuration.RepositoryName);
        var release    = releases.FirstOrDefault(x => new NuGetVersion(x.TagName).Equals(version));
        var releaseMetadataAsset = release.Assets.First(x => x.Name == _commonResolverSettings.MetadataFileName);

        using var webClient      = new WebClient();
        var releaseMetadataBytes = await webClient.DownloadDataTaskAsync(releaseMetadataAsset.BrowserDownloadUrl);
        var releaseMetadata      = Singleton<ReleaseMetadata>.Instance.ReadFromData(releaseMetadataBytes);

        var releaseItem  = releaseMetadata.GetRelease(version.ToString(), verificationInfo);
        var packageAsset = release.Assets.First(x => x.Name == releaseItem.FileName);

        //Create a WebRequest to get the file & create a response. 
        var fileReq  = WebRequest.CreateHttp(packageAsset.BrowserDownloadUrl);
        var fileResp = await fileReq.GetResponseAsync();
        await using var responseStream = fileResp.GetResponseStream();
        await using var targetFile = File.Open(destFilePath, System.IO.FileMode.Create);
        await responseStream.CopyToAsyncEx(targetFile, 262144, progress, cancellationToken);
    }
}