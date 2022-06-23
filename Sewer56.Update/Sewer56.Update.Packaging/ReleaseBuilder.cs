﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Compressors;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Exceptions;
using Sewer56.Update.Packaging.Extractors;
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

        // Get potential existing metadata.
        var metadata = await Singleton<ReleaseMetadata>.Instance.ReadFromDirectoryOrDefaultAsync(args.OutputFolder);

        // Add auto delta packages
        using var foldersToDelete = new TemporaryFolderAllocationCollection();
        if (metadata.Releases.Count > 0 && args.AutoGenerateDelta)
            await AddAutoGeneratedDeltas(metadata, args, foldersToDelete);

        // Validate for only 1 copy package if no version suffix used
        await ValidateDontAppendVersionToPackagesAsync(args);

        // Build/update metadata.
        var metadataBuilder = new ReleaseMetadataBuilder<T>();
        var progressMixer   = new ProgressSlicer(progress);
        var singleItemProgress = (double) 1 / Items.Count;
        using var concurrencySemaphore = new SemaphoreSlim(args.MaxParallelism);
        var taskFunctions = new Func<Task>[Items.Count];
        var tasks = new Task[Items.Count];

        // Make all packages first.
        for (var x = 0; x < Items.Count; x++)
        {
            var item = Items[x];
            var itemProgress = progressMixer.Slice(singleItemProgress);
            switch (item)
            {
                case ExistingPackageBuilderItem existingPackageItem:
                    taskFunctions[x] = await BuildExistingPackageItem(metadataBuilder, existingPackageItem, args, itemProgress);
                    break;
                case CopyBuilderItem<T> copyBuilderItem:
                    taskFunctions[x] = await BuildCopyItem(metadataBuilder, copyBuilderItem, args, itemProgress);
                    break;
                case DeltaBuilderItem<T> deltaBuilderItem:
                    taskFunctions[x] = await BuildDeltaItem(metadataBuilder, deltaBuilderItem, args, itemProgress);
                    break;
            }
        }

        // Start concurrent compress operations
        for (int x = 0; x < taskFunctions.Length; x++)
        {
            await concurrencySemaphore.WaitAsync();
#pragma warning disable CS4014
            
            var innerTask = taskFunctions[x]();
            innerTask.ContinueWith(task =>
            {
                concurrencySemaphore.Release();
            });
#pragma warning restore CS4014
            tasks[x] = innerTask;
        }

        Task.WaitAll(tasks);

        metadataBuilder.AddExtraData(args.ReleaseExtraData);
        metadata = metadataBuilder.Build(metadata);
        await metadata.ToDirectoryAsync(args.OutputFolder, args.MetadataFileName, args.JsonCompressionMode);
        progress?.Report(1);
        return metadata;
    }

    private async Task AddAutoGeneratedDeltas(ReleaseMetadata metadata, BuildArgs args, TemporaryFolderAllocationCollection tempFolderAllocationCollection)
    {
        foreach (var item in Items.ToArray()) // Copy because collection will change mid run.
        {
            switch (item)
            {
                case ExistingPackageBuilderItem existingPackageItem:
                {
                    var existingPackageMeta = await Package<T>.ReadMetadataFromDirectoryAsync(existingPackageItem.Path);
                    if (existingPackageMeta == null)
                        throw new FileNotFoundException($"Failed to read package metadata from existing directory. Directory: {existingPackageItem.Path}");

                    var previousVersion     = await GetPreviousVersion(existingPackageMeta.Version, metadata, args, tempFolderAllocationCollection);
                    if (previousVersion.version == null)
                        continue;

                    AddDeltaPackage(new DeltaBuilderItem<T>()
                    {
                        Data = existingPackageMeta.ExtraData,
                        Version = existingPackageMeta.Version,
                        FolderPath = existingPackageItem.Path,
                        IgnoreRegexes = existingPackageMeta.IgnoreRegexes,
                        IncludeRegexes = existingPackageMeta.IncludeRegexes,
                        PreviousVersion = previousVersion.version,
                        PreviousVersionFolder = previousVersion.path!
                    });

                    break;
                }

                case CopyBuilderItem<T> copyBuilderItem:
                {
                    var previousVersion = await GetPreviousVersion(copyBuilderItem.Version, metadata, args, tempFolderAllocationCollection);
                    if (previousVersion.version == null)
                        continue;

                    AddDeltaPackage(new DeltaBuilderItem<T>()
                    {
                        Data = copyBuilderItem.Data,
                        Version = copyBuilderItem.Version,
                        FolderPath = copyBuilderItem.FolderPath,
                        IgnoreRegexes = copyBuilderItem.IgnoreRegexes,
                        IncludeRegexes = copyBuilderItem.IncludeRegexes,
                        PreviousVersion = previousVersion.version,
                        PreviousVersionFolder = previousVersion.path!,
                    });

                    break;
                }
            }
        }
    }

    private async Task<(string? version, string? path)> GetPreviousVersion(string version, ReleaseMetadata metadata, BuildArgs buildArgs, TemporaryFolderAllocationCollection folderAllocationCollection)
    {
        var sortedByVersion = metadata.Releases.OrderByDescending(x => new NuGetVersion(x.Version));
        var nugetVersion = new NuGetVersion(version);
        string? resultVersion = null;
        string? resultPath = null;

        foreach (var item in sortedByVersion)
        {
            if (new NuGetVersion(item.Version) >= nugetVersion || item.ReleaseType == PackageType.Delta)
                continue;

            // Found next lowest version. Time to extract.
            resultVersion = item.Version;
            var allocation = new TemporaryFolderAllocation();
            resultPath = allocation.FolderPath;

            folderAllocationCollection.Allocations.Add(allocation);

            // Get file path of archive to extract, using name filter if necessary.
            string filePath = Path.Combine(buildArgs.OutputFolder, item.FileName);
            if (!File.Exists(filePath) && buildArgs.FileNameFilter != null)
            {
                filePath = Path.Combine(buildArgs.OutputFolder, buildArgs.FileNameFilter(item.FileName));
                if (!File.Exists(filePath))
                    throw new AutoDeltaException("Unable to find file for existing release.");
            }

            await buildArgs.PackageExtractor.ExtractPackageAsync(filePath, resultPath);
        }

        return (resultVersion, resultPath);
    }

    private async Task<Func<Task>> BuildExistingPackageItem(ReleaseMetadataBuilder<T> metadata, ExistingPackageBuilderItem existingPackageItem, BuildArgs args, IProgress<double> itemProgress)
    {
        var packageMetadata = await Package<T>.ReadMetadataFromDirectoryAsync(existingPackageItem.Path);
        if (packageMetadata == null)
            throw new FileNotFoundException($"Failed to read package metadata from existing directory. Directory: {existingPackageItem.Path}");

        return BuildItemCommon(metadata, args, packageMetadata, GetPackageFileList(packageMetadata), itemProgress);
    }

    private async Task<Func<Task>> BuildDeltaItem(ReleaseMetadataBuilder<T> metadata, DeltaBuilderItem<T> deltaBuilderItem, BuildArgs args, IProgress<double> itemProgress)
    {
        var packageOutputPath       = new TemporaryFolderAllocation();
        GC.SuppressFinalize(packageOutputPath);
        var deltaProgressMixer      = new ProgressSlicer(itemProgress);
        var deltaProgress           = deltaProgressMixer.Slice(0.3);
        var compressProgress        = deltaProgressMixer.Slice(0.7);

        var packageMetadata         = await Package<T>.CreateDeltaAsync(deltaBuilderItem.PreviousVersionFolder, deltaBuilderItem.FolderPath, packageOutputPath.FolderPath,
            deltaBuilderItem.PreviousVersion, deltaBuilderItem.Version, deltaBuilderItem.Data, deltaBuilderItem.IgnoreRegexes,
            (text, progress) =>
            {
                deltaProgress.Report(progress);
            }, deltaBuilderItem.IncludeRegexes);

        return BuildItemCommon(metadata, args, packageMetadata, GetPackageFileList(packageMetadata), compressProgress, true);
    }

    private async Task<Func<Task>> BuildCopyItem(ReleaseMetadataBuilder<T> metadata, CopyBuilderItem<T> copyBuilderItem, BuildArgs args, IProgress<double> itemProgress)
    {
        var packageOutputPath = new TemporaryFolderAllocation();
        GC.SuppressFinalize(packageOutputPath);
        var packageMetadata = await Package<T>.CreateAsync(copyBuilderItem.FolderPath, packageOutputPath.FolderPath, copyBuilderItem.Version, copyBuilderItem.Data, copyBuilderItem.IgnoreRegexes, copyBuilderItem.IncludeRegexes);
        return BuildItemCommon(metadata, args, packageMetadata, GetPackageFileList(packageMetadata), itemProgress, true);
    }

    private Func<Task> BuildItemCommon(ReleaseMetadataBuilder<T> metadata, BuildArgs args, PackageMetadata<T> packageMetadata, List<string> packageFiles, IProgress<double> progress, bool deleteDirectory = false)
    {
        var fileName = GetPackageFileName(packageMetadata, args);
        metadata.AddPackage(new ReleaseMetadataBuilder<T>.ReleaseMetadataBuilderItem()
        {
            FileName = fileName,
            Package = packageMetadata
        });

        packageFiles.Add(packageMetadata.GetDefaultFileName());
        return async () =>
        {
            await args.PackageArchiver.CreateArchiveAsync(packageFiles, packageMetadata.FolderPath!, Path.Combine(args.OutputFolder, fileName), new CreateArchiveExtras()
            {
                Metadata = packageMetadata,
                TotalUncompressedSize = IPackageArchiver.GetTotalFileSize(packageFiles, packageMetadata.FolderPath!)
            }, progress);

            if (deleteDirectory)
                IOEx.TryDeleteDirectory(packageMetadata.FolderPath!);
        };
    }

    [SuppressMessage("ReSharper", "InvokeAsExtensionMethod")]
    private string GetPackageFileName(PackageMetadata<T> package, BuildArgs args)
    {
        var extension = args.PackageArchiver.GetFileExtension();
        string suffix = package.Type switch
        {
            PackageType.Copy  => args.DontAppendVersionToPackages ? $"{extension}" : $"{package.Version}{extension}",
            PackageType.Delta => $"{package.DeltaData!.OldVersion}_to_{package.Version}{extension}",
            _ => throw new ArgumentOutOfRangeException()
        };

        var fileName = StringExtensions.SanitizeFileName(args.FileName + suffix);
        if (args.FileNameFilter != null)
            fileName = args.FileNameFilter(fileName);
        
        return fileName;
    }

    private List<string> GetPackageFileList(PackageMetadata<T> metadata)
    {
        return metadata.Type switch
        {
            PackageType.Copy   => metadata.Hashes!.Files.Select(x => x.RelativePath).ToList(),
            PackageType.Delta  => GetDeltaFileList(metadata),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private List<string> GetDeltaFileList(PackageMetadata<T> metadata) => metadata.DeltaData!.PatchData.FilePathSet.ToList();

    private async Task ValidateDontAppendVersionToPackagesAsync(BuildArgs args)
    {
        if (!args.DontAppendVersionToPackages)
            return;

        int numRegularPackages = 0;
        for (var x = 0; x < Items.Count; x++)
        {
            var item = Items[x];
            if (item is ExistingPackageBuilderItem existingItem)
            {
                var existingPackageMeta = await Package<T>.ReadMetadataFromDirectoryAsync(existingItem.Path);
                if (existingPackageMeta?.Type != PackageType.Delta)
                    numRegularPackages += 1;
            }
            else if (item is not DeltaBuilderItem<T>)
            {
                numRegularPackages += 1;
            }
        }

        if (numRegularPackages > 1)
            throw new BuilderValidationFailedException($"{nameof(args.DontAppendVersionToPackages)} is specified but there is more than 1 non-delta package.");
    }
}

/// <summary/>
public class BuildArgs
{
    /// <summary>
    /// Default parallelism setting; 
    /// </summary>
    public const int DefaultParallelism = -1;

    /// <summary>
    /// The file name for the current release, no extension.
    /// This name will be padded with version info.
    /// It may be shortened.
    /// </summary>
    public string FileName { get; set; } = "";

    /// <summary>
    /// The package name used for release metadata file for the current release.
    /// The metadata contains information about the available packages in this release.
    /// Only override this if hosting multiple items inside one release; e.g. multiple programs in one GitHub Release.
    /// </summary>
    public string MetadataFileName { get; set; } = Singleton<ReleaseMetadata>.Instance.GetDefaultFileName();

    /// <summary>
    /// The folder where everything should be output.
    /// If the folder already contains a release, the release will be updated.
    /// </summary>
    public string OutputFolder { get; set; } = "";

    /// <summary>
    /// Builds the package.
    /// </summary>
    public IPackageArchiver PackageArchiver { get; set; } = new ZipPackageArchiver();

    /// <summary>
    /// A function used for file name filtering.
    /// </summary>
    public Func<string, string>? FileNameFilter { get; set; }

    /// <summary>
    /// Maximum number of concurrent compress tasks.
    /// </summary>
    public int MaxParallelism { get; set; } = DefaultParallelism;

    /// <summary>
    /// Default extractor for existing packages.
    /// Used when <see cref="AutoGenerateDelta"/> setting is enabled.
    /// </summary>
    public IPackageExtractor PackageExtractor = new ZipPackageExtractor();

    /// <summary>
    /// Automatically generate delta package when updating an existing release.
    /// </summary>
    public bool AutoGenerateDelta { get; set; } = false;

    /// <summary>
    /// Does not append version to regular (exiting folder, copy) update packages.  
    /// This option is only valid if there is only 1 Copy/ExistingFolder package in this release.  
    /// </summary>
    public bool DontAppendVersionToPackages { get; set; } = false;

    /// <summary>
    /// Extra data to add to the package release.
    /// </summary>
    public object? ReleaseExtraData { get; set; }

    /// <summary>
    /// The JSON Compression Mode to use.
    /// </summary>
    public JsonCompression JsonCompressionMode { get; set; } = JsonCompression.Brotli;

    /// <summary>
    /// Validates whether the build arguments are correct.
    /// </summary>
    public void Validate()
    {
        ThrowHelpers.ThrowIfNullOrEmpty(FileName, () => new BuilderValidationFailedException($"File name was Null or Empty ({nameof(BuildArgs)}.{nameof(FileName)}) but should not be."));
        ThrowHelpers.ThrowIfNullOrEmpty(OutputFolder, () => new BuilderValidationFailedException($"Output folder name was Null or Empty ({nameof(BuildArgs)}.{nameof(OutputFolder)}) but should not be."));

        if (MaxParallelism == DefaultParallelism)
            MaxParallelism = Environment.ProcessorCount;
    }
}