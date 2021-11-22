using CommandLine;

namespace Sewer56.Update.Tool.Options;

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

    [Option(Required = false, HelpText = "Path to a text file containing a list of regular expressions (1 expression per line) of files to be un-ignored. Overrides IgnoreRegexesPath.")]
    public string IncludeRegexesPath { get; internal set; }
}