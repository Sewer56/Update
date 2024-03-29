﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Packaging;

/// <summary>
/// Builds packages from a given directory.
/// </summary>
public static class Package<T> where T : class
{
    /// <summary>
    /// Determines if a given directory contains package metadata.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <returns>If the package contains metadata.</returns>
    public static bool HasMetadata(string directory) => Singleton<PackageMetadata<T>>.Instance.CanReadFromDirectory(directory, null, out _, out _);

    /// <summary>
    /// Creates package metadata from a given directory.
    /// </summary>
    /// <param name="directory">The directory to create package from.</param>
    /// <param name="token">Allows for cancelling the task.</param>
    public static async Task<PackageMetadata<T>?> ReadMetadataFromDirectoryAsync(string directory, CancellationToken token = default) => await PackageMetadata<T>.ReadFromDirectoryAsync(directory, token);

    /// <summary>
    /// Creates package metadata from a given directory.
    /// </summary>
    /// <param name="directory">The directory to create package from.</param>
    /// <param name="token">Allows for cancelling the task.</param>
    public static async Task<PackageMetadata<T>> ReadOrCreateLegacyMetadataFromDirectoryAsync(string directory, CancellationToken token = default)
    {
        var result = (await ReadMetadataFromDirectoryAsync(directory, token))!;
        return result ?? PackageMetadata<T>.CreateFromDirectory(directory, "1.0", PackageType.Legacy);
    }

    /// <summary>
    /// Creates a package from a given folder.
    /// </summary>
    /// <param name="folderPath">Path to the folder from which to create package from.</param>
    /// <param name="outputFolder">Folder to where to save the package to.</param>
    /// <param name="version">The version of the package.</param>
    /// <param name="data">Extra data to add to the package.</param>
    /// <param name="ignoreRegexes">List of regexes; file is ignored if any matches.</param>
    /// <param name="includeRegexes">Regex pattern for including files. Overrides <paramref name="ignoreRegexes"/></param>
    public static async Task<PackageMetadata<T>> CreateAsync(string folderPath, string outputFolder, string version, T? data = null, List<string>? ignoreRegexes = null, List<string>? includeRegexes = null)
    {
        Directory.CreateDirectory(outputFolder);
        var metadata = PackageMetadata<T>.CreateFromDirectory(folderPath, version, PackageType.Copy, data, ignoreRegexes, includeRegexes);
        await metadata.ToDirectoryAsync(outputFolder);
        metadata.CopyFiles(outputFolder, folderPath);
        metadata.FolderPath = outputFolder;
        return metadata;
    }

    /// <summary>
    /// Creates a delta package from a given folder.
    /// </summary>
    /// <param name="oldFolderPath">Folder containing the previous version.</param>
    /// <param name="newFolderPath">Folder containing the new version.</param>
    /// <param name="outputFolder">Folder to where to save the package to.</param>
    /// <param name="version">The version of the package.</param>
    /// <param name="oldVersion">Previous version of the package.</param>
    /// <param name="data">Extra data to add to the package.</param>
    /// <param name="ignoreRegexes">List of regexes; file is ignored if any matches.</param>
    /// <param name="progress">Used for reporting current progress.</param>
    /// <param name="includeRegexes">Regex pattern for including files. Overrides <paramref name="ignoreRegexes"/></param>
    public static async Task<PackageMetadata<T>> CreateDeltaAsync(string oldFolderPath, string newFolderPath, string outputFolder, string oldVersion, string version, T? data = null, List<string>? ignoreRegexes = null, Events.ProgressCallback? progress = null, List<string>? includeRegexes = null)
    {
        Directory.CreateDirectory(outputFolder);
        var metadata       = PackageMetadata<T>.CreateFromDirectory(newFolderPath, version, PackageType.Delta, data, ignoreRegexes, includeRegexes);
        metadata.DeltaData = new DeltaPackageMetadata()
        {
            PatchData  = Patch.Generate(oldFolderPath, newFolderPath, outputFolder, progress, false),
            OldVersion = oldVersion
        };

        await metadata.ToDirectoryAsync(outputFolder);
        metadata.FolderPath = outputFolder;
        return metadata;
    }
}