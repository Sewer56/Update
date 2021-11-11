## CI/CD Integration

The easiest way to integrate `Update` with CI/CD is to download the latest version when you are building a new version. 

```bash
# Create Copy.csv before running this from your own script.
# Download Latest Release
curl -L -O https://github.com/Sewer56/Update/releases/latest/download/Sewer56.Update.Tool.zip

# Extract Tools
unzip Sewer56.Update.Tool.zip

# To run the tool use:
dotnet Sewer56.Update.Tool.dll /* Arguments */
```

Refer to [Release Creation](./release-creation.md) on how to create a release using the CLI.

### Note

This article is a stub. You can help by expanding it with a pull request!  
I'm in *docs/ci-cd.md*!.
