using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using FluentValidation;
using Mapster;
using NuGet.Packaging;
using NuGet.Protocol.Plugins;
using NuGet.Versioning;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Extractors.SevenZipSharp;
using Sewer56.Update.Extractors.SharpCompress;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Compressors;
using Sewer56.Update.Packaging.Interfaces;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Packaging.Structures.ReleaseBuilder;
using Sewer56.Update.Resolvers.GameBanana;
using Sewer56.Update.Resolvers.GitHub;
using Sewer56.Update.Resolvers.NuGet;
using Sewer56.Update.Resolvers.NuGet.Utilities;
using Sewer56.Update.Structures;
using Sewer56.Update.Tool.Options;
using Sewer56.Update.Tool.Options.Groups;
using Sewer56.Update.Tool.Validation;
using ShellProgressBar;
using IPackageResolver = Sewer56.Update.Interfaces.IPackageResolver;

namespace Sewer56.Update.Tool;

internal class Program
{
    static async Task Main(string[] args)
    {
        var parser = new Parser(with =>
        {
            with.AutoHelp = true;
            with.CaseSensitive = false;
            with.CaseInsensitiveEnumValues = true;
            with.EnableDashDash = true;
            with.HelpWriter = null;
        });

        var parserResult = parser.ParseArguments<CreateReleaseOptions, CreateCopyPackageOptions, CreateDeltaPackageOptions, DownloadPackageOptions, AutoCreateDeltaOptions>(args);
        await parserResult.WithParsedAsync<CreateReleaseOptions>(CreateRelease);
        await parserResult.WithParsedAsync<CreateCopyPackageOptions>(CreateCopyPackage);
        await parserResult.WithParsedAsync<CreateDeltaPackageOptions>(CreateDeltaPackage);
        await parserResult.WithParsedAsync<DownloadPackageOptions>(DownloadPackage);
        await parserResult.WithParsedAsync<AutoCreateDeltaOptions>(AutoCreateDeltaOptions);

        parserResult.WithNotParsed(errs => HandleParseError(parserResult, errs));
    }

    private static async Task DownloadPackage(DownloadPackageOptions options)
    {
        using var progressBar = new ProgressBar(10000, "Downloading Package");
        await DownloadPackageInternal(options, progressBar.AsProgress<double>(), message => progressBar.Message = message, true);
    }

    private static async Task CreateCopyPackage(CreateCopyPackageOptions options)
    {
        var validator = new CreateCopyPackageOptionsValidator();
        validator.ValidateAndThrow(options);

        var ignoreRegexes = string.IsNullOrEmpty(options.IgnoreRegexesPath) ? null : (await File.ReadAllLinesAsync(options.IgnoreRegexesPath)).ToList();
        var includeRegexes = string.IsNullOrEmpty(options.IncludeRegexesPath) ? null : (await File.ReadAllLinesAsync(options.IncludeRegexesPath)).ToList();
        await Package<Empty>.CreateAsync(options.FolderPath, options.OutputPath, options.Version, null, ignoreRegexes, includeRegexes);
    }

    private static async Task CreateDeltaPackage(CreateDeltaPackageOptions options)
    {
        using var progressBar = new ProgressBar(10000, "Downloading Package");
        await CreateDeltaPackageInternal(options, progressBar.AsProgress<double>(), message => progressBar.Message = message);
    }


    /// <summary>
    /// Creates a new package.
    /// </summary>
    private static async Task CreateRelease(CreateReleaseOptions releaseOptions)
    {
        // Act
        using var progressBar = new ProgressBar(10000, "Building Release");
        await CreateReleaseInternal(releaseOptions, progressBar.AsProgress<double>());
    }

