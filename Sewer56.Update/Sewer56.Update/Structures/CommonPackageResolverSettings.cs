using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Structures;

/// <summary>
/// Common settings to be used with package resolvers.
/// </summary>
public class CommonPackageResolverSettings
{
    /// <summary>
    /// The name of the file containing release metadata.
    /// Only override this if hosting multiple items inside one release; e.g. multiple programs in one GitHub Release.
    /// </summary>
    public string MetadataFileName { get; set; } = Singleton<ReleaseMetadata>.Instance.GetDefaultFileName();

    /// <summary>
    /// Set to true to allow prereleases.
    /// </summary>
    public bool AllowPrereleases { get; set; } = false;

    /// <summary>
    /// Gets the list of possible file names for compressed metadata.
    /// </summary>
    public List<string> GetCompressedFileNames()
    {
        return new List<string>()
        {
            MetadataFileName + ".br" // Brotli
        };
    }
}