## Structure of a Release

The following diagram shows a high level overview of the files/components involved in each release.

```
                      +--------------------+       +-------------------+
                      | RELEASE MANIFEST   |       | DELTA MANIFEST    |
                      +--------------------+       +-------------------+
                      | A list of:         |       | Used if Package   |
                      | - Package FileName +-------+ Type is Delta     |
          +-----------+ - Package Version  |       +-------------------+
          |           | - Package Type     |       | - Expected Hashes |	   
+---------+--------+  | - Target Version   |       +-------------------+
| RELEASE          |  +--------------------+
+------------------+
| Release Manifest |
| Package 1        +----+
| Package 2        |	|
+------------------+    |                       +------------------------+
                      +-+-----------------+     | PACKAGE MANIFEST       |
                      | PACKAGE        	  |     +------------------------+
                      +-------------------+     | - Package Type         |
                      | Package Manifest  +-----+ - Package Version      |
                      | <Package Files>   |     | - List of Files        |
                      +-------------------+     | - Cleanup/Ignore Regex |
												| - Custom Extra Data    |
												+------------------------+
```

## Release Creation

### Using the CLI

For creating fully featured update packages, a separate program called `Update.Tools` is provided. This tool is cross-platform and should be usable from any environment if .NET Core itself is available.

You can find the tool in the `releases` section of the official GitHub repository.  

Usage:  

```
  --copypackagespath     Path to a CSV file with all packages to be copied.
                         Entry should have format: "PathToFolder,Version"

  --deltapackagespath    Path to a CSV file with all packages to be delta
                         patched. Entry should have format:
                         "PathToCurrentVersionFolder,CurrentVersion,PathToPrevio
                         usVersionFolder,PreviousVersion"

  --outputpath           Required. The folder where to save the new release.

  --packagename          (Default: Package) The name for the packages as
                         downloaded by the user.

  --ignoreregexespath    Path to a text file containing a list of regular
                         expressions (1 expression per line) of files to be
                         ignored in the packages.
```

Example:

- Copy.csv
```
C:\Test\reloaded.sharedlib.hooks_1.12,1.12.0
```

- Delta.csv
```
C:\Test\reloaded.sharedlib.hooks_1.12,1.12.0,C:\Test\reloaded.sharedlib.hooks_1.11,1.11.0
```

- Command
```csharp
// Create a simple release, with full fat update details specified in
// Copy.csv and delta updates from previous versions in Delta.csv.
dotnet Sewer56.Update.Tool.dll --copypackagespath "Copy.csv" --deltapackagespath "Delta.csv" --outputpath out
```

### Using the API

You can build a fresh release by using the `ReleaseBuilder<T>` API.
Here is an example:

```csharp
// Arrange
ar builder = new ReleaseBuilder<Empty>();
builder.AddCopyPackage(new CopyBuilderItem<Empty>()
{
    FolderPath = Assets.ManyFileFolderOriginal,
    Version = "1.0"
});

// Act
var metadata = await builder.BuildAsync(new BuildArgs()
{
    FileName     = "Package",
    OutputFolder = this.OutputFolder
});
```

This will build a release with 1 package at `Assets.ManyFileFolderOriginal` and place the release into `OutputFolder`.

## Support for Legacy Releases

For legacy reasons, it is possible to work without this application, however you lose support for cleanup and delta updates (Reason: No manifest file).