# About Update

`Update` is a lightweight-ish updating framework for desktop applications. It's designed with the purpose of updating both the current application running as well as other potential components of the application, such as plugins. 

The library aims to require little configuration, attempts to be friendly to CI/CD where possible and should have no effect on the life cycle of the application.

Update is a fork of [Onova](https://github.com/Tyrrrz/Onova) by Alexey Golub and has a somewhat similar API, which makes switching between the two libraries more simple. `Update` in particular adds additional features at the expense of additional dependencies and slightly more complex configuration process.

# When to use Update

- You ship big updates and require delta support to keep downloads small.
- You want to clean up your application folder after updates.
- You want to update things other than just the application you are running.
- You need to support Semantic Versioning (and thus Prereleases).

# When to not use Update

Consider using the original [Onova](https://github.com/Tyrrrz/Onova) if you have any of the following requirements:

- If you wish to use this application with .NET Framework (VCDiff needs backported).
- You wish to have a smaller binary size (this library adds ~164K worth of 3rd party depenencies).
- You can only upload 1 file to a given page.

# How the Library Works (Summary)

Updates and/or delta patches for your application are downloaded directly inside your process and then extracted.

## Post Extraction Routine
- If simple update: Files are copied and overwritten.
- If delta update: VCDiff based delta patching happens.

After the update is complete, a manifest file is used (if available) to verify the new files are correct and remove any extra redundant files. 

## In Process & Out of Process

If the data to be updated is something other than the current application, everything is done inside your current application.

If the data to be updated is the current application itself, the library runs an instance of the application, which performs the steps outlined in `Post Extraction Routine`.

## Package Creation

For creating fully featured update packages, including a manifest file, a separate program called `Update.Tools` is provided. This tool is cross-platform and should be usable from any environment if .NET Core itself is available.

You can find the tool in the releases section of this repository.
Example usage:

```csharp
// Create a simple package using a provided folder.
dotnet Update.Tools.dll create "PathToNewVersion"

// Create a delta update from previous to new version.
dotnet Update.Tools.dll createdelta "PathToOldVersion" "PathToNewVersion" "DeltaOutputFolder"
```

For legacy reasons, it is possible to work without this application, however you lose support for cleanup and delta updates (Reason: No manifest file). 

### CI/CD Integration

The easiest way to integrate `Update` with CI/CD is to download the latest version when you are building a new version. 

```bash
# Download Latest Release
curl -L -O https://github.com/Sewer56/Update/releases/latest/download/Update.Tools.zip

# Extract Tools
unzip Update.Tools.zip

# To run the tool use:
dotnet Update.Tools.dll
```

# Usage

## Install Command Line Handler
Note: *This is only required when performing updates on the application you are currently running `Update` from. i.e. Out of Process updates.*

In your application startup function, you install a handler to `Update`.
This handler is used for copying files over to the destination directory.

```csharp
// Handles command line arguments passed to app.
Update.HandleCommandLineArgs();
```

## Library Usage

WIP.
API is mostly adapted from Onova.

# Extensibility

uwu

# Requires Testing

## Cross Platform Support

`Update` should in theory function on any platform that supports `CoreCLR` (.NET Core Runtime) providing the following criteria are met:

- Either of the following: 
    - The .NET Core runtime is available in `PATH` (i.e. you can execute `dotnet` in a terminal).
    - The application is self contained.  

- The folder containing the application is writable (not read only). 

This however is not yet actively tested.

# Etymology

Update is a pun on the name [Onova](https://github.com/Tyrrrz/Onova), which is the Ukrainian word for "update" (noun).