## CI/CD Integration

The easiest way to integrate `Update` with CI/CD is to download the latest version when you are building a new version.  

Refer to [Release Creation](./release-creation.md) on how to create a release using the CLI.  

### Bash:

```bash
# Download Latest Release
curl -L -O https://github.com/Sewer56/Update/releases/latest/download/Sewer56.Update.Tool.zip

# Extract Tools
unzip Sewer56.Update.Tool.zip -d "Update-Tools"

# To run the tool use:
dotnet ./Update-Tools/Sewer56.Update.Tool.dll /* Arguments */
```

### Powershell:

```bash
# Download Latest Release
Invoke-WebRequest -Uri "https://github.com/Sewer56/Update/releases/latest/download/Sewer56.Update.Tool.zip" -OutFile "Sewer56.Update.Tool.zip"

# Extract Tools
Expand-Archive -LiteralPath './Sewer56.Update.Tool.zip' -DestinationPath "Update-Tools"

# To run the tool use:
dotnet ./Update-Tools/Sewer56.Update.Tool.dll /* Arguments */
```

## Useful Use Cases

!!! warning

    Certain tool options return data to standard output (stdout).  
    Please specify the `--noprogressbar` parameter if you wish to use those.  

## Downloading Previous Packages

It is possible to download existing packages by using the update tool.  
This may be useful in some more advanced use cases.  

Here are some examples:  

**GitHub**  
```
dotnet Sewer56.Update.Tool.dll DownloadPackage --outputpath "Mod.pkg" --source GitHub --githubusername Sewer56 --githubrepositoryname Update.Test.Repo
```

**NuGet**  
```
dotnet Sewer56.Update.Tool.dll DownloadPackage --outputpath "Mod.pkg" --source NuGet --nugetfeedurl http://packages.sewer56.moe:5000/v3/index.json --nugetpackageid reloaded.sharedlib.hooks
```

**GameBanana**  
```
dotnet Sewer56.Update.Tool.dll DownloadPackage --extract --outputpath "Mod.pkg" --source GameBanana --gamebananaitemid 333681
```

The version of the downloaded package is returned to standard output. So you could write the version of the downloaded package to a file using `dotnet Sewer56.Update.Tool.dll DownloadPackage --noprogressbar ... > version.txt` to save it to a file called `version.txt`.

## Auto Creating Delta Packages

You can automatically download older versions of a package and create delta packages by using the `AutoCreateDelta` option.

```pwsh
dotnet Sewer56.Update.Tool.dll AutoCreateDelta `
--outputpath "DeltaPackages" `           # Where to save generated packages.
--folderpath "current-version-package" ` # Where package for current version (made with `CreateCopyPackage`) is stored.
--version "1.0.0" `                      # Current version's version number.
--source "GitHub" `                      # Where to get previous version from.
--githubusername "Reloaded-Project" `    
--githubrepositoryname "Reloaded-II" `
--githublegacyfallbackpattern "Release.zip" `
--numreleases 5 ` # Number of releases to create delta packages for.
--noprogressbar ` # Required for safe piping to packages.txt
>> packages.txt
```

This will generate delta updates and append their location to `packages.txt`, which can then be passed to `--existingpackagespath` parameter of `CreateRelease` command.

## Note

This article is a stub. You can help by expanding it with a pull request!  
I'm in *docs/ci-cd.md*!.
