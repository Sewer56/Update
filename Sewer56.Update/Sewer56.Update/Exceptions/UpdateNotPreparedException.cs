using System;
using System.Collections.Generic;
using System.Text;
using NuGet.Versioning;

namespace Sewer56.Update.Exceptions;

/// <summary>
/// Thrown when starting the update to install an update that was not prepared.
/// </summary>
public class UpdateNotPreparedException : Exception
{
    /// <summary>
    /// Package version.
    /// </summary>
    public NuGetVersion Version { get; }

    /// <summary>
    /// Initializes an instance of <see cref="UpdateNotPreparedException"/>.
    /// </summary>
    public UpdateNotPreparedException(NuGetVersion version)
        : base($"Update to version '{version}' is not prepared. Please prepare an update before applying it.")
    {
        Version = version;
    }
}