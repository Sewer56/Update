## Prerequisites

### Install Command Line Handler

In your application's startup/main function, install a handler to `Update`.
This handler is used for copying files over to the destination directory.

```csharp
// Handles command line arguments passed to app.
if (Startup.HandleCommandLineArgs(args))
{
    /* If true, cleanly exit application here */ 
}
```

Note: *This is only required when performing updates on the application you are currently running `Update` from.*

## Basic Usage

### Simple Example

```csharp
// Create an update manager that updates from filesystem `LocalPackageResolver` and stores packages as zips `ZipPackageExtractor`.
using var manager = await UpdateManager<Empty>.CreateAsync(updatee, new LocalPackageResolver("c:\\test\\release"), new ZipPackageExtractor());

// Check for new version and, if available, perform full update to latest version.
if (await manager.CheckPerformUpdateAsync(new OutOfProcessOptions(), progressHandler))
{
    /* 
        Returns true if shutting down the application is required.
        So cleanly exit application here. 
    */
}
```

### Handling Update Steps Manually

To provide users the most optimal experience, you might probably want to handle intermediate steps manually.


```csharp
var result = await manager.CheckForUpdatesAsync();
if (result.CanUpdate)
{
    // Downloads and extracts the package in the background
    // (supports progress reporting and cancellation)
    await manager.PrepareUpdateAsync(result.LastVersion);

    // Launch an executable that will apply the update
    // (can be instructed to restart the application afterwards)
    if (await manager.StartUpdateAsync(result.LastVersion, outOfProcessOptions)) 
    {
        /* 
            Returns true if shutting down the application is required.
            So cleanly exit application here. 
        */
    }
}
```

## Appendix

If you are curious about the purpose of `Empty` generic item, refer to [Extra Package Metadata](./extensibility/package-custom-metadata.md).