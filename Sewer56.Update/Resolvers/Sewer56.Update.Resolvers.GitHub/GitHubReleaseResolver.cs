﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Octokit;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Interfaces.Extensions;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.IO;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Structures;
using FileMode = Octokit.FileMode;

namespace Sewer56.Update.Resolvers.GitHub;

/// <summary>
/// A package resolver that downloads packages from GitHub releases, with support for response caching.
/// </summary>
public class GitHubReleaseResolver : IPackageResolver, IPackageResolverDownloadSize
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
            if (!_commonResolverSettings.AllowPrereleases)
                releasesEnumerable = releases.Where(x => x.Prerelease == false);

            var result = releasesEnumerable.Select(x => new NuGetVersion(x.TagName)).ToList();
            result.Sort((a, b) => a.CompareTo(b));
            return result;
        }
        catch (Exception)
        {
            return new List<NuGetVersion>();
        }
    }

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
        var releases = await _client!.Repository.Release.GetAll(_configuration.UserName, _configuration.RepositoryName);
        var release = releases.FirstOrDefault(x => new NuGetVersion(x.TagName).Equals(version));
        var possibleMetadataNames = JsonCompressionExtensions.GetPossibleFilePaths(_commonResolverSettings.MetadataFileName);
        var releaseMetadataAsset  = release!.Assets.FirstOrDefault(x => possibleMetadataNames.Contains(x.Name));

        using var webClient = new WebClient();
        string? releaseItemName = null;

        // Find Release File
        if (releaseMetadataAsset != null)
        {
            var compressionScheme = JsonCompressionExtensions.GetCompressionFromFileName(releaseMetadataAsset.Name);
            var releaseMetadataBytes = await webClient.DownloadDataTaskAsync(releaseMetadataAsset.BrowserDownloadUrl);
            var releaseMetadata = await Singleton<ReleaseMetadata>.Instance.ReadFromDataAsync(releaseMetadataBytes, compressionScheme);
            releaseItemName = releaseMetadata.GetRelease(version.ToString(), verificationInfo)!.FileName;
        }
        else
        {
            if (!string.IsNullOrEmpty(_configuration.LegacyFallbackPattern))
                releaseItemName = release.Assets.FirstOrDefault(asset => WildcardPattern.IsMatch(asset.Name, _configuration.LegacyFallbackPattern))?.Name;
        }

        if (string.IsNullOrEmpty(releaseItemName))
            throw new GitHubResolverException($"Failed to find a download at {_configuration.UserName}/{_configuration.RepositoryName}");

        var packageAsset = release.Assets.First(x => x.Name == releaseItemName);
        return packageAsset.BrowserDownloadUrl;
    }
}