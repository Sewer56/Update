using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Sewer56.Update.Tool;

[Verb("CreateDeltaPackage", HelpText = "Creates a new Delta package.")]
internal class CreateDeltaPackageOptions : CreateCopyPackageOptionsBase
{
    [Option(Required = true, HelpText = "The path containing the contents of the last package.")]
    public string LastVersionFolderPath { get; set; }

    [Option(Required = true, HelpText = "The version of the last package.")]
    public string LastVersion { get; set; }
}

[Verb("CreateCopyPackage", HelpText = "Creates a new Copy package.")]
internal class CreateCopyPackageOptions : CreateCopyPackageOptionsBase { }

internal class CreateCopyPackageOptionsBase
{
    [Option(Required = true, HelpText = "The path where the contents to be placed inside the package are stored.")]
    public string FolderPath { get; set; }

    [Option(Required = true, HelpText = "The version of the package.")]
    public string Version { get; set; }

    [Option(Required = true, HelpText = "The folder where to save the package.")]
    public string OutputPath { get; internal set; }

    [Option(Required = false, HelpText = "Path to a text file containing a list of regular expressions (1 expression per line) of files to be ignored in the newly created (non-existing) packages.")]
    public string IgnoreRegexesPath { get; internal set; }
}

[Verb("CreateRelease", HelpText = "Creates a new release.")]
internal class CreateReleaseOptions
{
    [Option(Required = false, HelpText = "Path to a text file with file paths to existing packages (1 path per line). Paths should point to uncompressed folders.")]
    public string ExistingPackagesPath { get; internal set; }

    [Option(Required = true, HelpText = "The folder where to save the new release.")]
    public string OutputPath { get; internal set; }

    [Option(Required = false, HelpText = "The name for the packages as downloaded by the user.", Default = "Package")]
    public string PackageName { get; internal set; }
}