using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Sewer56.Update.Misc;

/// <summary />
public static class StringExtensions
{
    /// <summary />
    public static string ToBase64(this string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    /// <summary />
    public static string FromBase64(this string text) => Encoding.UTF8.GetString(Convert.FromBase64String(text));

    /// <summary>
    /// Tries to match a string against any available regular expression.
    /// </summary>
    /// <param name="text">The text to perform matching on.</param>
    /// <param name="ignoreRegexes">The regular expressions to try match against.</param>
    public static bool TryMatchAnyRegex(this string text, IEnumerable<Regex>? ignoreRegexes = null)
    {
        if (ignoreRegexes == null)
            return false;

        foreach (var regex in ignoreRegexes)
        {
            if (regex.IsMatch(text))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Sanitizes a file name such that it can be written to a file.
    /// </summary>
    public static string SanitizeFileName(this string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName.Where(x => !invalidChars.Contains(x)).ToArray());
    }
}