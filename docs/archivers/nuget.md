## NuGet Package Archiver

The `NuGet Package Archiver` can be used to extract or archive NuGet compatible packages with the `.nupkg` extension.

### Example Usage (Extract)

```csharp
// Example: In the UpdateManager API.
await UpdateManager<Empty>.CreateAsync(dummyUpdatee, new LocalPackageResolver(this.OutputFolder), new NuGetPackageExtractor());
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
    PackageArchiver = new new NuGetPackageArchiverSettings()
    {
        Id = "NuGet.Package", // <======= Fill in Required NuSpec components
        Authors = new List<string>() { "Sewer56" },
        Description = "No"
    })
}
```

### About this Implementation

This implementation by default packs files in `contentFiles/any/Sewer56.Update`. 
[This is to roughly match the structure of a real `NuGet` package](https://docs.microsoft.com/en-us/nuget/reference/nuspec#using-the-contentfiles-element-for-content-files).

If the expected directory does not exist, the whole package is extracted instead.