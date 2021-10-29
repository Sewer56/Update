namespace Sewer56.Update.Packaging.Exceptions;

/// <summary />
public class BuilderValidationFailedException : System.Exception
{
    /// <summary />
    public BuilderValidationFailedException(string? message) : base($"Failed to validate builder item: {message}") { }
}