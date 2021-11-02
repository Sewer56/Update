using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Packaging.Structures.ReleaseBuilder;

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

        var parserResult = parser.ParseArguments<CreateReleaseOptions, CreateCopyPackageOptions, CreateDeltaPackageOptions>(args);
        await parserResult.WithParsedAsync<CreateReleaseOptions>(CreateRelease);
        await parserResult.WithParsedAsync<CreateCopyPackageOptions>(CreateCopyPackage);
        await parserResult.WithParsedAsync<CreateDeltaPackageOptions>(CreateDeltaPackage);
        parserResult.WithNotParsed(errs => HandleParseError(parserResult, errs));
    }

    private static async Task CreateDeltaPackage(CreateDeltaPackageOptions options)
    {
        var ignoreRegexes = string.IsNullOrEmpty(options.IgnoreRegexesPath) ? null : (await File.ReadAllLinesAsync(options.IgnoreRegexesPath)).ToList();
        await Package<Empty>.CreateDeltaAsync(options.LastVersionFolderPath, options.FolderPath, options.OutputPath, options.LastVersion, options.Version, null, ignoreRegexes);
    }

    private static async Task CreateCopyPackage(CreateCopyPackageOptions options)
    {
        var ignoreRegexes = string.IsNullOrEmpty(options.IgnoreRegexesPath) ? null : (await File.ReadAllLinesAsync(options.IgnoreRegexesPath)).ToList();
        await Package<Empty>.CreateAsync(options.FolderPath, options.OutputPath, options.Version, null, ignoreRegexes);
    }

    /// <summary>
    /// Creates a new package.
    /// </summary>
    private static async Task CreateRelease(CreateReleaseOptions releaseOptions)
    {
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
            OutputFolder = releaseOptions.OutputPath
        });
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
            help.AdditionalNewLineAfterOption = true;
            return HelpText.DefaultParsingErrorsHandler(options, help);
        }, example => example, true);

        Console.WriteLine(helpText);
    }
}