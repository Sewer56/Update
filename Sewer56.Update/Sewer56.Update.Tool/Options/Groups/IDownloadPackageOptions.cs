using CommandLine;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Tool.Options.Groups;

internal interface IDownloadPackageOptions
{
    [Option(Required = true, HelpText = $"Where the downloaded package file should be saved. This is a folder if {nameof(Extract)} is true, else the file name.")]
    string OutputPath { get; set; }

    [Option(Required = false, HelpText = "Extracts the package using the default extractor. 7z for Windows, else SharpCompress.", Default = false)]
    bool Extract { get; set; }

    [Option(Required = false, HelpText = "A value of 0 downloads current release. A value of 1 downloads previous release.", Default = 0)]
    int ReleaseIndex { get; set; }
}