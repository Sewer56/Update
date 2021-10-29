using System;
using System.Collections.Generic;
using System.Text;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Exceptions;

namespace Sewer56.Update.Packaging.Structures.ReleaseBuilder;

/// <summary />
public class BaseBuilderItem<T> where T : class
{
    /// <summary>
    /// Additional information to include in the package being built.
    /// This information will be available in the <see cref="PackageMetadata{T}"/>.
    /// </summary>
    public T? Data = null;

    /// <summary>
    /// List of regexes; a file is ignored (will not be included in update) if any matches.
    /// </summary>
    public List<string>? IgnoreRegexes = null;

    /// <summary>
    /// Version of the current package.
    /// </summary>
    public string Version = "1.0";

    /// <summary>
    /// Validates the current item.
    /// </summary>
    public virtual void Validate()
    {
        ThrowHelpers.ThrowIfNullOrEmpty(Version, () => new BuilderValidationFailedException($"Builder Item Should specify a version ({nameof(BaseBuilderItem<T>)}.{nameof(Version)}) for the package, but does not."));
    }
}