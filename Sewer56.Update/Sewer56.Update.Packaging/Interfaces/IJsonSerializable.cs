using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Structures;

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
    public static async Task ToJsonAsync<T>(this T serializable, string filePath) where T : IJsonSerializable, new()
    {
        await using var fileStream = File.Open(filePath, FileMode.Create);
        await JsonSerializer.SerializeAsync(fileStream, serializable);
    }

    /// <summary>
    /// Reads the current item from a Json Directory.
    /// </summary>
    /// <param name="serializable">The "this" instance.</param>
    /// <param name="directory">The directory to read item from.</param>
    /// <param name="fileName">Optional custom file name for the item (if not using default).</param>
    /// <param name="token">Allows for cancelling the task.</param>
    public static async Task<T> ReadFromDirectoryAsync<T>(this T serializable, string directory, string fileName = null, CancellationToken token = default) where T : IJsonSerializable, new()
    {
        return await ReadFromDirectoryAsync<T>(directory, fileName, token);
    }

    /// <summary>
    /// Reads the current item from a Json Directory.
    /// </summary>
    /// <param name="directory">The directory to read item from.</param>
    /// <param name="fileName">Optional custom file name for the item (if not using default).</param>
    /// <param name="token">Allows for cancelling the task.</param>
    public static async Task<T> ReadFromDirectoryAsync<T>(string directory, string? fileName = null, CancellationToken token = default) where T : IJsonSerializable, new()
    {
        var path = Singleton<T>.Instance.GetMetadataPath(directory, fileName);
        await using var fileStream = File.Open(path, FileMode.Open);
        var metadata = await JsonSerializer.DeserializeAsync<T>(fileStream, null, token);
        metadata!.AfterDeserialize(metadata, path);
        return metadata;
    }

    /// <summary>
    /// Reads the current item from a given stream.
    /// </summary>
    /// <param name="serializable">The "this" instance.</param>
    /// <param name="stream">The stream containing the json to deserialize</param>
    public static async Task<T> ReadFromStreamAsync<T>(this T serializable, Stream stream) where T : IJsonSerializable, new()
    {
        return await ReadFromStreamAsync<T>(stream);
    }

    /// <summary>
    /// Reads the current item from raw data.
    /// </summary>
    /// <param name="stream">The stream containing the json to deserialize</param>
    public static async Task<T> ReadFromStreamAsync<T>(Stream stream) where T : IJsonSerializable, new()
    {
        var metadata = await JsonSerializer.DeserializeAsync<T>(stream);
        metadata!.AfterDeserialize(metadata, "");
        return metadata;
    }

    /// <summary>
    /// Reads the current item from raw data.
    /// </summary>
    /// <param name="serializable">The "this" instance.</param>
    /// <param name="data">The data containing the file to deserialize</param>
    public static T ReadFromData<T>(this T serializable, Span<byte> data) where T : IJsonSerializable, new()
    {
        return ReadFromData<T>(data);
    }

    /// <summary>
    /// Reads the current item from raw data.
    /// </summary>
    /// <param name="data">The data containing the file to deserialize</param>
    public static T ReadFromData<T>(Span<byte> data) where T : IJsonSerializable, new()
    {
        var metadata = JsonSerializer.Deserialize<T>(data);
        metadata!.AfterDeserialize(metadata, "");
        return metadata;
    }

    /// <summary>
    /// Writes the current class to a folder.
    /// </summary>
    /// <param name="serializable">The serializable item.</param>
    /// <param name="folderPath">Path to the file to write Json file in.</param>
    /// <param name="fileName">Optional custom file name for the item (if not using default).</param>
    public static async Task ToDirectoryAsync<T>(this T serializable, string folderPath, string? fileName = null) where T : IJsonSerializable, new()
    {
        await ToJsonAsync(serializable, Path.Combine(folderPath, fileName ?? serializable.GetDefaultFileName()));
    }

    /// <summary>
    /// True if can read a serialized file from a directory, else false.
    /// </summary>
    /// <param name="serializable">The serializable item.</param>
    /// <param name="directory">The directory possibly containing metadata.</param>
    public static bool CanReadFromDirectory<T>(this T serializable, string directory) where T : IJsonSerializable, new()
    {
        return File.Exists(GetMetadataPath(serializable, directory));
    }

    internal static string GetMetadataPath<T>(this T serializable, string directory, string? fileName = null) where T : IJsonSerializable, new()
    {
        return Path.Combine(directory, fileName ?? serializable.GetDefaultFileName());
    }
}