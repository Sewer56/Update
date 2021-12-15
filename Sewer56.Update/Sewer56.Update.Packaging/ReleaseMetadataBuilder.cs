using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Exceptions;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Packaging;

/// <summary>
/// Builds release metadata for a given set of packages.
/// </summary>
public class ReleaseMetadataBuilder<T> where T : class
{
    /// <summary />
    public BlockingCollection<ReleaseMetadataBuilderItem> Packages { get; private set; }= new BlockingCollection<ReleaseMetadataBuilderItem>();

    /// <summary>
    /// Adds a package to this metadata builder.
    /// </summary>
    /// <returns>The package metadata.</returns>
    /// <exception cref="BuilderValidationFailedException">There's an issue with the item given.</exception>
    public ReleaseMetadataBuilder<T> AddPackage(ReleaseMetadataBuilderItem builderItem)
    {
        builderItem.Validate();
        Packages.Add(builderItem);
        return this;
    }

    /// <summary>
    /// Builds the metadata for this release.
    /// </summary>
    /// <param name="existingMetadata">Existing metadata object to update. Else a new one is created.</param>
    public ReleaseMetadata Build(ReleaseMetadata? existingMetadata = null)
    {
        var result = existingMetadata ?? new ReleaseMetadata();

        foreach (var package in Packages)
        {
            var releaseItem = new ReleaseItem()
            {
                Version     = package.Package!.Version,
                ReleaseType = package.Package.Type,
                FileName    = package.FileName!,
            };

            if (package.Package.Type == PackageType.Delta)
            {
                releaseItem.Delta = new DeltaReleaseItem()
                {
                    Version = package.Package.DeltaData!.OldVersion,
                    DeltaHashes = package.Package.DeltaData.PatchData.ToFileHashSet()
                };
            }

            result.Releases.Add(releaseItem);
        }

        return result;
    }

    /// <summary>
    /// Contains information about an item used in the Release Metadata Builder (<see cref="ReleaseMetadataBuilder{T}"/>),
    /// </summary>
    public class ReleaseMetadataBuilderItem
    {
        /// <summary>
        /// Contains information about the package.
        /// </summary>
        public PackageMetadata<T>? Package;

        /// <summary>
        /// The file name assigned to this package.
        /// </summary>
        public string? FileName;

        /// <summary />
        public void Validate()
        {
            if (Package == null)
                throw new BuilderValidationFailedException($"{nameof(ReleaseMetadataBuilderItem)}.{nameof(Package)} was null.");

            if (String.IsNullOrEmpty(FileName))
                throw new BuilderValidationFailedException($"{nameof(ReleaseMetadataBuilderItem)}.{nameof(FileName)} was null.");
        }
    }
}