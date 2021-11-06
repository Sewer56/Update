## SharpCompress Archiver

The `SharpCompress Archiver` can be used to extract or archive packages using various container formats and compression algorithms.

The implementation is actively tested with the following containers:  
- Zip  
- Tar  

And the following compression formats:  
- Deflate  
- BZip2  
- LZMA  
- PPMd  

Other formats supported by the library should however still work.

[Full list of formats](https://github.com/adamhathcock/sharpcompress/blob/master/FORMATS.md).

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