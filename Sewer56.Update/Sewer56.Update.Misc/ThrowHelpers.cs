﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Sewer56.Update.Misc;

/// <summary>
/// Helper methods for throwing exceptions.
/// </summary>
public static class ThrowHelpers
{
    /// <summary>
    /// Throws an exception if the string is null or empty.
    /// </summary>
    public static void ThrowIfNullOrEmpty<T>(string text, Func<T> makeException) where T : System.Exception
    {
        if (string.IsNullOrEmpty(text))
            throw makeException();
    }
}