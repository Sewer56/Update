using System;
using System.Collections.Generic;
using System.Text;

namespace Sewer56.Update.Resolvers.GitHub;

/// <summary>
/// Exception for GitHub Resolver.
/// </summary>
public class GitHubResolverException : Exception
{
    /// <inheritdoc />
    public GitHubResolverException()
    {
    }

    /// <inheritdoc />
    public GitHubResolverException(string? message) : base(message)
    {
    }
}