    /// <summary>
    /// Automatically creates delta packages.
    /// </summary>
    private static async Task AutoCreateDeltaOptions(AutoCreateDeltaOptions options)
    {
        using var progressBar = new ProgressBar(10000, "Resolving Available Versions");
        var validator = new AutoCreateDeltaValidator();
        validator.ValidateAndThrow(options);

        // Create Output Folder
        options.OutputPath = Path.GetFullPath(options.OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!);

        // Resolve Packages
        var resolver = await SetupResolverAsync(options);
        var versions = await resolver.GetPackageVersionsAsync();

        // Download Packages
        var slicer = new ProgressSlicer(progressBar.AsProgress<double>());
        var resolverOptions = (IPackageResolverOptions) options;
        var currentPackageDetails = (ICurrentPackageDetails) options;

        for (int x = 0; x < options.NumReleases; x++)
        {
            // Setup for this item.
            using var tempFolder = new TemporaryFolderAllocation();
            var progressForThisItem = slicer.Slice(1.0 / options.NumReleases);
            var slicerForThisItem = new ProgressSlicer(progressForThisItem);

            // Set download package options.
            var downloadOptions = new DownloadPackageOptions()
            {
                OutputPath = tempFolder.FolderPath,
                Extract = true,
                ReleaseIndex = (x + 1)
            };

            // Copy resolver options.
            resolverOptions.Adapt(downloadOptions);

            // Download package.
            var maxNumReleases = options.NumReleases - 1;
            void ReportMessage(string message) => progressBar.Message = $"[Version {x}/{maxNumReleases}] {message}";
            var previousVersion = await SelectAndDownloadVersion(downloadOptions, slicerForThisItem.Slice(0.8), ReportMessage, resolver, versions);

            // Create Delta
            var createDeltaOptions = new CreateDeltaPackageOptions()
            {
                LastVersion = previousVersion,
                LastVersionFolderPath = downloadOptions.OutputPath,
                OutputPath = Path.Combine(options.OutputPath, $"delta-package-{x}")
            };
            currentPackageDetails.Adapt(createDeltaOptions);

            await CreateDeltaPackageInternal(createDeltaOptions, slicerForThisItem.Slice(0.8), ReportMessage);
            await Console.Out.WriteLineAsync(createDeltaOptions.OutputPath);
        }
    }

    private static async Task<string> DownloadPackageInternal(DownloadPackageOptions options, IProgress<double> progress, ReportProgressMessage reportMessage = null, bool writeVersionToStdout = false)
    {
        var validator = new PackageResolverOptionsValidator();
        validator.ValidateAndThrow(options);

        Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!);
        var resolver = await SetupResolverAsync(options, reportMessage);
        var versions = await resolver.GetPackageVersionsAsync();

