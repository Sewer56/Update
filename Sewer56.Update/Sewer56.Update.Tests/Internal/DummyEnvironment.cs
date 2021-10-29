using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Polly;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Enums;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Packaging.Structures.ReleaseBuilder;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

namespace Sewer56.Update.Tests.Internal;

internal class DummyEnvironment : IDisposable
{
    private static readonly Assembly DummyAssembly = typeof(Dummy.Program).Assembly;
    private static readonly string DummyAssemblyFileName = Path.GetFileName(DummyAssembly.Location)!;
    private static readonly string DummyAssemblyDirPath  = Path.GetDirectoryName(DummyAssembly.Location)!;

    private readonly string _rootDirPath;

    private string DummyFilePath { get; }

    private string DummyPackagesDirPath { get; }

    private string BasePackageDirPath { get; }

    public DummyEnvironment(string rootDirPath)
    {
        _rootDirPath = rootDirPath;

        DummyPackagesDirPath = Path.Combine(_rootDirPath, "Packages");
        BasePackageDirPath = Path.Combine(_rootDirPath, "Base");
        DummyFilePath = Path.Combine(BasePackageDirPath, DummyAssemblyFileName);
    }

    private void SetAssemblyVersion(string filePath, Version version)
    {
        using var assemblyStream     = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite);
        using var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyStream);

        assemblyDefinition.Name.Version = version;
        assemblyDefinition.Write(assemblyStream);
    }

    private void CreateBase(Version version)
    {
        Directory.CreateDirectory(DummyPackagesDirPath);
        Directory.CreateDirectory(BasePackageDirPath);

        // Copy files
        foreach (var filePath in Directory.EnumerateFiles(DummyAssemblyDirPath))
        {
            var fileName = Path.GetFileName(filePath);
            File.Copy(filePath, Path.Combine(BasePackageDirPath, fileName));
        }

        // Change base dummy version
        SetAssemblyVersion(DummyFilePath, version);
    }

    private string CreatePackage(Version version)
    {
        // Temporarily copy the dummy
        var dummyTempFilePath    = Path.Combine(DummyPackagesDirPath, version.ToString(), $"{DummyAssemblyFileName}"); // File path
        var dummyTargetDirectory = Path.GetDirectoryName(dummyTempFilePath);

        Directory.CreateDirectory(dummyTargetDirectory!);
        IOEx.CopyDirectory(BasePackageDirPath, dummyTargetDirectory);

        // Change dummy version
        SetAssemblyVersion(dummyTempFilePath, version);
        
        // Delete temp file
        return dummyTargetDirectory;
    }

    private void Cleanup()
    {
        // Sometimes this fails for some reason, even when dummy has already exited.
        // Use a retry policy to circumvent that.
        var policy = Policy.Handle<UnauthorizedAccessException>().WaitAndRetry(5, _ => TimeSpan.FromSeconds(1));
        policy.Execute(() => IOEx.TryDeleteDirectory(_rootDirPath));
    }

    public async Task SetupAsync(Version baseVersion, IReadOnlyList<Version> availableVersions, PackageType packageType = PackageType.Copy)
    {
        Cleanup();
        CreateBase(baseVersion);

        var releaseBuilder = new ReleaseBuilder<Empty>();
        var packageFolders = MakePackagesForVersions(availableVersions);
        foreach (var packageFolder in packageFolders)
        {
            switch (packageType)
            {
                case PackageType.Copy:
                    releaseBuilder.AddCopyPackage(new CopyBuilderItem<Empty>()
                    {
                        FolderPath = packageFolder.Item1,
                        Version = packageFolder.Item2.ToString()
                    });
                    break;
                case PackageType.Delta:
                    releaseBuilder.AddDeltaPackage(new DeltaBuilderItem<Empty>()
                    {
                        FolderPath = packageFolder.Item1,
                        Version = packageFolder.Item2.ToString(),

                        PreviousVersion = DummyAssembly.GetName().Version.ToString(),
                        PreviousVersionFolder = BasePackageDirPath
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(packageType), packageType, null);
            }
        }

        await releaseBuilder.BuildAsync(new BuildArgs()
        {
            FileName     = DummyAssemblyFileName,
            OutputFolder = DummyPackagesDirPath
        });
    }

    private List<(string, Version)> MakePackagesForVersions(IReadOnlyList<Version> availableVersions)
    {
        var packageDirs = new List<(string, Version)>();
        foreach (var version in availableVersions)
            packageDirs.Add((CreatePackage(version), version));

        return packageDirs;
    }

    public async Task<string> RunDummyAsync(params string[] arguments)
    {
        var result = await Cli.Wrap("dotnet")
            .WithArguments(a => a
                .Add(DummyFilePath)
                .Add(arguments))
            .ExecuteBufferedAsync();

        return result.StandardOutput;
    }

    public void Dispose() => Cleanup();
}