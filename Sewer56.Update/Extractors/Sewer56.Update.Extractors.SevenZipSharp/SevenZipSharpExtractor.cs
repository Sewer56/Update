using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SevenZip;
using Sewer56.Update.Packaging.Interfaces;

namespace Sewer56.Update.Extractors.SevenZipSharp;

/// <summary>
/// Extracts archives supported by 7z.dll
/// </summary>
public class SevenZipSharpExtractor : IPackageExtractor
{
    /// <inheritdoc />
    public async Task ExtractPackageAsync(string sourceFilePath, string destDirPath, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        using var extractor = new SevenZipExtractor(sourceFilePath);

        Directory.CreateDirectory(destDirPath);
        extractor.Extracting += (sender, args) => progress?.Report(args.PercentDone / 100.0);
        await extractor.ExtractArchiveAsync(destDirPath);
    }
}