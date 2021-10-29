using System;
using System.IO;
using System.Reflection;
using NuGet.Versioning;

namespace Sewer56.Update.Structures;

/// <summary>
/// Contains information about an item to be updated.
/// </summary>
public class ItemMetadata
{
    /// <summary>
    /// Assembly version.
    /// </summary>
    public NuGetVersion Version { get; }

    /// <summary>
    /// Reference file path for package/item.
    /// Must point to a valid file.
    /// This is used to determine if the package/item is currently in use.
    /// 
    /// If updating the current program, this should be the executable of current process.
    /// </summary>
    public string? ExecutablePath { get; }

    /// <summary>
    /// Contains the base folder for this package/item.
    /// This is useful if your application/package has a "bin" folder or similar and the executable
    /// isn't in the root folder of the program.
    /// </summary>
    public string BaseDirectory { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ItemMetadata"/>.
    /// </summary>
    public ItemMetadata(NuGetVersion version, string baseDirectory)
    {
        Version = version;
        BaseDirectory = baseDirectory;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ItemMetadata"/>.
    /// </summary>
    public ItemMetadata(NuGetVersion version, string filePath, string? baseDirectory = null)
    {
        BaseDirectory = baseDirectory ?? Path.GetDirectoryName(filePath)!;
        Version = version;
        ExecutablePath = filePath;
    }

    /// <summary>
    /// Extracts assembly metadata from given assembly.
    /// The specified path is used to override the executable file path in case the assembly is not meant to run directly.
    /// </summary>
    public static ItemMetadata FromAssembly(Assembly assembly, string assemblyFilePath) =>
        new (new NuGetVersion(assembly.GetName().Version!), assemblyFilePath, null);

    /// <summary>
    /// Extracts assembly metadata from given assembly.
    /// </summary>
    public static ItemMetadata FromAssembly(Assembly assembly) => FromAssembly(assembly, assembly.Location);

    /// <summary>
    /// Extracts assembly metadata from entry assembly.
    /// </summary>
    public static ItemMetadata FromEntryAssembly()
    {
        var assembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Can't get entry assembly.");
        return FromAssembly(assembly);
    }
}