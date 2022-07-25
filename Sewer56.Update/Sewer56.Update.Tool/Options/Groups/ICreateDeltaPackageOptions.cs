using CommandLine;

namespace Sewer56.Update.Tool.Options.Groups;

internal interface ICreateDeltaPackageOptions : ICreateCopyPackageOptionsBase
{
    [Option(Required = true, HelpText = "The path containing the contents of the last package.")]
    string LastVersionFolderPath { get; set; }

    [Option(Required = true, HelpText = "The version of the last package.")]
    string LastVersion { get; set; }
}