using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Resolvers.NuGet;

/// <summary>
/// Allows you to create NuGet compatible package archives.
/// </summary>
public class NuGetPackageArchiver : IPackageArchiver
{
    public NuGetPackageArchiverSettings _settings;

    /// <summary/>
    public NuGetPackageArchiver(NuGetPackageArchiverSettings settings)
    {
        _settings = settings;
    }

    /// <inheritdoc />
    public Task CreateArchiveAsync(List<string> relativeFilePaths, string baseDirectory, string destPath, PackageMetadata metadata, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var writer = new PackageBuilder(false)
        {
            Id = _settings.Id,
            Version = new NuGetVersion(metadata.Version),
            Description = _settings.Description
        };
        
        writer.Authors.AddRange(_settings.Authors);
        writer.TargetFrameworks.Add(new NuGetFramework(_settings.TargetFrameworkName));

        // Add files
        var destination = _settings.NupkgContentFolder;
        foreach (var relativePath in relativeFilePaths)
        {
            writer.Files.Add(new PhysicalPackageFile()
            {
                SourcePath = Paths.AppendRelativePath(relativePath, baseDirectory),
                TargetPath = Path.Combine(destination, relativePath),
            });
        }

        // User stuff
        _settings.OnPreBuild?.Invoke(writer);

        // Save Package
        using var outputStream = new FileStream(destPath, FileMode.Create);
        writer.Save(outputStream);

        progress?.Report(1);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string GetFileExtension() => ".nupkg";
}