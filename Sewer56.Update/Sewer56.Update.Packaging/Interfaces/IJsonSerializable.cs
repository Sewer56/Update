using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.Update.Packaging.IO;
#if NET5_0_OR_GREATER
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
#endif

namespace Sewer56.Update.Packaging.Interfaces;

/// <summary />
public interface IJsonSerializable
{
    /// <summary>
    /// Gets the default file name for this kind of item.
    /// </summary>
    string GetDefaultFileName();

    /// <summary>
    /// Called after an item is deserialized.
    /// </summary>
    /// <param name="thisItem">An instance of the this item.</param>
    /// <param name="filePath">Path where file was deserialized from. May be null.</param>
    void AfterDeserialize(IJsonSerializable thisItem, string? filePath) { }
}

/// <summary>
/// Common functions used by items implementing <see cref="IJsonSerializable"/>.
/// </summary>
public static class JsonSerializableExtensions
{
    /// <summary>
    /// Writes the current class to a Json file.
    /// </summary>
    /// <param name="serializable">The serializable item.</param>
    /// <param name="filePath">Path to the file to write.</param>
    /// <param name="compressionMode">The compression mode to use for the file write operation.</param>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Preserved All.")]
#endif
    public static async Task ToJsonAsync<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(All)]
#endif
    T>(this T serializable, string filePath, JsonCompression compressionMode) where T : IJsonSerializable, new()
    {
        await using var fileStream = File.Open(filePath, FileMode.Create);
        await using var compressionStream = JsonCompressionExtensions.GetStreamForCompression(fileStream, compressionMode);
        await JsonSerializer.SerializeAsync(compressionStream, serializable);
    }

    /// <summary>
    /// Reads the current item from a Json Directory or returns the default (non-null) item.
    /// </summary>
    /// <param name="serializable">The "this" instance.</param>
    /// <param name="directory">The directory to read item from.</param>
    /// <param name="fileName">Optional custom file name for the item (if not using default).</param>
    /// <param name="token">Allows for cancelling the task.</param>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "Caused by compiler generated code already covered by our annotations.")]
#endif
    public static async Task<T> ReadFromDirectoryOrDefaultAsync<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(All)]
#endif
    T>(this T serializable, string directory, string? fileName = null, CancellationToken token = default) where T : IJsonSerializable, new()
    {
        if (CanReadFromDirectory(serializable, directory, fileName, out var compressionMode, out var fullFilePath))
            return await ReadFromDirectoryAsync_Internal<T>(fullFilePath, token, compressionMode);

        return new T();
    }

    /// <summary>
    /// Reads the current item from a Json Directory or returns the default (null) item.
    /// </summary>
    /// <param name="serializable">The "this" instance.</param>
    /// <param name="directory">The directory to read item from.</param>
    /// <param name="fileName">Optional custom file name for the item (if not using default).</param>
    /// <param name="token">Allows for cancelling the task.</param>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "Caused by compiler generated code already covered by our annotations.")]
#endif
    public static async Task<T?> ReadFromDirectoryOrNullAsync<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(All)]
#endif
    T>(this T serializable, string directory, string? fileName = null, CancellationToken token = default) where T : class, IJsonSerializable, new() 
    {
        if (CanReadFromDirectory(serializable, directory, fileName, out var compressionMode, out var fullFilePath))
            return await ReadFromDirectoryAsync_Internal<T>(fullFilePath, token, compressionMode);

        return null;
    }

    /// <summary>
    /// Reads the current item from a Json Directory.
    /// </summary>
    /// <param name="fullFilePath">Full file path to the file.</param>
    /// <param name="token">Allows for cancelling the task.</param>
    /// <param name="compressionMode">The compression mode to use for the file to be read.</param>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Preserved All.")]
#endif
    internal static async Task<T> ReadFromDirectoryAsync_Internal<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(All)]
#endif
    T>(string fullFilePath, CancellationToken token = default, JsonCompression compressionMode = JsonCompression.Brotli) where T : IJsonSerializable, new()
    {
        await using var fileStream = File.Open(fullFilePath, FileMode.Open);
        await using var decompressionStream = JsonCompressionExtensions.GetStreamForDecompression(fileStream, compressionMode);
        var metadata = await JsonSerializer.DeserializeAsync<T>(decompressionStream, (JsonSerializerOptions?)null, token);
        metadata!.AfterDeserialize(metadata, fullFilePath);
        return metadata;
    }

    /// <summary>
    /// Reads the current item from a given stream.
    /// </summary>
    /// <param name="serializable">The "this" instance.</param>
    /// <param name="stream">The stream containing the json to deserialize</param>
    /// <param name="compressionMode">The compression mode for the file to read.</param>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "Caused by compiler generated code already covered by our annotations.")]
