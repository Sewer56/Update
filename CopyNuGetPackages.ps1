$publishDir = "Publish"
$packagePattern  = "*.nupkg"
$filePattern  = "Sewer56.*.nupkg"

# Set CD
Push-Location (Split-Path $MyInvocation.MyCommand.Path)

# Run actual publish script.
New-Item -ItemType Directory -Force -Path $publishDir
Get-ChildItem -Path "." -Recurse | Where-Object { $_.Name -match $filePattern } | Move-Item -Destination $publishDir

# Restore CD
Pop-Location