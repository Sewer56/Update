using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sewer56.Update.Misc;

/// <summary>
/// Miscellaneous extensions to help work with streams.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Reads from a given stream into an existing buffer and writes out to the other stream.
    /// </summary>
    /// <param name="source">Where to copy the data from.</param>
    /// <param name="destination">Where to copy the data to.</param>
    /// <param name="buffer">The buffer to use for copying.</param>
    /// <param name="cancellationToken">Allows to cancel the operation.</param>
    public static async Task<int> CopyBufferedToAsync(this Stream source, Stream destination, byte[] buffer, CancellationToken cancellationToken = default)
    {
        var bytesCopied = await source.ReadAsync(buffer, cancellationToken);
        await destination.WriteAsync(buffer, 0, bytesCopied, cancellationToken);
        return bytesCopied;
    }

    /// <summary>
    /// Copies data from a stream to another stream asynchronously, with support for 
    /// </summary>
    /// <param name="source">Where to copy the data from.</param>
    /// <param name="destination">Where to copy the data to.</param>
    /// <param name="bufferSize">Size of chunks used to copy from source to destination.</param>
    /// <param name="progress">Can be used to report current copying progress.</param>
    /// <param name="contentLength">Length of content to be downloaded. Provide this if source stream doesn't support length property but it is known.</param>
    /// <param name="cancellationToken">Can be used to cancel the operation.</param>
    public static async Task CopyToAsyncEx(this Stream source, Stream destination, int bufferSize = 262144, IProgress<double>? progress = null, long? contentLength = null, CancellationToken cancellationToken = default)
    {
        using var buffer = new ArrayRental<byte>(bufferSize);
        var totalBytesCopied = 0L;
        int bytesCopied;

        bool supportsLength = true;
        long length = 0;

        if (!contentLength.HasValue)
        {
            try
            {
                length = source.Length;
                if (length == 0)
                    length = 1; // just in case.
            }
            catch (Exception e) { supportsLength = false; }
        }
        else
        {
            length = contentLength.Value;
        }

        do
        {
            bytesCopied = await source.CopyBufferedToAsync(destination, buffer.Array, cancellationToken);
            totalBytesCopied += bytesCopied;
            if (supportsLength)
                progress?.Report((double)totalBytesCopied / length);
        }
        while (bytesCopied > 0);

        progress?.Report(1.0);
    }

    /// <summary>
    /// Copies data from a stream to another stream asynchronously, with support for progress reporting.
    /// </summary>
    /// <param name="source">Where to copy the data from.</param>
    /// <param name="destination">Where to copy the data to.</param>
    /// <param name="bufferSize">Size of chunks used to copy from source to destination.</param>
    /// <param name="progress">Can be used to report current copying progress.</param>
    /// <param name="cancellationToken">Can be used to cancel the operation.</param>
    public static async Task CopyToAsyncEx(this Stream source, Stream destination, int bufferSize = 262144, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        await CopyToAsyncEx(source, destination, bufferSize, progress, null, cancellationToken);
    }
}