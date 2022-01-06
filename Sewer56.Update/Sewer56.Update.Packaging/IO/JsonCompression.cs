using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Sewer56.Update.Packaging.IO;

/// <summary>
/// Constants related to JSON Compression.
/// </summary>
public static class JsonCompressionExtensions
{
    /// <summary>
    /// Map from individual JSON compression methods to extension strings.
    /// </summary>
    public static readonly Dictionary<JsonCompression, string> CompressionToExtensionMap = new()
    {
        { JsonCompression.None, "" },
        { JsonCompression.Brotli, ".br" }
    };
    /// <summary>
    /// Map from individual extension strings to compression methods.
    /// </summary>
    public static readonly Dictionary<string, JsonCompression> ExtensionToCompressionMap = new();

    static JsonCompressionExtensions()
    {
        foreach (var keyValuePair in CompressionToExtensionMap)
            ExtensionToCompressionMap[keyValuePair.Value] = keyValuePair.Key;
    }

    /// <summary>
    /// Gets the compression scheme used from a file name or path.
    /// </summary>
    /// <param name="fileName">Original file name with compression extension.</param>
    /// <returns>The compression scheme used based on extension.</returns>
    public static JsonCompression GetCompressionFromFileName(string fileName)
    {
        if (ExtensionToCompressionMap.TryGetValue(Path.GetExtension(fileName), out var value))
            return value;

        return JsonCompression.None;
    }

    /// <summary>
    /// Returns a stream for a given decompression scheme to allow for streaming of the data.
    /// </summary>
    /// <param name="outputStream">The stream source the decompressed data from.</param>
    /// <param name="compression">The decompression to apply.</param>
    public static Stream GetStreamForDecompression(Stream outputStream, JsonCompression compression)
    {
        switch (compression)
        {
            case JsonCompression.None:
                return outputStream;
            case JsonCompression.Brotli:
                return new BrotliStream(outputStream, CompressionMode.Decompress);
            default:
                throw new ArgumentOutOfRangeException(nameof(compression), compression, null);
        }
    }

    /// <summary>
    /// Returns a stream for a given compression scheme to allow for streaming of the data.,
    /// </summary>
    /// <param name="outputStream">The stream to output the compressed data to.</param>
    /// <param name="compression">The compression to apply.</param>
    public static Stream GetStreamForCompression(Stream outputStream, JsonCompression compression)
    {
        switch (compression)
        {
            case JsonCompression.None:
                return outputStream;
            case JsonCompression.Brotli:
                return new BrotliStream(outputStream, CompressionLevel.Optimal);
            default:
                throw new ArgumentOutOfRangeException(nameof(compression), compression, null);
        }
    }

    /// <summary>
    /// Gets the compressed file name of a file by appending the relevant extension.
    /// </summary>
    /// <param name="fileName">Original file name without compression extension.</param>
    /// <param name="compression">The compression to apply.</param>
    /// <returns>File name with custom extension.</returns>
    public static string GetCompressedFileName(string fileName, JsonCompression compression)
    {
        return $"{fileName}{CompressionToExtensionMap[compression]}";
    }

    /// <summary>
    /// Returns a list of all possible file paths (standard + compression extensions) for a given file path.
    /// </summary>
    /// <param name="filePath">Path or file name.</param>
    public static List<string> GetPossibleFilePaths(string filePath)
    {
        var result = new List<string>();
        foreach (var key in CompressionToExtensionMap.Keys)
            result.Add(GetCompressedFileName(filePath, key));

        return result;
    }
}

/// <summary>
/// The compression mode used on the JSON file.
/// </summary>
public enum JsonCompression
{
    /// <summary>
    /// No compression is used.
    /// </summary>
    None,

    /// <summary>
    /// Uses max level brotli compression.
    /// </summary>
    Brotli
}