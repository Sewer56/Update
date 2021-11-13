using System.Collections.Generic;
using CommandLine;
using Sewer56.Update.Resolvers.NuGet;

namespace Sewer56.Update.Tool.Options.Groups;

internal interface INuGetOptions
{
    [Option(SetName = "NuGet", HelpText = $"[{nameof(Archiver.NuGet)} Specific] The id of the NuGet package.")]
    public string NuGetId { get; set; }

    [Option(SetName = "NuGet", HelpText = $"[{nameof(Archiver.NuGet)} Specific] The description of the NuGet package.")]
    public string NuGetDescription { get; set; }

    [Option(SetName = "NuGet", HelpText = $"[{nameof(Archiver.NuGet)} Specific] The authors of the NuGet package.")]
    public IEnumerable<string> NuGetAuthors { get; set; }
}

public static class NuGetOptionsExtensions
{
    internal static NuGetPackageArchiverSettings GetArchiver(this INuGetOptions releaseOptions)
    {
        return new NuGetPackageArchiverSettings()
        {
            Id = releaseOptions.NuGetId,
            Authors = releaseOptions.NuGetAuthors,
            Description = releaseOptions.NuGetDescription
        };
    }
}