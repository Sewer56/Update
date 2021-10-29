namespace Sewer56.Update.Structures;

/// <summary />
public class OutOfProcessOptions
{
    /// <summary>
    /// Whether the application should be restarted.
    /// </summary>
    public bool Restart = true;

    /// <summary>
    /// The commandline arguments to pass to the application being restarted.
    /// </summary>
    public string RestartArguments = "";
}