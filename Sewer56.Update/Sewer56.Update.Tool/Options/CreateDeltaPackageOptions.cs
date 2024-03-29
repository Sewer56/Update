﻿using CommandLine;
using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Options;

[Verb("CreateDeltaPackage", HelpText = "Creates a new Delta package.")]
internal class CreateDeltaPackageOptions : CreateCopyPackageOptionsBase, ICreateDeltaPackageOptions, IProgressBarOptions
{
    public string LastVersionFolderPath { get; set; }

    public string LastVersion { get; set; }

    public bool NoProgressBar { get; set; }
}