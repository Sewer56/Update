using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sewer56.Update.Extensions;
using Sewer56.Update.Hooks;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Packaging.Extractors;
using Sewer56.Update.Packaging.Structures;
using Sewer56.Update.Resolvers;
using Sewer56.Update.Structures;

namespace Sewer56.Update.Tests.Dummy;

public class Program
{
    public const string Command_Version            = "version";
    public const string Command_Update             = "update";
    public const string Command_Update_And_Restart = "update-and-restart";

    private static Version Version => Assembly.GetExecutingAssembly().GetName().Version!;

    private static string AssemblyDirPath => AppDomain.CurrentDomain.BaseDirectory!;

    private static string LogFilePath => Path.Combine(AssemblyDirPath, $"log-{Version}.txt");

    private static string PackagesDirPath => Path.Combine(AssemblyDirPath, "../Packages");

    private static readonly IUpdateManager UpdateManager = Task.Run(() => UpdateManager<Empty>.CreateAsync(
        new LocalPackageResolver(PackagesDirPath),
        new ZipPackageExtractor())).Result;

    static async Task Main(string[] args)
    {
        // Dump arguments to file.
        // This is only accurate enough for simple inputs.
        await using StreamWriter logWriter = File.AppendText(LogFilePath);
        await logWriter.WriteLineAsync($"Starting with Args: ");
        foreach (var item in args)
            await logWriter.WriteLineAsync(item);

        // Get command name
        var command = args.FirstOrDefault();

        if (Startup.HandleCommandLineArgs(args))
        {
            await logWriter.WriteLineAsync($"Startup Handled CMD Args. Exiting.");
            return;
        }

        // Print current assembly version
        if (command == Command_Version || command == null)
        {
            await logWriter.WriteLineAsync($"Printing Version: {Version}");
            Console.WriteLine(Version);
        }
        // Update to latest version
        else if (command == Command_Update || command == Command_Update_And_Restart)
        {
            await logWriter.WriteLineAsync($"Performing Update: {command}");
            var restart = command == Command_Update_And_Restart;
            var progressHandler = new Progress<double>(p => Console.WriteLine($"Progress: {p:P0}"));
            await UpdateManager.CheckPerformUpdateAsync(new OutOfProcessOptions()
            {
                Restart = restart
            }, progressHandler);
        }
    }
}