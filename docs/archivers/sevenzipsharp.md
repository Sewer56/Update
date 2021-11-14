## SevenZipSharp Archiver

[Warning: ***Windows Only***]  
[Found In: `Sewer56.Update.Extractors.SevenZipSharp`]  

The `SevenZipSharp Archiver` is based on [Squid Box's fork of SevenZipSharp](https://github.com/squid-box/SevenZipSharp), a wrapper for the native `7z.dll` on Windows.

The implementation is actively tested with the following containers:  
- Zip  
- 7z  

And the following compression formats:  
- Deflate  
- Deflate64  
- LZMA  
- LZMA2   
- PPMd  

Other formats supported by the library should however still work.

### Example Usage (Extract)

```csharp
// Example: In the UpdateManager API.
await UpdateManager<Empty>.CreateAsync(dummyUpdatee, new LocalPackageResolver(this.OutputFolder), new SevenZipSharpExtractor());
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
    PackageArchiver = new SevenZipSharpArchiver(new SevenZipSharpArchiverSettings()
    {
        CompressionLevel = CompressionLevel.Ultra,
        ArchiveFormat = OutArchiveFormat.SevenZip,
        CompressionMethod = CompressionMethod.Lzma2
    })
}
```