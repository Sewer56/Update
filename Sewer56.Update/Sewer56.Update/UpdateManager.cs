﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Exceptions;
using Sewer56.Update.Hooks;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Structures;

namespace Sewer56.Update;

/// <inheritdoc />
public class UpdateManager<T> : IUpdateManager where T : class
{
    /// <summary>
    /// The resolver that was used to instantiate this UpdateManager.
    /// </summary>
    public readonly IPackageResolver Resolver;

    /// <summary>
    /// The extractor that was used to instantiate this UpdateManager.
    /// </summary>
    public readonly IPackageExtractor Extractor;

    /// <summary>
    /// Executed when an update to self is about to be applied (i.e. process is about to restart). 
    /// Passed in argument contains the location of data to be copied into current program folder,
    /// after decompression (including deltas) and right before it is applied.
    /// </summary>
    public event Action<string>? OnApplySelfUpdate;

    private Lazy<string> _storageDirPath;
    private bool _cleanupOnDispose = true;

    /// <inheritdoc />
    public ItemMetadata Updatee { get; }

    private UpdateManager(ItemMetadata updatee, IPackageResolver resolver, IPackageExtractor extractor)
    {
        Updatee = updatee;
        Resolver = resolver;
        Extractor = extractor;
        _storageDirPath = new Lazy<string>(() => Utilities.MakeUniqueFolder(Path.GetTempPath()));
    }

    /// <summary>
    /// Initializes an instance of <see cref="UpdateManager{T}"/>.
    /// </summary>
    /// <param name="updatee">Information about the item to be updated.</param>
    /// <param name="resolver">The interface that checks for package updates.</param>
    /// <param name="extractor">The interface that extracts the package.</param>
    public static async Task<UpdateManager<T>> CreateAsync(ItemMetadata updatee, IPackageResolver resolver, IPackageExtractor extractor)
    {
        var result = new UpdateManager<T>(updatee, resolver, extractor);
        await result.Resolver.InitializeAsync();
        return result;
    }

    /// <summary>
    /// Initializes an instance of <see cref="UpdateManager{T}"/>.
    /// </summary>
    /// <param name="resolver">The interface that checks for package updates.</param>
    /// <param name="extractor">The interface that extracts the package.</param>
    public static async Task<UpdateManager<T>> CreateAsync(IPackageResolver resolver, IPackageExtractor extractor)
    {
        return await CreateAsync(ItemMetadata.FromEntryAssembly(), resolver, extractor);
    }
    
