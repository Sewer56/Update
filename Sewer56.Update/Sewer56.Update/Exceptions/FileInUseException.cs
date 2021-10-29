using System;

namespace Sewer56.Update.Exceptions;

/// <summary>
/// Thrown when starting the update to install an update that was not prepared.
/// </summary>
public class FileInUseException : Exception
{
    /// <summary>
    /// Package version.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Initializes an instance of <see cref="UpdateNotPreparedException"/>.
    /// </summary>
    public FileInUseException(string filePath) : base($"Unable to perform update because file '{filePath}' is in use.")
    {
        FilePath = filePath;
    }
}