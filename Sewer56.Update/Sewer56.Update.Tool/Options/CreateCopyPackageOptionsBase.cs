using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Options;

internal class CreateCopyPackageOptionsBase : ICreateCopyPackageOptionsBase
{
    public string FolderPath { get; set; }

    public string Version { get; set; }

    public string OutputPath { get; internal set; }

    public string IgnoreRegexesPath { get; internal set; }

    public string IncludeRegexesPath { get; internal set; }
}