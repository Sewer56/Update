using CommandLine;

namespace Sewer56.Update.Tool.Options.Groups;

public interface IProgressBarOptions
{
    [Option(Required = false, HelpText = "The folder where to save the package.", Default = false)]
    bool NoProgressBar { get; set; }
}