using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sewer56.DeltaPatchGenerator.Lib;
using Sewer56.DeltaPatchGenerator.Lib.Model;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Exceptions;
using Sewer56.Update.Packaging.Interfaces;

namespace Sewer56.Update.Packaging.Structures;

/// <summary>
/// Encapsulates all currently download-able release information.
/// </summary>
public class ReleaseMetadata : IJsonSerializable
{
    /// <summary />
    public string GetDefaultFileName() => "Sewer56.Update.ReleaseMetadata.json";

    /// <summary>
    /// Provides a list of all releases available in this metadata.
    /// </summary>
    public List<ReleaseItem> Releases { get; set; } = new List<ReleaseItem>();

    /// <summary>
    /// Stores extra data.
    /// Contents are specific to package consumer.
    /// Consider using <see cref="GetExtraData{T}"/>.
    /// </summary>
    public object? ExtraData { get; set; }

    /// <summary>
    /// Returns the most appropriate release for a given version. 
    /// </summary>
    /// <param name="version">The release version to get.</param>
    /// <param name="verificationInfo">Information required to verify whether some package types, e.g. Delta Packages can be applied.</param>
    public ReleaseItem? GetRelease(string version, ReleaseMetadataVerificationInfo verificationInfo)
    {
        verificationInfo.Validate();
        ThrowHelpers.ThrowIfNullOrEmpty(version, () => new ArgumentException("Version was Null or Empty"));

        var releases = Releases.Where(x => x.Version.Equals(version, StringComparison.OrdinalIgnoreCase)).OrderByDescending(item => item.ReleaseType.GetPackageTypePriority());
        foreach (var release in releases)
        {
            if (release.TypeCanApply(verificationInfo.FolderPath))
                return release;
        }

        return null;
    }

    /// <summary>
    /// Gets extra data as an instanca of Type T
    /// </summary>
    /// <typeparam name="T">Any type T containing extra data.</typeparam>
    /// <returns>Extra data casted to desired return type.</returns>
    public T? GetExtraData<T>() where T : class
    {
        if (ExtraData is JsonElement element)
            return JsonSerializer.Deserialize<T>(element.GetRawText());

        return ExtraData as T;
    }

    /// <summary>
    /// Sets extra data as an instance of Type T
    /// </summary>
    /// <typeparam name="T">Any type T containing extra data.</typeparam>
    public void SetExtraData<T>(T value) where T : class => ExtraData = value;
}

/// <summary>
/// Information used for verifying the compatibility of e.g. Delta packages
/// in release metadata
/// </summary>
public class ReleaseMetadataVerificationInfo
{
    /// <summary>
    /// Path where the release will be extracted.
    /// </summary>
    public string FolderPath { get; set; } = "";
    
    /// <summary/>
    public void Validate()
    {
        ThrowHelpers.ThrowIfNullOrEmpty(FolderPath, () => new ValidationFailedException("Folder Path was Null or Empty"));
    }
}

/// <summary />
public class ReleaseItem
{
    /// <summary>
    /// The type of release this item contains.
    /// </summary>
    public PackageType ReleaseType { get; set; } = PackageType.Copy;

    /// <summary>
    /// Contains the name of the item to download.
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// Semantic version for this release item.
    /// </summary>
    public string Version { get; set; } = "";

    /// <summary>
    /// Contains all delta package related properties.
    /// Valid if the package is a <see cref="PackageType.Delta"/>.
    /// </summary>
    public DeltaReleaseItem? Delta { get; set; }

    /// <summary>
    /// Returns true if a release type can be applied, else false.
    /// </summary>
    internal bool TypeCanApply(string folderPath)
    {
        switch (ReleaseType)
        {
            case PackageType.Copy:
                return true;
            case PackageType.Delta:
                return HashSet.Verify(Delta!.DeltaHashes, folderPath, out _, out _);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

/// <summary />
public class DeltaReleaseItem
{
    /// <summary>
    /// The package version the delta is intended to support.
    /// Used as a quick string check before checking DeltaHashes.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Valid if the item is a delta package.
    /// Contains a list of hashes of all files to be patched.
    /// Used for validation before applying delta patch.
    /// </summary>
    public FileHashSet? DeltaHashes { get; set; }
}