using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib;
using Sewer56.DeltaPatchGenerator.Lib.Model;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Interfaces;

namespace Sewer56.Update.Packaging.Structures;

/// <summary>
/// Contains metadata for a singular package.
/// </summary>
public class PackageMetadata : IJsonSerializable
{
    /// <summary />
    public string GetDefaultFileName() => "Sewer56.Update.Metadata.json";

    /// <summary>
    /// Path to the folder containing this metadata;
    /// </summary>
    [JsonIgnore]
    public string? FolderPath { get; set; }

    /// <summary>
    /// The type of the package shipped.
    /// </summary>
    public PackageType Type { get; set; }

    /// <summary>
    /// The version contained in this package.
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// Contains a list of all hashes in this package.
    /// </summary>
    public FileHashSet? Hashes { get; set; }

    /// <summary>
    /// Regex patterns for files to be ignored during cleanup.
    /// </summary>
    public List<string>? IgnoreRegexes { get; set; }

    /// <summary>
    /// Regex pattern for including files. Overrides <see cref="IgnoreRegexes"/>.
    /// </summary>
    public List<string>? IncludeRegexes { get; set; }

    /// <summary>
    /// Contains delta information about the patch.
    /// Only available if <see cref="Type"/> is <see cref="PackageType.Delta"/>
    /// </summary>
    public DeltaPackageMetadata? DeltaData { get; set; }

    /// <summary>
    /// Internal use only (but required for serialization :/).
    /// Please use the static methods.
    /// </summary>
    public PackageMetadata()
    {
        Version = "";
        Hashes = new FileHashSet();
    }

    /// <summary>
    /// Verifies all files specified in the Hash Set (<see cref="Hashes"/>).
    /// </summary>
    /// <param name="missingFiles">Files that were missing from the folder.</param>
    /// <param name="mismatchFiles">Files with a mismatching hash.</param>
    /// <param name="sourceDirectory">The source directory. If not specified, defaults to <see cref="FolderPath"/></param>
    public bool Verify(out List<string> missingFiles, out List<string> mismatchFiles, string? sourceDirectory = null)
    {
        if (Hashes == null)
        {
            mismatchFiles = new List<string>();
            missingFiles = new List<string>();
            return true;
        }

        sourceDirectory ??= FolderPath;
        return HashSet.Verify(Hashes, sourceDirectory, out missingFiles, out mismatchFiles);
    }

    /// <summary>
    /// Copies all files specified in the Hash Set (<see cref="Hashes"/>).
    /// </summary>
    /// <param name="targetDirectory">The directory set to receive the patch.</param>
    /// <param name="sourceDirectory">The source directory. If not specified, defaults to <see cref="FolderPath"/></param>
    /// <param name="overWrite">Whether the copy operation is allowed to overwrite any existing files.</param>
    public void CopyFiles(string targetDirectory, string? sourceDirectory = null, bool overWrite = true)
    {
        if (Hashes == null)
            throw new NullReferenceException($"Expected {nameof(Hashes)} to not be null but was null.");

        sourceDirectory ??= FolderPath;
        Hashes.HashSetCopyFiles(sourceDirectory!, targetDirectory, overWrite);
    }

