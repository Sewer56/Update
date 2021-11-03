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

A package contains an individual version of a program; a release is a collection of packages storing multiple versions.
Packages you install, Releases you upload/publish to the web.

## Creating Packages

For creating fully featured update packages using CLI, a separate program called `Sewer56.Update.Tool` is provided.  
This tool is cross-platform and should be usable from any environment if .NET Core itself is available.

You can find the tool in the `releases` section of the official GitHub repository.  

### Using the CLI

```bash
// Create a Copy Package (files are copied directly to destination).
dotnet Sewer56.Update.Tool.dll CreateCopyPackage --folderpath "Files/NewVersion" --version "1.0" --outputpath "Files/Packages/1.0"

// Create a Delta Package from Previous to New Version.
dotnet Sewer56.Update.Tool.dll CreateDeltaPackage --folderpath "Files/NewVersion" --lastversionfolderpath "Files/LastVersion" --version "1.0.1" --lastversion "1.0" --outputpath "Files/Packages/1.0_to_1.0.1"
```

You can access help with e.g. `dotnet Sewer56.Update.Tool.dll CreateCopyPackage --help`.

### Using the API

*Note: You can also automatically create packages as part of the release process by using the `ReleaseBuilder<T>` api (see below).
Only use this API if you wish to create packages without making a release.*

Consider using the `Package<T>` API.  
Example: 

```csharp
// Create Regular Package
await Package<Empty>.CreateAsync(PackageContentFolder, OutputFolder, "1.0");

// Create Delta Package
await Package<Empty>.CreateDeltaAsync(LastVersionFolder, CurrentVersionFolder, OutputFolder, "1.0", "1.0.1");
```

## Creating Releases

### Using the CLI

Create a text file containing a list of all packages in the release (one package per line). 
Each line should contain a relative or full path to the package to be included. 

Example:

- Packages.txt
```
Files/Packages/1.0
Files/Packages/1.0.1
Files/Packages/1.0_to_1.0.1
```

- Command
```bash
// Create a simple release, with existing packages sourced from Packages.txt
// Output the release in `Files.release` and call the package files "Poems".
dotnet Sewer56.Update.Tool.dll --existingpackagespath "Files/Packages.txt" --outputpath "Files/Release" --packagename Poems 
```

### Using the API

You can build a fresh release by using the `ReleaseBuilder<T>` API.
Here is an example:

```csharp
// Arrange
var builder = new ReleaseBuilder<Empty>();
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

For legacy reasons, packages without a manifest are also supported.  
This is to make moving from older update systems easier; however you lose the support for automatic cleanup of leftover files from old versions.