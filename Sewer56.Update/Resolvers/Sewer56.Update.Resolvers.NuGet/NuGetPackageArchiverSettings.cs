using System;
using System.Collections.Generic;
using System.Text;
using NuGet.Packaging;
using NuGet.Versioning;

namespace Sewer56.Update.Resolvers.NuGet;

/// <inheritdoc />
public class NuGetPackageArchiverSettings : NuGetPackageExtractorSettings
{
    /// <summary>
    /// Unique ID for this NuGet package.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Description of this package.
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// Authors of the package.
    /// </summary>
    public IEnumerable<string> Authors { get; set; } = new List<string>();

    /// <summary>
    /// Allows you to perform additional changes to the build package before it is saved.
    /// </summary>
    public Action<PackageBuilder>? OnPreBuild { get; set; }
}