    /// <summary>
    /// Patches a target to the new version using this metadata as base.
    /// </summary>
    /// <param name="targetDirectory">The directory set to receive the patch.</param>
    /// <param name="sourceDirectory">
    ///     The source directory containing the patch files.
    ///     If not specified, defaults to <see cref="FolderPath"/>.
    /// </param>
    /// <param name="deltaSourceDirectory">
    ///     The directory containing files to be delta patched.
    ///     If not specified, defaults to <paramref name="targetDirectory"/>.
    /// </param>
    /// <param name="cleanupAfterUpdate">
    ///     If true (default) removes redundant files after updating.
    /// </param>
    public void Apply(string targetDirectory, string? sourceDirectory = null, string? deltaSourceDirectory = null, bool cleanupAfterUpdate = true)
    {
        sourceDirectory ??= FolderPath;
        deltaSourceDirectory ??= targetDirectory;

        switch (Type)
        {
            case PackageType.Copy:
                Apply_Copy(targetDirectory, sourceDirectory!);
                break;
            case PackageType.Legacy:
                Apply_Legacy(targetDirectory, sourceDirectory!);
                break;
            case PackageType.Delta:
                Apply_Delta(targetDirectory, sourceDirectory!, deltaSourceDirectory);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Cleanup.
        if (cleanupAfterUpdate)
            Cleanup(targetDirectory);
    }

    /// <summary>
    /// Cleans up a folder (removes files not in list) based on the file list in the package metadata.
    /// </summary>
    /// <param name="targetDirectory">The directory to be cleaned up.</param>
    public void Cleanup(string targetDirectory)
    {
        if (Hashes != null)
        {
            var compiledIgnoreRegexes  = IgnoreRegexes?.Select(x => new Regex(x));  // Delete if not match 
            var compiledIncludeRegexes = IncludeRegexes?.Select(x => new Regex(x)); // Delete if match.
            HashSet.Cleanup(Hashes, targetDirectory, path => !path.TryMatchAnyRegex(compiledIgnoreRegexes) && path.TryMatchAnyRegex(compiledIncludeRegexes));
        }
    }

    private void Apply_Legacy(string targetDirectory, string sourceDirectory) => IOEx.CopyDirectory(sourceDirectory, targetDirectory);

    private void Apply_Copy(string targetDirectory, string sourceDirectory) => CopyFiles(targetDirectory, sourceDirectory, true);

    private void Apply_Delta(string targetDirectory, string sourceDirectory, string? deltaSourceDirectory)
    {
        if (DeltaData == null)
            throw new NullReferenceException($"Expected {nameof(DeltaData)} to not be null but was null.");

        CopyFiles(targetDirectory, deltaSourceDirectory, false); // Copy non-patched files.
        var oldDirectory = DeltaData.PatchData.Directory;

        try
        {
            DeltaData.PatchData.Directory = sourceDirectory;
            Patch.Apply(DeltaData.PatchData, deltaSourceDirectory, targetDirectory);
        }
        finally
        {
            DeltaData.PatchData.Directory = oldDirectory;
        }
    }

    /// <inheritdoc />
    public void AfterDeserialize(IJsonSerializable thisItem, string? filePath)
    {
        // Dangerous! Reinterpret cast!
        // Do not use code depending on `T`.
        var metadata = Unsafe.As<PackageMetadata<Empty>>(thisItem);
        var directory = Path.GetDirectoryName(filePath);
        metadata.FolderPath = directory;
        metadata.DeltaData?.PatchData.Initialize(directory);
    }
}

/// <summary>
/// Contains metadata for a singular package.
/// </summary>
public class PackageMetadata<T> : PackageMetadata where T : class
{
    /// <summary>
    /// Stores extra data.
    /// Contents are specific to package consumer.
    /// </summary>
    public T? ExtraData { get; set; }

    /// <summary>
    /// Reads the current item from a Json Directory.
    /// </summary>
    /// <param name="directory">The directory to create package from.</param>
    /// <param name="token">Allows for cancelling the task.</param>
    public static async Task<PackageMetadata<T>?> ReadFromDirectoryAsync(string directory, CancellationToken token = default)
    {
        var instance = Singleton<PackageMetadata<T>>.Instance;
        return await instance.ReadFromDirectoryOrNullAsync(directory, null, token);
    }

    /// <summary>
    /// Creates package metadata from a given directory.
    /// </summary>
    /// <param name="directory">The directory to create package from.</param>
    /// <param name="packageType">Type of package used.</param>
    /// <param name="version">The version of the package.</param>
    /// <param name="data">Extra data to add to the package.</param>
    /// <param name="ignoreRegexes">List of regexes; file is ignored if any matches.</param>
    /// <param name="includeRegexes">Regex pattern for including files. Overrides <paramref name="ignoreRegexes"/></param>
    public static PackageMetadata<T> CreateFromDirectory(string directory, string version, PackageType packageType = PackageType.Copy, T? data = null, List<string>? ignoreRegexes = null, List<string>? includeRegexes = null)
    {
        var compiledIgnoreRegexes = ignoreRegexes?.Select(x => new Regex(x));
        var compiledIncludeRegexes = includeRegexes?.Select(x => new Regex(x));
        var hashes = packageType != PackageType.Legacy ? HashSet.Generate(directory, null, path => path.TryMatchAnyRegex(compiledIgnoreRegexes) && !path.TryMatchAnyRegex(compiledIncludeRegexes)) : null;

        return new PackageMetadata<T>()
        {
            FolderPath = directory,
            Version = version,
            Hashes = hashes,
            Type = packageType,
            ExtraData = data,
            IgnoreRegexes = ignoreRegexes,
            IncludeRegexes = includeRegexes
        };
    }
}

/// <summary>
/// Metadata for delta packages.
/// </summary>
public class DeltaPackageMetadata
{
    /// <summary>
    /// Contains patch data.
    /// Only available if <see cref="Type"/> is <see cref="PackageType.Delta"/>
    /// </summary>
    public PatchData PatchData { get; set; } = new PatchData();

    /// <summary>
    /// Previous version of the package.
    /// </summary>
    public string OldVersion { get; set; } = "";
}