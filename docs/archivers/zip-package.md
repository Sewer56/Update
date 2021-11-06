## Zip Package Archiver

The `Zip Package Archiver` can be used to extract or archive packages using the zip container and the DEFLATE algorithm.

### Example Usage (Extract)

```csharp
// Example: In the UpdateManager API.
await UpdateManager<Empty>.CreateAsync(dummyUpdatee, new LocalPackageResolver(this.OutputFolder), new ZipPackageExtractor());
```

### Example Usage (Compress)

Note: This is the default archiver and will be used if none is specified.

```csharp
// Example: In the ReleaseBuilder API.
// builder == ReleaseBuilder
var metadata = await builder.BuildAsync(new BuildArgs()
{
   FileName = "Package",
     OutputFolder = this.OutputFolder,
    PackageCompressor = new ZipPackageCompressor() // <=======
```