## NuGet Package Resolver

[Found In: `Sewer56.Update.Resolvers.NuGet`]  

The `NuGet Package Resolver` obtains releases from a NuGet V3 Feed.  

Note: *The NuGet implementation does not support delta updates.* 

### Example Usage

#### Receiving Updates
```csharp
var resolver = new NuGetUpdateResolver(new NuGetUpdateResolverSettings()
{
    NugetRepository = new NugetRepository("https://api.nuget.org/v3/index.json"), // Index URL
    PackageId = "Totally.Not.Newtonsoft.Json" // Package ID
});

var versions = await resolver.GetPackageVersionsAsync();
await resolver.DownloadPackageAsync(versions[0], packageFilePath, new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });
```