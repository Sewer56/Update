using System;
using System.Collections.Generic;
using System.Text;

namespace Sewer56.Update.Resolvers.NuGet;

/// <summary>
/// Settings for creating and extracting NuGet Package Archive.
/// </summary>
public class NuGetPackageExtractorSettings
{
    /// <summary>
    /// The target framework name to use for storing content files.
    /// Files are located in "content/{TargetFrameworkName}"
    /// </summary>
    public string TargetFrameworkName { get; set; } = "Sewer56.Update";

    /// <summary>
    /// Gets the folder containing the content inside the .nupkg.
    /// </summary>
    public string NupkgContentFolder => $"contentFiles/any/{TargetFrameworkName}";
}