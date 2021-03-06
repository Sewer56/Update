## GameBanana Package Resolver

[Found In: `Sewer56.Update.Resolvers.GameBanana`]  

The `GameBanana Package Resolver` obtains releases from a GameBanana Mod page.

### Example Usage

#### Receiving Updates
```csharp
// Use GameBanana Package Resolver with a specified Repo as Config
var resolver = new GameBananaUpdateResolver(new GameBananaResolverConfiguration()
{
    ItemId = 333681 // Taken from URL, e.g. https://gamebanana.com/mods/333681
});

await resolver.InitializeAsync();
var versions = await resolver.GetPackageVersionsAsync();
await resolver.DownloadPackageAsync(versions[0], packageFilePath, new ReleaseMetadataVerificationInfo() { FolderPath = this.OutputFolder });
```

#### Release Building

```csharp
var metadata = await builder.BuildAsync(new BuildArgs()
{
    FileName = "Package",
    OutputFolder = this.OutputFolder,
    FileNameFilter = GameBananaUtilities.SanitizeFileName
});
```

You ***must*** use the file name sanitizer `GameBananaUtilities.SanitizeFileName` when building releases to be uploading to GameBanana.
Not doing so risks GameBanana trimming the end of the file names, making the updater not able to pick out the correct package to update from.

### About this Implementation

This implementation is a bit quirky especially due issues with file names specific to GameBanana.
GameBanana uses a set of rules for file names, which can be are pretty challenging to work with.

#### File Name Length
GameBanana currently has a 100 character limit for file names (+ extension).
Before 08 Nov 2021, it was a 40 character limit, which was tough!. 

#### File Name Deduplication

If a file with a given name has been uploaded prior, the file name will has a 6 character string appended to the end to make it unique. 
If the resulting string is too long, characters will be cut from the original string.

Any file name with over 94 characters risks the loss of version number from its name,
making the resolver unable to determine the correct package to update from.

As such. package names, including version information and extension must be limited to 94 characters max.

### File Name Character Set
GameBanana only supports lowercase AlphaNumeric characters, underscores `_` and dashes `-` in file names.

### File Format Allowance
GameBanana does not allow you to upload non archive formats (e.g. `.json`).
Release metadata has to be wrapped in a dummy `zip` archive, which often is wasteful as it will result in a larger file size. (e.g. 304 bytes instead of 237 bytes)