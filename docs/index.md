## About Update

`Update` is a lightweight-ish updating framework for .NET applications. 

It is designed with the purpose of updating arbitrary things, including but not limited to:  
- Current Application  
- Plugins  
- Modules  

The goal of this library is to be extensible; allowing users to easily add support for their own components such as download sources and compression formats without requiring changes to the library code.

Update is heavily inspired by [Onova](https://github.com/Tyrrrz/Onova) by Alexey Golub and has a somewhat similar API. `Update` in particular adds additional features such as delta compression at the expense of a slightly more complex configuration process.

## When to use Update

- You ship very big updates and require delta compression support between versions.
- You want to clean up your application folder after updates.
- You want to update things other than just the application you are running.
- You need to support Semantic Versioning (and thus Prereleases).

## When to not use Update

Consider using the original [Onova](https://github.com/Tyrrrz/Onova) (or another library) if you have any of the following requirements:

- If you wish to use this library with .NET Framework (VCDiff needs backported).
- You need a simpler CI/CD deployment & integration experience.
- You can only upload 1 file to a given website.

## How the Library Works (Summary)

### Releases
You create releases using either the pre-included tool (`Sewer56.Update.Tool`) or from your own program via the API.

Releases are composed of archives for every version included in the release and a manifest, storing metadata for these archives (e.g. Versions).

### Download Routine

When you check for updates, the release manifest is downloaded from your specified source and the list of available versions is returned to you. When you download a specific version, the library gets the update file name from the manifest and downloads it from your specified source.

### Post Extraction Routine
- If simple update: Files are copied and overwritten.
- If delta update: VCDiff based delta patching happens.

After the update is complete, a `package manifest` file is used (if available) to verify the new files are correct and remove any extra redundant files. 

### In Process & Out of Process

If the data to be updated is something other than the current application, everything is done inside your current application.

If the data to be updated is the current application itself, the library runs an instance of the application, which performs the steps outlined in `Post Extraction Routine`.

## Cross Platform Support

`Update` should in theory function on any platform that supports `CoreCLR` (.NET Core Runtime) providing the following criteria are met:

- Either of the following: 
    - The .NET Core runtime is available in `PATH` (i.e. you can execute `dotnet` in a terminal).
    - The application is self contained.  

- The folder containing the application is writable (not read only). 

While I don't actively test non-Windows targets, the CI/CD builds and testing are actively ran against Ubuntu (Latest); so hopefully the library should work on other platforms.

## Etymology

Update is a pun on the name [Onova](https://github.com/Tyrrrz/Onova), which is the Ukrainian word for "update" (noun).