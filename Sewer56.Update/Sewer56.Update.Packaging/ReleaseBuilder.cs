using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Compressors;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Exceptions;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.IO;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Packaging.Structures.ReleaseBuilder;
using ThrowHelpers = Sewer56.Update.Misc.ThrowHelpers;

namespace Sewer56.Update.Packaging;

/// <summary>
/// Class that builds a simple release.
/// </summary>
public class ReleaseBuilder<T> where T : class
{
    /// <summary>
    /// The items to be constructed in this release.
    /// </summary>
    public List<object> Items { get; } = new List<object>();

    /// <summary>
    /// Adds an existing package (must be unpacked) to this release.
    /// </summary>
    /// <param name="existingPackageBuilderItem">Builder item containing the location of the existing package.</param>
    public ReleaseBuilder<T> AddExistingPackage(ExistingPackageBuilderItem existingPackageBuilderItem)
    {
        existingPackageBuilderItem.Validate();
        Items.Add(existingPackageBuilderItem);
        return this;
    }

    /// <summary>
    /// Adds a default (copy) package to this release.
    /// </summary>
    /// <param name="copyBuilderItem">Builder item containing the location of the current package version.</param>
    public ReleaseBuilder<T> AddCopyPackage(CopyBuilderItem<T> copyBuilderItem)
    {
        copyBuilderItem.Validate();
        Items.Add(copyBuilderItem);
        return this;
    }

    /// <summary>
    /// Adds a delta package to this release.
    /// </summary>
    /// <param name="deltaBuilderItem">Builder item containing the location of the current and previous package version.</param>
    public ReleaseBuilder<T> AddDeltaPackage(DeltaBuilderItem<T> deltaBuilderItem)
    {
        deltaBuilderItem.Validate();
        Items.Add(deltaBuilderItem);
        return this;
    }

    /// <summary>
    /// Builds the current release.
    /// </summary>
    public async Task<ReleaseMetadata> BuildAsync(BuildArgs args, IProgress<double>? progress = null)
    {
        args.Validate();
        Directory.CreateDirectory(args.OutputFolder);

        var metadataBuilder = new ReleaseMetadataBuilder<T>();
        var progressMixer   = new ProgressSlicer(progress);
        var singleItemProgress = (double) 1 / Items.Count;

        for (var x = 0; x < Items.Count; x++)
        {
            var item = Items[x];
            var itemProgress = progressMixer.Slice(singleItemProgress);
            switch (item)
            {
                case ExistingPackageBuilderItem existingPackageItem:
                    await BuildExistingPackageItem(metadataBuilder, existingPackageItem, args, itemProgress);
                    break;
                case CopyBuilderItem<T> copyBuilderItem:
                    await BuildCopyItem(metadataBuilder, copyBuilderItem, args, itemProgress);
                    break;
                case DeltaBuilderItem<T> deltaBuilderItem:
                    await BuildDeltaItem(metadataBuilder, deltaBuilderItem, args, itemProgress);
                    break;
            }
        }

        var metadata = metadataBuilder.Build();
        await metadata.ToDirectoryAsync(args.OutputFolder);
        return metadata;
    }

    private async Task BuildExistingPackageItem(ReleaseMetadataBuilder<T> metadata, ExistingPackageBuilderItem existingPackageItem, BuildArgs args, IProgress<double> itemProgress)
    {
        var packageMetadata = await Package<T>.ReadMetadataFromDirectoryAsync(existingPackageItem.Path);
        await BuildItemCommon(metadata, args, packageMetadata, GetPackageCopyFiles(packageMetadata), itemProgress);
    }

    private async Task BuildDeltaItem(ReleaseMetadataBuilder<T> metadata, DeltaBuilderItem<T> deltaBuilderItem, BuildArgs args, IProgress<double> itemProgress)
    {
        using var packageOutputPath = new TemporaryFolderAllocation();
        var deltaProgressMixer      = new ProgressSlicer(itemProgress);
        var deltaProgress           = deltaProgressMixer.Slice(0.7);
        var compressProgress        = deltaProgressMixer.Slice(0.3);

        var packageMetadata         = await Package<T>.CreateDeltaAsync(deltaBuilderItem.PreviousVersionFolder, deltaBuilderItem.FolderPath, packageOutputPath.FolderPath,
            deltaBuilderItem.PreviousVersion, deltaBuilderItem.Version, deltaBuilderItem.Data, deltaBuilderItem.IgnoreRegexes,
            (text, progress) =>
            {
                deltaProgress.Report(progress);
            });

        await BuildItemCommon(metadata, args, packageMetadata, GetPackageCopyFiles(packageMetadata), compressProgress);
    }

