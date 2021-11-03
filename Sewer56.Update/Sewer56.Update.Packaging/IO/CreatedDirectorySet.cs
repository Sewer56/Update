using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sewer56.Update.Packaging.IO;

/// <summary>
/// Represents a set of created directories; used to avoid unnecessary IO calls to create directories.
/// </summary>
public class CreatedDirectorySet
{
    /// <summary>
    /// Set containing all created directories.
    /// </summary>
    public HashSet<string> CreatedSet { get; } = new HashSet<string>();

    /// <summary>
    /// Creates a directory if it has not yet been created.
    /// </summary>
    /// <param name="directory">The directory to create.</param>
    public void CreateDirectoryIfNeeded(string directory)
    {
        if (CreatedSet.Add(directory))
            Directory.CreateDirectory(directory);
    }
}