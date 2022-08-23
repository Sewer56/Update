## Package Resolvers

You can add support for downloading releases from custom sources by extending the `IPackageResolver` interface, which is defined as:

```csharp
/// <summary>
/// Provider for resolving packages.
/// </summary>
public interface IPackageResolver
{
    /// <summary>
    /// Called only once.
    /// 
    /// Use this for performing any asynchronous initialization (that you cannot do in the constructor) such as
    /// - Reading from cache.
    /// - Fetching release metadata.
    /// - Fetching resources from a remote server.
    /// Use this for any asynchronous operations that would be required in your constructor.
    /// </summary>
    Task InitializeAsync() { return Task.CompletedTask; }

    /// <summary>
    /// Returns all available package versions.
    /// </summary>
    /// <remarks>
    ///     If you have release metadata, available consider using <see cref="NuGetExtensions.GetNuGetVersionsFromReleaseMetadata(ReleaseMetadata)"/>.
    ///     If the source provides its own release system with versions (e.g. GitHub API), use the versions returned from the API call.
    /// </remarks>
    Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads given package version.
    /// </summary>
    /// <remarks>
    ///     If source uses its own release system e.g. GitHub, download the release metadata here first.
    ///     Pass <see cref="ReleaseMetadataVerificationInfo"/> to <see cref="ReleaseMetadata.GetRelease"/>.
    ///     Get file name from returned item, and download using that file name.
    /// </remarks>
    /// <param name="progress">Reports progress about the package download.</param>
    /// <param name="cancellationToken">Allows for cancellation of the operation.</param>
    /// <param name="version">The version to be downloaded.</param>
    /// <param name="destFilePath">File path where the item should be downloaded to.</param>
    /// <param name="verificationInfo">
    ///     Pass this to <see cref="ReleaseMetadata.GetRelease"/>.
    ///     This is the information required to verify whether some package types, e.g. Delta Packages can be applied.
    /// </param>
    Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
```

### Example

```csharp
/// <summary>
/// Resolves packages from a local folder.
/// Local folder must contain manifest for each package.
/// </summary>
public class LocalPackageResolver : IPackageResolver
{
    private ReleaseMetadata? _releases;
    private string _repositoryFolder;

    /// <summary/>
    /// <param name="repositoryFolder">Folder containing packages and package manifest.</param>
    public LocalPackageResolver(string repositoryFolder)
    {
        _repositoryFolder = repositoryFolder;
    }

    /// <inheritdoc />
    public async Task InitializeAsync() => _releases = await Singleton<ReleaseMetadata>.Instance.ReadFromDirectoryAsync(_repositoryFolder);

    /// <inheritdoc />
    public Task<List<NuGetVersion>> GetPackageVersionsAsync(CancellationToken cancellationToken = default) => Task.FromResult(_releases!.GetNuGetVersionsFromReleaseMetadata());

    /// <inheritdoc />
    public async Task DownloadPackageAsync(NuGetVersion version, string destFilePath, ReleaseMetadataVerificationInfo verificationInfo, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var releaseItem = _releases!.GetRelease(version.ToString(), verificationInfo);
        await using var sourceFile = File.Open(Path.Combine(_repositoryFolder, releaseItem!.FileName), FileMode.Open);
        await using var targetFile = File.Open(destFilePath, FileMode.Create);
        await sourceFile.CopyToAsyncEx(targetFile, 65536, progress, cancellationToken);
    }
}
```

The following implementation would allow you to download packages from a local folder on your machine.

### Usage

Use your new package resolver class when creating the update manager, as such:

```csharp
// Create an update manager that updates from filesystem `LocalPackageResolver` and stores packages as zips `ZipPackageExtractor`.
using var manager = await UpdateManager<Empty>.CreateAsync(updatee, new LocalPackageResolver("c:\\test\\release"), new ZipPackageExtractor());
```

## Extensions

Some resolvers may support additional (optional) extensions such as `IPackageResolverDownloadSize` which allows you to get the download size of a package before downloading it.  

Example usage:  
```csharp
// Get file size (if supported)
if (resolver is IPackageResolverDownloadSize downloadSizeProvider)
    fileSize = await downloadSizeProvider.GetDownloadFileSizeAsync(version, verificationInfo, token);
```

Available Extensions:  

| Type                               | Description                                       |
|------------------------------------|---------------------------------------------------|
| IPackageResolverDownloadSize       | Returns the size of the package to be downloaded. |
| IPackageResolverDownloadUrl        | Returns direct download URL for the package.      |
| IPackageResolverGetReleaseMetadata | Retrieves the release metadata file.              |