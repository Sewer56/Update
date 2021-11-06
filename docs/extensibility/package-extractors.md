## Package Extractors

You can add support for custom archive formats by extending the `IPackageExtractor` interface, which is defined as:

```csharp
/// <summary>
/// Provider for extracting packages.
/// </summary>
public interface IPackageExtractor
{
    /// <summary>
    /// Extracts contents of the given package to the given output directory.
    /// </summary>
    Task ExtractPackageAsync(string sourceFilePath, string destDirPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default);
}
```

### Example

```csharp
/// <summary>
/// Extracts files from zip-archived packages.
/// </summary>
public class ZipPackageExtractor : IPackageExtractor
{
    /// <inheritdoc />
    public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        // Read the zip
        using var archive = ZipFile.OpenRead(sourceFilePath);

        // For progress reporting
        var totalBytes = archive.Entries.Sum(e => e.Length);
        var totalBytesCopied = 0L;

        // Loop through all entries
        foreach (var entry in archive.Entries)
        {
            // Get destination paths
            var entryDestFilePath = Path.Combine(destDirPath, entry.FullName);
            var entryDestDirPath  = Path.GetDirectoryName(entryDestFilePath);

            // Create directory
            if (!string.IsNullOrWhiteSpace(entryDestDirPath))
                Directory.CreateDirectory(entryDestDirPath);

            // If the entry is a directory - continue
            if (entry.FullName.Last() == Path.DirectorySeparatorChar || entry.FullName.Last() == Path.AltDirectorySeparatorChar)
                continue;

            // Extract entry
            await using var input  = entry.Open();
            await using var output = File.Create(entryDestFilePath);
            using var buffer = new ArrayRental<byte>(65536);

            int bytesCopied;
            do
            {
                bytesCopied = await input.CopyBufferedToAsync(output, buffer.Array, cancellationToken);
                totalBytesCopied += bytesCopied;
                progress?.Report(1.0 * totalBytesCopied / totalBytes);
            } 
            while (bytesCopied > 0);
        }
    }
}
```

### Usage

Use your new package resolver class when creating the update manager, as such:

```csharp
// Create an update manager that updates from filesystem `LocalPackageResolver` and stores packages as zips `ZipPackageExtractor`.
using var manager = await UpdateManager<Empty>.CreateAsync(updatee, new LocalPackageResolver("c:\\test\\release"), new ZipPackageExtractor());
```