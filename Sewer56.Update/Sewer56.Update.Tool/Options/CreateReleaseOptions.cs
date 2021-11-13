using System.Collections.Generic;
using CommandLine;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Options;

[Verb("CreateRelease", HelpText = "Creates a new release.")]
internal class CreateReleaseOptions : ISharpCompressOptions, INuGetOptions
{
    [Option(Required = false, HelpText = "Path to a text file with file paths to existing packages (1 path per line). Paths should point to uncompressed folders.")]
    public string ExistingPackagesPath { get; internal set; }

    [Option(Required = true, HelpText = "The folder where to save the new release.")]
    public string OutputPath { get; internal set; }

    [Option(Required = false, HelpText = "The name for the packages as downloaded by the user.", Default = "Package")]
    public string PackageName { get; internal set; }

    [Option(Required = false, HelpText = "The archiver to use for archiving the content.", Default = Archiver.Zip)]
    public Archiver Archiver { get; internal set; }

    /* SharpCompress Specific */
    public SharpCompressFormat SharpCompressFormat { get; set; }

    /* NuGet Specific */
    public string NuGetId { get; set; }
    public string NuGetDescription { get; set; }
    public IEnumerable<string> NuGetAuthors { get; set; }
}

public enum Archiver
{
    Zip,
    NuGet,
    SharpCompress,
}