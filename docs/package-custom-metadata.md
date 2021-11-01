## Custom Package Metadata

It is possible to specify custom metadata to apply to packages. 
Many APIs, such as `ReleaseBuilder` have a generic `<T>` type that can be used for holding additional data.

This additional data is used in tandem with APIs which add items, such as `ReleaseBuilder<T>.AddCopyPackage()`.

### Building a Package with Custom Metadata

Consider the following snippet using `ReleaseBuilder<T>`.

```csharp
var builder = new ReleaseBuilder<string>();
builder.AddCopyPackage(new CopyBuilderItem<Empty>()
{
    FolderPath = Assets.ManyFileFolderOriginal,
    Version = "1.0",
    Data = "This is a string attached to an individual release package." // <= Custom Data
});
```

### Reading Custom Metadata from Package

If you are handling the intermediate update steps manually, you can get custom package metadata as soon as the update has been prepared.

Consider a situation where you are handling the intermediate steps manually:

```csharp
var result = await manager.CheckForUpdatesAsync();
if (result.CanUpdate)
{
    // Downloads and extracts the package in the background
    // (supports progress reporting and cancellation)
    await manager.PrepareUpdateAsync(result.LastVersion);

    // Get the metadata from the package.
    var packageMetadata = await updateManager.TryGetPackageMetadataAsync(result.LastVersion);
}
```