    /// <inheritdoc />
    ~UpdateManager() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (_storageDirPath.IsValueCreated && _cleanupOnDispose)
            IOEx.TryDeleteDirectory(_storageDirPath.Value, true);
    }

    /// <inheritdoc />
    public async Task<CheckForUpdatesResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        // Get versions.
        var versions    = await Resolver.GetPackageVersionsAsync(cancellationToken);
        var lastVersion = versions.Count > 0 ? versions[^1] : null;
        var canUpdate   = lastVersion != null && Updatee.Version < lastVersion;
        return new CheckForUpdatesResult(versions, lastVersion, canUpdate);
    }

    /// <inheritdoc />
    public bool IsUpdatePrepared(NuGetVersion version)
    {            
        // Get package file path and content directory path
        var packageFilePath       = GetPackageFilePath(version);
        var packageContentDirPath = GetPackageContentDirPath_Internal(version);

        // Package content directory should exist
        // Package file should have been deleted after extraction
        return !File.Exists(packageFilePath) && Directory.Exists(packageContentDirPath);
    }

    /// <summary>
    /// Tries to get metadata for a given package version.
    /// </summary>
    /// <param name="version">The version to get the release metadata for.</param>
    /// <param name="token">The token used for potentially cancelling this method call.</param>
    /// <returns>Null if not found, else the metadata.</returns>
    public async Task<PackageMetadata<T>?> TryGetPackageMetadataAsync(NuGetVersion version, CancellationToken token = default)
    {
        if (!IsUpdatePrepared(version))
            return null;
        
        return await PackageMetadata<T>.ReadFromDirectoryAsync(GetPackageContentDirPath_Internal(version), token);
    }

    /// <inheritdoc />
    public IReadOnlyList<NuGetVersion> GetPreparedUpdates()
    {
        var result = new List<NuGetVersion>();

        // Enumerate all immediate directories in storage
        if (!Directory.Exists(_storageDirPath.Value)) 
            return result;

        foreach (var packageContentDirPath in Directory.EnumerateDirectories(_storageDirPath.Value))
        {
            // Get directory name
            var packageContentDirName = Path.GetFileName(packageContentDirPath);

            // Try to extract version out of the name
            if (string.IsNullOrWhiteSpace(packageContentDirName) || !NuGetVersion.TryParse(packageContentDirName, out var version))
                continue;

            // If this package is prepared - add it to the list
            if (IsUpdatePrepared(version))
                result.Add(version);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task PrepareUpdateAsync(NuGetVersion version, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        // Set up progress mixer
        var progressMixer = progress != null ? new ProgressSlicer(progress) : null;

        // Get package file path and content directory path
        var packageFilePath       = GetPackageFilePath(version);
        var packageContentDirPath = GetPackageContentDirPath_Internal(version);

        // Ensure storage directory exists
        Directory.CreateDirectory(_storageDirPath.Value);

        // Download package
        await Resolver.DownloadPackageAsync(version, packageFilePath, 
            new ReleaseMetadataVerificationInfo() { FolderPath = Updatee.BaseDirectory },
            progressMixer?.Slice(0.9), // 0% -> 90%
            cancellationToken
        );

        // Ensure package content directory exists and is empty
        IOEx.TryEmptyDirectory(packageContentDirPath);

        // Extract package contents
        await Extractor.ExtractPackageAsync(packageFilePath, packageContentDirPath,
            progressMixer?.Slice(0.1), // 90% -> 100%
            cancellationToken
        );

        // Delete package
        File.Delete(packageFilePath);
    }

    /// <inheritdoc />
    public async Task<bool> StartUpdateAsync(NuGetVersion version, OutOfProcessOptions? outOfProcessOptions, UpdateOptions? updateOptions)
    {
        updateOptions ??= new UpdateOptions();
        outOfProcessOptions ??= new OutOfProcessOptions();

        // Ensure that the current state is valid for this operation
        EnsureUpdatePrepared(version);

        // Get package content directory path.
        var packageContentDirPath = GetPackageContentDirPath_Internal(version);
        var metadata = await Package<T>.ReadOrCreateLegacyMetadataFromDirectoryAsync(packageContentDirPath);

        if (IsPackageCurrentProgram())
        {
            if (updateOptions == null)
                throw new NullReferenceException($"Expected {nameof(updateOptions)} to not be null but was null.");
            
            // Apply delta to self.
            if (metadata.Type == PackageType.Delta)
            {
                using var tempDirectoryAlloc = new TemporaryFolderAllocation();
                metadata.Apply(tempDirectoryAlloc.FolderPath, null, Updatee.BaseDirectory, true, updateOptions.CaseInsensitiveCleanup);
                IOEx.MoveDirectory(tempDirectoryAlloc.FolderPath, metadata.FolderPath);
            }

            var startupParams = new StartupParams()
            {
                CurrentProcessId = Process.GetCurrentProcess().Id,
                PackageContentPath = packageContentDirPath,
                StartupApplication = outOfProcessOptions.Restart ? Updatee.ExecutablePath! : "",
                StartupApplicationArgs = outOfProcessOptions.Restart ? outOfProcessOptions.RestartArguments : "",
                TargetDirectory = Updatee.BaseDirectory,
                CleanupAfterUpdate = updateOptions.CleanupAfterUpdate,
                CaseInsensitiveCleanup = updateOptions.CaseInsensitiveCleanup
            };

            OnApplySelfUpdate?.Invoke(packageContentDirPath!);
            var startInfo = Startup.GetProcessStartInfo(Updatee.ExecutablePath!, Updatee.BaseDirectory, packageContentDirPath, startupParams);
            Process.Start(startInfo);
            _cleanupOnDispose = false;
            return true;
        }

        // Ensure Updatee isn't Currently Used
        if (!string.IsNullOrEmpty(Updatee.ExecutablePath) && File.Exists(Updatee.ExecutablePath) && !IOEx.CheckFileAccess(Updatee.ExecutablePath))
            throw new FileInUseException(Updatee.ExecutablePath!);

        // Parse out the package contents.
        metadata.Apply(Updatee.BaseDirectory, metadata.FolderPath, Updatee.BaseDirectory, updateOptions.CleanupAfterUpdate, updateOptions.CaseInsensitiveCleanup);
        return false;
    }

    /// <inheritdoc />
    public bool TryGetPackageContentDirPath(NuGetVersion version, out string path)
    {
        path = GetPackageContentDirPath_Internal(version);
        return Directory.Exists(path);
    }

    private string GetPackageFilePath(NuGetVersion version) => Path.Combine(_storageDirPath.Value, $"{version}.onv");

    private string GetPackageContentDirPath_Internal(NuGetVersion version) => Path.Combine(_storageDirPath.Value!, $"{version}");

    private void EnsureUpdatePrepared(NuGetVersion version)
    {
        if (!IsUpdatePrepared(version))
            throw new UpdateNotPreparedException(version);
    }
    
    private bool IsPackageCurrentProgram()
    {
        try
        {
            var assembly = ItemMetadata.FromEntryAssembly();
            return assembly.ExecutablePath!.Equals(Updatee.ExecutablePath, StringComparison.OrdinalIgnoreCase) ||
                   Path.ChangeExtension(assembly.ExecutablePath, ".exe").Equals(Updatee.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception) { return false; }
    }
}

/// <summary>
/// Options that affect how updates are applied.
/// </summary>
public class UpdateOptions
{
    /// <summary>
    /// True if old version files should be cleaned up (removed) after an update, else false.
    /// </summary>
    public bool CleanupAfterUpdate = true;
    
    /// <summary>
    /// Performs cleanup in a case insensitive manner.
    /// </summary>
    public bool CaseInsensitiveCleanup = true;
}