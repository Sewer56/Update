using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Sewer56.Update.Misc;

/// <summary>
/// Allows you to temporarily rent an amount of memory from a shared pool.
/// Use with the `using` statement.
/// </summary>
/// <typeparam name="T"></typeparam>
public struct ArrayRental<T> : IDisposable
{
    /// <summary>
    /// The rented array of data.
    /// </summary>
    public T[] Array { get; }

    /// <summary>
    /// Rents an Array from a shared pool.
    /// </summary>
    /// <param name="minimumLength">Minimum length to rent.</param>
    public ArrayRental(int minimumLength) => Array = ArrayPool<T>.Shared.Rent(minimumLength);

    /// <summary>
    /// Returns the data back to the pool.
    /// </summary>
    public void Dispose() => ArrayPool<T>.Shared.Return(Array);
}