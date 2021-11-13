using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Resolvers.NuGet.Utilities;

namespace Sewer56.Update.Resolvers.NuGet;

/// <summary>
/// Provides support for easy extracting of NuGet packages.
/// </summary>
public class NuGetPackageExtractor : IPackageExtractor
{
    private NuGetPackageExtractorSettings _extractorSettings;

    /// <summary>
    /// Settings for the NuGet Package.
    /// </summary>
    /// <param name="archiveSettings">Archive settings.</param>
    public NuGetPackageExtractor(NuGetPackageExtractorSettings? archiveSettings = null)
    {
        _extractorSettings = archiveSettings ?? new NuGetPackageExtractorSettings();
    }

    /// <inheritdoc />
    public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        await using var fileStream = new FileStream(sourceFilePath, FileMode.Open);
        using var reader           = new PackageArchiveReader(fileStream);
        var allFiles               = GetFiles(reader, out string contentFolder);
        var singleItemProgress     = 1.0 / allFiles.Length;

        var mixer = new ProgressSlicer(progress);
        foreach (var file in allFiles)
        {
            var slicedProgress = mixer.Slice(singleItemProgress);
            var relativePath   = contentFolder.Length == 0 ? file : Paths.GetRelativePath(file, contentFolder);
            var destFilePath   = Path.Combine(destDirPath, relativePath);
            await ExtractFileAsync(file, destFilePath, reader, slicedProgress, cancellationToken);
        }
    }

    private string[] GetFiles(PackageArchiveReader reader, out string contentFolder)
    {
        var allFiles = reader.GetFiles(_extractorSettings.NupkgContentFolder).RemoveDirectories().ToArray();
        contentFolder = _extractorSettings.NupkgContentFolder;

        // Fallback to root directory if subfolder not found.
        if (allFiles.Length == 0)
        {
            allFiles = reader.GetFiles().RemoveDirectories().ToArray();
            contentFolder = "";
        }

        return allFiles;
    }

    private async Task ExtractFileAsync(string file, string destFilePath, PackageArchiveReader reader, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var entry = reader.GetEntry(file);
        var resultPath = destFilePath;

        Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
        await using var inputStream = entry.Open();
        await using var outputFileStream = new FileStream(resultPath, FileMode.Create);
        await inputStream.CopyToAsyncEx(outputFileStream, 131072, progress, cancellationToken);
    }
}