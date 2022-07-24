using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Sewer56.DeltaPatchGenerator.Lib.Utility;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging;
using Sewer56.Update.Packaging.Structures;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sewer56.Update.Hooks;

/// <summary />
public static class Startup
{
    /// <summary />
    public const string ParameterDelimiter = "--sewer56.update";

    /// <summary>
    /// A handler for command line arguments passed to the program.
    /// </summary>
    /// <returns>True if the process should exit, else false.</returns>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Preserved in Module Initializer")]
#endif
    public static bool HandleCommandLineArgs(string[] args, bool printToConsoleOnError = true)
    {
        StartupParams? startupParams = null;
        for (var x = 0; x < args.Length; x++)
        {
            if (args[x] == ParameterDelimiter)
                startupParams = JsonSerializer.Deserialize<StartupParams>(args[x + 1].FromBase64());
        }

        if (startupParams == null)
            return false;

        // Wait for last process to finish.
        try
        {
            var lastProcess = Process.GetProcessById(startupParams.CurrentProcessId);
            lastProcess.WaitForExit();
        }
        catch (ArgumentException)
        {
            /* Ignore if process already died. */
        }

        // Copy out specified contents.
        IOEx.CopyDirectory(startupParams.PackageContentPath, startupParams.TargetDirectory);

        // Cleanup (if necessary)
        if (startupParams.CleanupAfterUpdate)
        {
            var metadata = Task.Run(() => Package<Empty>.ReadOrCreateLegacyMetadataFromDirectoryAsync(startupParams.PackageContentPath)).Result;
            metadata.Cleanup(startupParams.TargetDirectory);
        }

        IOEx.TryDeleteDirectory(startupParams.PackageContentPath);
        if (!string.IsNullOrEmpty(startupParams.StartupApplication))
            Process.Start(startupParams.StartupApplication, startupParams.StartupApplicationArgs);

        return true;
    }

    /// <summary>
    /// Gets the start information.
    /// </summary>
    /// <param name="executablePath">Executable path of the current program.</param>
    /// <param name="baseDirectory">Base directory for the current program.</param>
    /// <param name="targetDirectory">Folder containing the new version of the program.</param>
    /// <param name="arguments">Arguments to pass to the application.</param>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Preserved in Module Initializer")]
#endif
    internal static ProcessStartInfo GetProcessStartInfo(string executablePath, string baseDirectory, string targetDirectory, StartupParams arguments)
    {
        var relativePath         = Paths.GetRelativePath(executablePath, baseDirectory);
        var targetExecutablePath = Paths.AppendRelativePath(relativePath, targetDirectory);

        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = Path.GetDirectoryName(targetExecutablePath)
        };

        // If updatee is an .exe file - start it directly
        if (string.Equals(Path.GetExtension(targetExecutablePath), ".exe", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.FileName = targetExecutablePath;
        }
        else
        {
            // If not - figure out what to do with it
            // If there's an .exe file with same name - start it instead. Security vulnerability?
            if (File.Exists(Path.ChangeExtension(targetExecutablePath, ".exe")))
            {
                startInfo.FileName = Path.ChangeExtension(targetExecutablePath, ".exe");
            }
            else
            {
                // Otherwise - start the updatee using dotnet runtime/SDK
                startInfo.FileName = "dotnet";
                startInfo.ArgumentList.Add(targetExecutablePath);
            }
        }

        startInfo.ArgumentList.Add(ParameterDelimiter);
        startInfo.ArgumentList.Add(JsonSerializer.Serialize(arguments).ToBase64());

        return startInfo;
    }
}

internal class StartupParams
{
    /// <summary>
    /// Path containing the files to copy to the output.
    /// </summary>
    public string PackageContentPath { get; set; } = "";

    /// <summary>
    /// The directory where to copy the output to.
    /// </summary>
    public string TargetDirectory { get; set; } = "";

    /// <summary>
    /// The command to start the next process.
    /// </summary>
    public string StartupApplication { get; set; } = "";

    /// <summary>
    /// The arguments to pass to the next process.
    /// </summary>
    public string StartupApplicationArgs { get; set; } = "";

    /// <summary>
    /// Id of the process to wait before it dies.
    /// </summary>
    public int CurrentProcessId { get; set; } = 0;

    /// <summary>
    /// True if old version files should be cleaned up (removed) after an update, else false.
    /// </summary>
    public bool CleanupAfterUpdate { get; set; } = true;
}