using CommandLine;

namespace Sewer56.Update.Tool.Options;

[Verb("CreateDeltaPackage", HelpText = "Creates a new Delta package.")]
internal class CreateDeltaPackageOptions : CreateCopyPackageOptionsBase
{
    [Option(Required = true, HelpText = "The path containing the contents of the last package.")]
    public string LastVersionFolderPath { get; set; }

    [Option(Required = true, HelpText = "The version of the last package.")]
    public string LastVersion { get; set; }
}