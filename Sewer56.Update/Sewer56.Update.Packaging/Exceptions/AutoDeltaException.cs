using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Sewer56.Update.Packaging.Exceptions;

/// <inheritdoc />
public class AutoDeltaException : Exception
{
    /// <inheritdoc />
    public AutoDeltaException(string? message) : base($"Cannot create automatic delta package: {message}") { }
}