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

## Downloading Previous Packages

It is possible to download existing packages by using the update tool; this is useful for creating delta updates in a CI/CD environment.

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

### Note

This article is a stub. You can help by expanding it with a pull request!  
I'm in *docs/ci-cd.md*!.
