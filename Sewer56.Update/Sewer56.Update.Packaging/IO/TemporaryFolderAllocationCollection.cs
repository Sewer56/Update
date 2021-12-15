using System;
using System.Collections.Generic;
using System.Text;
using Sewer56.DeltaPatchGenerator.Lib.Utility;

namespace Sewer56.Update.Packaging.IO;

/// <summary>
/// Collection of folders for temporary allocation, all of which are disposed on exit.
/// </summary>
public class TemporaryFolderAllocationCollection : IDisposable
{
    /// <summary>
    /// Folder allocations to be made.
    /// </summary>
    public List<TemporaryFolderAllocation> Allocations { get; private set; } = new List<TemporaryFolderAllocation>();

    /// <summary/>
    ~TemporaryFolderAllocationCollection()
    {
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var allocation in Allocations)
            allocation?.Dispose();
    }
}