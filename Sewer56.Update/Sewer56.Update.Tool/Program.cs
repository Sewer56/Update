using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using CsvHelper;
using CsvHelper.Configuration;
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

        var parserResult = parser.ParseArguments<CreateOptions>(args);
        await parserResult.WithParsedAsync<CreateOptions>(Create);
        parserResult.WithNotParsed(errs => HandleParseError(parserResult, errs));
    }

    /// <summary>
    /// Creates a new package.
    /// </summary>
    private static async Task Create(CreateOptions options)
    {
        var copyPackages  = TryParseCsv<CopyPackage>(options.CopyPackagesPath);
        var deltaPackages = TryParseCsv<DeltaPackage>(options.DeltaPackagesPath);
        var ignoreRegexes = string.IsNullOrEmpty(options.IgnoreRegexesPath) ? null : (await File.ReadAllLinesAsync(options.IgnoreRegexesPath)).ToList();
        Directory.CreateDirectory(options.OutputPath);

        // Arrange
        var builder = new ReleaseBuilder<Empty>();
        foreach (var copyPackage in copyPackages)
        {
            builder.AddCopyPackage(new CopyBuilderItem<Empty>()
            {
                FolderPath = copyPackage.FolderPath,
                Version = copyPackage.Version,
                IgnoreRegexes = ignoreRegexes
            });
        }

        foreach (var deltaPackage in deltaPackages)
        {
            builder.AddDeltaPackage(new DeltaBuilderItem<Empty>()
            {
                FolderPath = deltaPackage.CurrentVersionFolder,
                PreviousVersionFolder = deltaPackage.LastVersionFolder,
                Version = deltaPackage.CurrentVersion,
                PreviousVersion = deltaPackage.LastVersion,
                IgnoreRegexes = ignoreRegexes
            });
        }

        // Act
        await builder.BuildAsync(new BuildArgs()
        {
            FileName = options.PackageName,
            OutputFolder = options.OutputPath
        });
    }

    private static T[] TryParseCsv<T>(string path)
    {
        if (string.IsNullOrEmpty(path))
            return Array.Empty<T>();

        using TextReader reader = new StreamReader(path);
        using var csvReader     = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            Delimiter = ","
        });

        return csvReader.GetRecords<T>().ToArray();
    }

    /// <summary>
    /// Errors or --help or --version.
    /// </summary>
    static void HandleParseError(ParserResult<CreateOptions> options, IEnumerable<Error> errs)
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