﻿using CommandLine;

namespace Sewer56.Update.Tool.Options.Groups;

internal interface ICreateReleaseOptions : ISharpCompressOptions, INuGetOptions, ISevenZipSharpOptions
{
    public const int DefaultInt = -1;

    [Option(Required = false, HelpText = "Path to a text file with file paths to existing packages (1 path per line). Paths should point to uncompressed folders.")]
    string ExistingPackagesPath { get; set; }

    [Option(Required = true, HelpText = "The folder where to save the new release. If there is already a release in that folder, it will be updated.")]
    string OutputPath { get; set; }

    [Option(Required = false, HelpText = "The name for the packages as downloaded by the user.", Default = "Package")]
    string PackageName { get; set; }

    [Option(Required = false, HelpText = "The archiver to use for archiving the content.", Default = Archiver.Zip)]
    Archiver Archiver { get; set; }

    [Option(Required = false, HelpText = "Maximum number of parallel compression tasks.", Default = DefaultInt)]
    int MaxParallelism { get; set; }

    [Option(Required = false, HelpText = "If updating existing release, auto generates delta package from last version to current version.", Default = false)]
    bool AutoGenerateDelta { get; set; }

    [Option(Required = false, HelpText = "Does not append version to non-delta update packages. Only valid if this release has only 1 non-delta package.", Default = false)]
    bool DontAppendVersionToPackages { get; set; }
}