#endif
    public static async Task<T> ReadFromStreamAsync<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(All)]
#endif
    T>(this T serializable, Stream stream, JsonCompression compressionMode = JsonCompression.None) where T : IJsonSerializable, new()
    {
        return await ReadFromStreamAsync<T>(stream, compressionMode);
    }

    /// <summary>
    /// Reads the current item from raw data.
    /// </summary>
    /// <param name="stream">The stream containing the json to deserialize</param>
    /// <param name="compressionMode">The compression mode for the file to read.</param>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Preserved All.")]
#endif
    public static async Task<T> ReadFromStreamAsync<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(All)]
#endif
    T>(Stream stream, JsonCompression compressionMode = JsonCompression.None) where T : IJsonSerializable, new()
    {
        await using var decompressStream = JsonCompressionExtensions.GetStreamForDecompression(stream, compressionMode);
        var metadata = await JsonSerializer.DeserializeAsync<T>(decompressStream);
        metadata!.AfterDeserialize(metadata, "");
        return metadata;
    }

    /// <summary>
    /// Reads the current item from raw data.
    /// </summary>
    /// <param name="serializable">The "this" instance.</param>
    /// <param name="data">The data containing the file to deserialize</param>
    /// <param name="compressionMode">The compression mode for the file to read.</param>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "Caused by compiler generated code already covered by our annotations.")]
#endif
    public static async Task<T> ReadFromDataAsync<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(All)]
#endif
    T>(this T serializable, byte[] data, JsonCompression compressionMode = JsonCompression.None) where T : IJsonSerializable, new()
    {
        return await ReadFromDataAsync<T>(data, compressionMode);
    }

    /// <summary>
    /// Reads the current item from raw data.
    /// </summary>
    /// <param name="data">The data containing the file to deserialize</param>
    /// <param name="compressionMode">The compression mode for the file to read.</param>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Preserved All.")]
#endif
    public static async Task<T> ReadFromDataAsync<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(All)]
#endif
    T>(byte[] data, JsonCompression compressionMode) where T : IJsonSerializable, new()
    {
        await using var memoryStream      = new MemoryStream(data);
        await using var decompressStream  = JsonCompressionExtensions.GetStreamForDecompression(memoryStream, compressionMode);
        
        var metadata = await JsonSerializer.DeserializeAsync<T>(decompressStream);
        metadata!.AfterDeserialize(metadata, "");
        return metadata;
    }

    /// <summary>
    /// Writes the current class to a folder.
    /// </summary>
    /// <param name="serializable">The serializable item.</param>
    /// <param name="folderPath">Path to the file to write Json file in.</param>
    /// <param name="fileName">Optional custom file name for the item (if not using default).</param>
    /// <param name="compressionMode">The compression mode to use for the files.</param>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2091", Justification = "Caused by compiler generated code already covered by our annotations.")]
#endif
    public static async Task ToDirectoryAsync<T>(this T serializable, string folderPath, string? fileName = null, JsonCompression compressionMode = JsonCompression.None) where T : IJsonSerializable, new()
    {
        var fName = Path.Combine(folderPath, fileName ?? serializable.GetDefaultFileName());
        await ToJsonAsync(serializable, JsonCompressionExtensions.GetCompressedFileName(fName, compressionMode), compressionMode);
    }

    /// <summary>
    /// True if can read a serialized file from a directory, else false.
    /// </summary>
    /// <param name="serializable">The serializable item.</param>
    /// <param name="directory">The directory possibly containing metadata.</param>
    /// <param name="fileName">Optional custom file name to use for the file.</param>
    /// <param name="compressionMode">The compression mode used.</param>
    /// <param name="newFilePath">File path of the file to be read.</param>
    public static bool CanReadFromDirectory<T>(this T serializable, string directory, string? fileName, out JsonCompression compressionMode, out string newFilePath) where T : IJsonSerializable, new()
    {
        var metaDataPath      = GetMetadataPath(serializable, directory, fileName);
        var possibleFilePaths = JsonCompressionExtensions.GetPossibleFilePaths(metaDataPath);

        foreach (var filePath in possibleFilePaths)
        {
            if (!File.Exists(filePath)) 
                continue;

            newFilePath = filePath;
            compressionMode = JsonCompressionExtensions.GetCompressionFromFileName(filePath);
            return true;
        }

        newFilePath = "";
        compressionMode = JsonCompression.None;
        return false;
    }

    internal static string GetMetadataPath<T>(this T serializable, string directory, string? fileName = null) where T : IJsonSerializable, new()
    {
        return Path.Combine(directory, fileName ?? serializable.GetDefaultFileName());
    }
}

