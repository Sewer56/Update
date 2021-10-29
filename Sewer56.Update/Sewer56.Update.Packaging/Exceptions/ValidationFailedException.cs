using System;
using System.Collections.Generic;
using System.Text;

namespace Sewer56.Update.Packaging.Exceptions;

/// <summary />
public class ValidationFailedException : System.Exception
{
    /// <summary />
    public ValidationFailedException(string? message) : base($"Failed to validate item: {message}") { }
}