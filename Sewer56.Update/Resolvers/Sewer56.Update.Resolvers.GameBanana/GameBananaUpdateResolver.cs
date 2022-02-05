using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.Update.Extensions;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Interfaces.Extensions;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.IO;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Resolvers.GameBanana.Structures;
using Sewer56.Update.Structures;

namespace Sewer56.Update.Resolvers.GameBanana;

/// <summary>
/// A package resolver that allows people to receive updates performed via gamebanana.
/// </summary>
public class GameBananaUpdateResolver : IPackageResolver, IPackageResolverDownloadSize
{
    private GameBananaResolverConfiguration _configuration;
    private CommonPackageResolverSettings _commonResolverSettings;

    private ReleaseMetadata? _releases;
    private GameBananaItem? _gbItem;

    /// <summary>
    /// Creates a new instance of the GameBanana update resolver.
    /// </summary>
    /// <param name="configuration">Configuration specific to GameBanana.</param>
    /// <param name="commonResolverSettings">Configuration settings shared between all items.</param>
    public GameBananaUpdateResolver(GameBananaResolverConfiguration configuration, CommonPackageResolverSettings? commonResolverSettings = null)
    {
        _configuration = configuration;
        _commonResolverSettings = commonResolverSettings ?? new CommonPackageResolverSettings();
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _gbItem   = await GameBananaItem.FromTypeAndIdAsync(_configuration.ModType, _configuration.ItemId);
        if (_gbItem == null)
            return;

        if (_gbItem.Files == null)
            throw new KeyNotFoundException("GameBanana did not return a file list for the given mod page.");

        // Download Metadata
        var metadataFile = GetGameBananaMetadataFile(_gbItem.Files, out var isZip);
        if (metadataFile == null || string.IsNullOrEmpty(metadataFile.DownloadUrl))
            return;

        using var client = new WebClient();
        var bytes = await client.DownloadDataTaskAsync(metadataFile.DownloadUrl);
        if (isZip)
        {
            await using var memoryStream    = new MemoryStream(bytes);
            using var zipFile               = new ZipArchive(memoryStream);
            var firstEntry                  = zipFile.Entries.First();
            await using var metadataStream  = firstEntry.Open(); 
            var compressionScheme = JsonCompressionExtensions.GetCompressionFromFileName(firstEntry.Name);
            _releases = await Singleton<ReleaseMetadata>.Instance.ReadFromStreamAsync(metadataStream, compressionScheme);
            return;
        }

        _releases = await Singleton<ReleaseMetadata>.Instance.ReadFromDataAsync(bytes);
    }

    /// <inheritdoc />
    public Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default)
    {
        if (_releases == null)
            return Task.FromResult(new List<NuGetVersion>());
        
        return Task.FromResult(_releases.GetNuGetVersionsFromReleaseMetadata(_commonResolverSettings.AllowPrereleases));
    }

    /// <inheritdoc />
    public async Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        if (_releases == null || _gbItem == null)
            return;
        
        var downloadUrl = GetVersionDownloadUrl(version, verificationInfo);

        //Create a WebRequest to get the file & create a response. 
        var fileReq  = WebRequest.CreateHttp(downloadUrl);
        var fileResp = await fileReq.GetResponseAsync();
        await using var responseStream = fileResp.GetResponseStream();
        await using var targetFile = File.Open(destFilePath, System.IO.FileMode.Create);
        await responseStream.CopyToAsyncEx(targetFile, 262144, progress, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> GetDownloadFileSizeAsync(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo, CancellationToken token = default)
    {
        if (_releases == null || _gbItem == null)
            return -1;

        var url = GetVersionDownloadUrl(version, verificationInfo);
        var fileReq = WebRequest.CreateHttp(url);
        return (await fileReq.GetResponseAsync()).ContentLength;
    }

    private string GetVersionDownloadUrl(NuGetVersion version, ReleaseMetadataVerificationInfo verificationInfo)
    {
        var releaseItem = _releases!.GetRelease(version.ToString(), verificationInfo);
        if (releaseItem == null)
            throw new ArgumentException($"Unable to find Release for the specified NuGet Version `{nameof(version)}` ({version})");
        
        if (TryGetGameBananaFile(_gbItem!.Files!, releaseItem.FileName, out var isZip, out var gbItemFile))
            return gbItemFile!.DownloadUrl!;

        return "";
    }

    private GameBananaItemFile? GetGameBananaMetadataFile(Dictionary<string, GameBananaItemFile> files, out bool isZip)
    {
        var possibleMetadataNames = JsonCompressionExtensions.GetPossibleFilePaths(_commonResolverSettings.MetadataFileName);
        if (TryGetGameBananaFile(files, possibleMetadataNames, out isZip, out var gameBananaItemFile))
            return gameBananaItemFile;
        
        isZip = false;
        return null;
    }

    private static bool TryGetGameBananaFile(Dictionary<string, GameBananaItemFile> files, IEnumerable<string> fileNames, out bool isZip, out GameBananaItemFile? gameBananaItemFile)
    {
        foreach (var fileName in fileNames)
        {
            if (TryGetGameBananaFile(files, fileName, out isZip, out gameBananaItemFile))
                return true;
        }

        isZip = false;
        gameBananaItemFile = null;
        return false;
    }

    private static bool TryGetGameBananaFile(Dictionary<string, GameBananaItemFile> files, string fileName, out bool isZip, out GameBananaItemFile? gameBananaItemFile)
    {
        var possibleFileNames = GameBananaUtilities.GetFileNameStarts(fileName);
        foreach (var possibleFileName in possibleFileNames)
        {
            foreach (var file in files)
            {
                if (!file.Value.FileName!.StartsWith(possibleFileName))
                    continue;

                isZip = Path.GetExtension(file.Value.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase);
                gameBananaItemFile = file.Value;
                return true;
            }
        }

        isZip = false;
        gameBananaItemFile = null;
        return false;
    }
}