using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Sewer56.Update.Tool;

[Verb("create", HelpText = "Creates a new release.")]
internal class CreateOptions
{
    [Option(Required = false, HelpText = "Path to a CSV file with all packages to be copied. Entry should have format: \"PathToFolder,Version\"")]
    public string CopyPackagesPath { get; internal set; }

    [Option(Required = false, HelpText = "Path to a CSV file with all packages to be delta patched. Entry should have format: \"PathToCurrentVersionFolder,CurrentVersion,PathToPreviousVersionFolder,PreviousVersion\"")]
    public string DeltaPackagesPath { get; internal set; }

    [Option(Required = true, HelpText = "The folder where to save the new release.")]
    public string OutputPath { get; internal set; }

    [Option(Required = false, HelpText = "The name for the packages as downloaded by the user.", Default = "Package")]
    public string PackageName { get; internal set; }

    [Option(Required = false, HelpText = "Path to a text file containing a list of regular expressions (1 expression per line) of files to be ignored in the packages.")]
    public string IgnoreRegexesPath { get; internal set; }
}


public class CopyPackage
{
    public string FolderPath { get; set; }
    public string Version { get; set; }
}

public class DeltaPackage
{
    public string CurrentVersionFolder { get; set; }
    public string CurrentVersion { get; set; }

    public string LastVersionFolder { get; set; }
    public string LastVersion { get; set; }
}
