using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Sewer56.DeltaPatchGenerator.Lib;
using Sewer56.DeltaPatchGenerator.Lib.Model;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.IO;

namespace Sewer56.Update.Packaging;

/// <summary/>
public static class Utilities
{
    /// <summary>
    /// Copies all files specified in a given hash set from the source to the destination directory.
    /// </summary>
    /// <param name="hashSet">The set to copy files using.</param>
    /// <param name="sourceDirectory">Directory to copy from.</param>
    /// <param name="targetDirectory">Directory to copy to.</param>
    /// <param name="overWrite">Whether the copy operation is allowed to overwrite any existing files.</param>
    public static void HashSetCopyFiles(this FileHashSet hashSet, string sourceDirectory, string targetDirectory, bool overWrite = true)
    {
        var createdDirectorySet = new CreatedDirectorySet();
        foreach (var file in hashSet.Files)
        {
            var oldPath = Paths.AppendRelativePath(file.RelativePath, sourceDirectory);
            var newPath = Paths.AppendRelativePath(file.RelativePath, targetDirectory);
            
            // Do not overwrite if not necessary.
            if (!overWrite && File.Exists(newPath))
                continue;

            createdDirectorySet.CreateDirectoryIfNeeded(Path.GetDirectoryName(newPath)!);

            // Wait until we can copy.
            while (File.Exists(newPath) && !IOEx.CheckFileAccess(newPath, FileMode.Open, FileAccess.Write))
                Thread.Sleep(100);
            
            File.Copy(oldPath, newPath, true);
        }
    }

    /// <summary>
    /// Makes a unique, empty folder inside a specified folder.
    /// </summary>
    /// <param name="folder">The path of the folder to make folder inside.</param>
    public static string MakeUniqueFolder(string folder)
    {
        string fullPath;

        do
        {
            fullPath = Path.Combine(folder, Path.GetRandomFileName());
        } 
        while (Directory.Exists(fullPath));

        Directory.CreateDirectory(fullPath);
        return fullPath;
    }
}