        return await SelectAndDownloadVersion(options, progress, reportMessage, resolver, versions, writeVersionToStdout);
    }

    private static async Task<string> SelectAndDownloadVersion(IDownloadPackageOptions options, IProgress<double> progress, ReportProgressMessage reportMessage, IPackageResolver resolver, List<NuGetVersion> versions, bool writeVersionToStdout = false)
    {
        var lastVersion = versions.Count > 0 ? versions[^(options.ReleaseIndex + 1)] : null;
        var versionString = lastVersion!.ToString();
        if (writeVersionToStdout)
            await Console.Out.WriteLineAsync(versionString);

        if (options.Extract)
        {
            // Download to temp folder and extract.
            using var tempFolder = new TemporaryFolderAllocation();
            var tempPath = Path.Combine(tempFolder.FolderPath, $"{Path.GetRandomFileName()}.pkg");
            var slicer = new ProgressSlicer(progress);

            await resolver.DownloadPackageAsync(lastVersion, tempPath,
                new ReleaseMetadataVerificationInfo() { FolderPath = Path.GetDirectoryName(tempPath)! }, slicer.Slice(0.8));

            reportMessage?.Invoke("Extracting Package");
            await GetExtractor().ExtractPackageAsync(tempPath, options.OutputPath, slicer.Slice(0.2));
        }
        else
        {
            reportMessage?.Invoke("Downloading Package");
            await resolver.DownloadPackageAsync(lastVersion, options.OutputPath,
                new ReleaseMetadataVerificationInfo()
                    { FolderPath = Path.GetDirectoryName(Path.GetFullPath(options.OutputPath))! }, progress);
        }

        return versionString;
    }

    private static async Task<IPackageResolver> SetupResolverAsync(IPackageResolverOptions options, ReportProgressMessage reportMessage = null)
    {
        var commonResolverSettings = new CommonPackageResolverSettings()
        {
            AllowPrereleases = options.AllowPrereleases.GetValueOrDefault(),
            MetadataFileName = options.MetadataFileName
        };

        IPackageResolver resolver = options.Source switch
        {
            DownloadSource.GitHub => new GitHubReleaseResolver(new GitHubResolverConfiguration()
            {
                UserName = options.GitHubUserName,
                RepositoryName = options.GitHubRepositoryName,
                LegacyFallbackPattern = options.GitHubLegacyFallbackPattern,
                InheritVersionFromTag = options.GitHubInheritVersionFromTag.GetValueOrDefault()
            }, commonResolverSettings),
            DownloadSource.NuGet => new NuGetUpdateResolver(new NuGetUpdateResolverSettings()
            {
                PackageId = options.NuGetPackageId,
                AllowUnlisted = options.AllowPrereleases.GetValueOrDefault(),
                NugetRepository = new NugetRepository(options.NuGetFeedUrl),
            }, commonResolverSettings),
            DownloadSource.GameBanana => new GameBananaUpdateResolver(new GameBananaResolverConfiguration()
            {
                ItemId = options.GameBananaItemId,
                ModType = options.GameBananaModType
            }, commonResolverSettings),
            _ => throw new ArgumentOutOfRangeException()
        };

        reportMessage?.Invoke("Initializing Package Resolver");
        await resolver.InitializeAsync();
        return resolver;
    }


    private static async Task CreateDeltaPackageInternal(CreateDeltaPackageOptions options, IProgress<double> progress, ReportProgressMessage reportProgressMessage = null)
    {
        var validator = new CreateDeltaPackageValidator();
        validator.ValidateAndThrow(options);

        var ignoreRegexes = string.IsNullOrEmpty(options.IgnoreRegexesPath) ? null : (await File.ReadAllLinesAsync(options.IgnoreRegexesPath)).ToList();
        var includeRegexes = string.IsNullOrEmpty(options.IncludeRegexesPath) ? null : (await File.ReadAllLinesAsync(options.IncludeRegexesPath)).ToList();
        await Package<Empty>.CreateDeltaAsync(options.LastVersionFolderPath, options.FolderPath, options.OutputPath,
            options.LastVersion, options.Version, null, ignoreRegexes,
            (text, d) =>
            {
                progress.Report(d);
                reportProgressMessage?.Invoke(text);
            }, includeRegexes);
    }

    /// <summary>
    /// Creates a new package.
    /// </summary>
    private static async Task CreateReleaseInternal(CreateReleaseOptions releaseOptions, IProgress<double> progress)
    {
        // Validate and set defaults.
        var validator = new CreateReleaseOptionsValidator();
        validator.ValidateAndThrow(releaseOptions);
        if (releaseOptions.MaxParallelism == ICreateReleaseOptions.DefaultInt)
            releaseOptions.MaxParallelism = Environment.ProcessorCount;

        // Get in there!
        var existingPackages = string.IsNullOrEmpty(releaseOptions.ExistingPackagesPath) ? new List<string>() : (await File.ReadAllLinesAsync(releaseOptions.ExistingPackagesPath)).ToList();
        Directory.CreateDirectory(releaseOptions.OutputPath);

        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        foreach (var existingPackage in existingPackages)
        {
            builder.AddExistingPackage(new ExistingPackageBuilderItem()
            {
                Path = existingPackage
            });
        }

        // Act
        await builder.BuildAsync(new BuildArgs()
        {
            FileName = releaseOptions.PackageName,
            OutputFolder = releaseOptions.OutputPath,
            PackageArchiver = GetArchiver(releaseOptions),
            MaxParallelism = releaseOptions.MaxParallelism,
            AutoGenerateDelta = releaseOptions.AutoGenerateDelta,
            DontAppendVersionToPackages = releaseOptions.DontAppendVersionToPackages,
            PackageExtractor = GetExtractor()
        }, progress);
    }

    private static IPackageExtractor GetExtractor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && (RuntimeInformation.OSArchitecture == Architecture.X86 || RuntimeInformation.OSArchitecture == Architecture.X64))
            return new SevenZipSharpExtractor();

        return new SharpCompressExtractor();
    }

    private static IPackageArchiver GetArchiver(CreateReleaseOptions releaseOptions)
    {
        return releaseOptions.Archiver switch
        {
            Archiver.Zip => new ZipPackageArchiver(),
            Archiver.NuGet => new NuGetPackageArchiver(releaseOptions.GetArchiver()),
            Archiver.SharpCompress => releaseOptions.SharpCompressFormat.GetArchiver(),
            Archiver.SevenZipSharp => new SevenZipSharpArchiver(new SevenZipSharpArchiverSettings()
            {
                CompressionLevel = releaseOptions.SevenZipSharpCompressionLevel,
                ArchiveFormat = releaseOptions.SevenZipSharpArchiveFormat,
                CompressionMethod = releaseOptions.SevenZipSharpCompressionMethod
            }),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <summary>
    /// Errors or --help or --version.
    /// </summary>
    static void HandleParseError(ParserResult<object> options, IEnumerable<Error> errs)
    {
        var helpText = HelpText.AutoBuild(options, help =>
        {
            help.Copyright = "Created by Sewer56, licensed under GNU LGPL V3";
            help.AutoHelp = false;
            help.AutoVersion = false;
            help.AddDashesToOption = true;
            help.AddEnumValuesToHelpText = true;
            help.AddNewLineBetweenHelpSections = true;
            help.AdditionalNewLineAfterOption = true;
            return HelpText.DefaultParsingErrorsHandler(options, help);
        }, example => example, true);

        Console.WriteLine(helpText);
    }

    private delegate void ReportProgressMessage(string message);
}