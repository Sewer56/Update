using CommandLine;

namespace Sewer56.Update.Tool.Options.Groups;

internal interface ICreateCopyPackageOptionsBase : ICurrentPackageDetails
{
    [Option(Required = true, HelpText = "The folder where to save the package.")]
    string OutputPath { get; set; }
}

internal interface ICurrentPackageDetails
{
    [Option(Required = true, HelpText = "The path where the contents to be placed inside the package (files for current version) are stored.")]
    string FolderPath { get; set; }

    [Option(Required = true, HelpText = "The version of the package.")]
    string Version { get; set; }

    [Option(Required = false, HelpText = "Path to a text file containing a list of regular expressions (1 expression per line) of files to be ignored in the newly created (non-existing) packages.")]
    string IgnoreRegexesPath { get; set; }

    [Option(Required = false, HelpText = "Path to a text file containing a list of regular expressions (1 expression per line) of files to be un-ignored. Overrides IgnoreRegexesPath.")]
    string IncludeRegexesPath { get; set; }
}