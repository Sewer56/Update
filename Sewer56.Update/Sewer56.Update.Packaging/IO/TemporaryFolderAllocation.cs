using System;
using System.IO;
using Sewer56.DeltaPatchGenerator.Lib.Utility;

namespace Sewer56.Update.Packaging.IO;

/// <summary>
/// Creates a temporary folder that is disposed with the class.
/// </summary>
public class TemporaryFolderAllocation : IDisposable
{
    /// <summary>
    /// Path of the temporary folder.
    /// </summary>
    public string FolderPath { get; set; }

    /// <summary/>
    public TemporaryFolderAllocation()
    {
        FolderPath = Utilities.MakeUniqueFolder(Paths.TempFolder);
    }

    /// <inheritdoc />
    ~TemporaryFolderAllocation() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        IOEx.TryDeleteDirectory(FolderPath);
        GC.SuppressFinalize(this);
    }
}