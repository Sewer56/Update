using Sewer56.Update.Tool.Options.Groups;

namespace Sewer56.Update.Tool.Options;

internal class CreateCopyPackageOptionsBase : ICreateCopyPackageOptionsBase
{
    public string FolderPath { get; set; }

    public string Version { get; set; }

    public string OutputPath { get; set; }

    public string IgnoreRegexesPath { get; set; }

    public string IncludeRegexesPath { get; set; }
}