    private async Task BuildCopyItem(ReleaseMetadataBuilder<T> metadata, CopyBuilderItem<T> copyBuilderItem, BuildArgs args, IProgress<double> itemProgress)
    {
        using var packageOutputPath = new TemporaryFolderAllocation();
        var packageMetadata = await Package<T>.CreateAsync(copyBuilderItem.FolderPath, packageOutputPath.FolderPath, copyBuilderItem.Version, copyBuilderItem.Data, copyBuilderItem.IgnoreRegexes);
        await BuildItemCommon(metadata, args, packageMetadata, GetPackageCopyFiles(packageMetadata), itemProgress);
    }

    private async Task BuildItemCommon(ReleaseMetadataBuilder<T> metadata, BuildArgs args, PackageMetadata<T> packageMetadata, List<string> packageFiles, IProgress<double> progress)
    {
        var fileName = GetPackageFileName(packageMetadata, args);
        metadata.AddPackage(new ReleaseMetadataBuilder<T>.ReleaseMetadataBuilderItem()
        {
            FileName = fileName,
            Package = packageMetadata
        });

        packageFiles.Add(packageMetadata.GetDefaultFileName());
        await args.PackageArchiver.CreateArchiveAsync(packageFiles, packageMetadata.FolderPath!, Path.Combine(args.OutputFolder, fileName), progress);
    }

    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    private string GetPackageFileName(PackageMetadata<T> package, BuildArgs args)
    {
        var extension = args.PackageArchiver.GetFileExtension();
        string suffix = package.Type switch
        {
            PackageType.Copy  => $"{package.Version}{extension}",
            PackageType.Delta => $"{package.DeltaData!.OldVersion}_to_{package.Version}{extension}",
            _ => throw new ArgumentOutOfRangeException()
        };

        if (!args.MaxFileNameLength.HasValue) 
            return StringExtensions.SanitizeFileName(args.FileName + suffix);

        var remainingLength = args.MaxFileNameLength.Value - suffix.Length;
        remainingLength     = Math.Clamp(remainingLength, 0, args.FileName.Length);
        return StringExtensions.SanitizeFileName(args.FileName.Substring(0, remainingLength) + suffix);
    }

    private List<string> GetPackageCopyFiles(PackageMetadata<T> metadata)
    {
        return metadata.Type switch
        {
            PackageType.Copy   => metadata.Hashes!.Files.Select(x => x.RelativePath).ToList(),
            PackageType.Delta  => metadata.DeltaData!.PatchData.ToFileHashSet().Files.Select(x => x.RelativePath).ToList(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

/// <summary/>
public class BuildArgs
{
    /// <summary>
    /// The file name for the current release.
    /// This name will be padded with version info.
    /// It may be shortened.
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// The folder where everything should be output.
    /// </summary>
    public string OutputFolder { get; set; } = "";

    /// <summary>
    /// Builds the package.
    /// </summary>
    public IPackageArchiver PackageArchiver { get; set; } = new ZipPackageArchiver();

    /// <summary>
    /// Trims the filename to a maximum set number of characters.
    /// This is required for some hosting sites.
    /// </summary>
    public int? MaxFileNameLength { get; set; }

    /// <summary>
    /// Validates whether the build arguments are correct.
    /// </summary>
    public void Validate()
    {
        ThrowHelpers.ThrowIfNullOrEmpty(FileName, () => new BuilderValidationFailedException($"File name was Null or Empty ({nameof(BuildArgs)}.{nameof(FileName)}) but should not be."));
        ThrowHelpers.ThrowIfNullOrEmpty(OutputFolder, () => new BuilderValidationFailedException($"Output folder name was Null or Empty ({nameof(BuildArgs)}.{nameof(OutputFolder)}) but should not be."));
    }
}