## Package Compressors

You can add support for creating releases with custom archive formats by extending the `IPackageArchiver` interface, which is defined as:

```csharp
/// <summary>
/// Provider for archiving packages.
/// </summary>
public interface IPackageArchiver
{
    /// <summary>
    /// Extracts contents of the given package to the given output directory.
    /// </summary>
    /// <param name="relativeFilePaths">List of relative paths of files to compress.</param>
    /// <param name="baseDirectory">The base directory to which these paths are relative to.</param>
    /// <param name="destPath">The path where to save the package.</param>
    /// <param name="progress">Reports progress back.</param>
    /// <param name="cancellationToken">Can be used to cancel the operation.</param>
    Task CreateArchiveAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
```

### Example

```csharp
/// <summary>
/// Compresses packages into a zip file using the DEFLATE compression algorithm.
/// </summary>
public class ZipPackageArchiver : IPackageArchiver
{
    /// <inheritdoc />
    public async Task CompressPackageAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        await using var zipStream = new FileStream(destPath, FileMode.Create);
        using var zipFile         = new ZipArchive(zipStream, ZipArchiveMode.Update);
        var progressSlicer        = new ProgressSlicer(progress);

        for (var x = 0; x < relativeFilePaths.Count; x++)
        {
            var progressForFile = progressSlicer.Slice((float)x / relativeFilePaths.Count);
            var relativePath    = relativeFilePaths[x];

            var entry = zipFile.CreateEntry(relativePath, CompressionLevel.Optimal);
            await using var sourceStream = File.Open(Paths.AppendRelativePath(relativePath, baseDirectory), FileMode.Open, FileAccess.Read);
            await using var entryStream  = entry.Open();

            await sourceStream.CopyToAsyncEx(entryStream, 65536, progressForFile, cancellationToken);
        }

        progress?.Report(1);
    }
}
```

### Usage

Specify `IPackageArchiver` in `BuildArgs` when creating a new release with `ReleaseBuilder`.

```csharp
var metadata = await builder.BuildAsync(new BuildArgs()
{
    FileName = "Package",
    OutputFolder = this.OutputFolder,
    PackageCompressor = new ZipPackageArchiver()
});
```