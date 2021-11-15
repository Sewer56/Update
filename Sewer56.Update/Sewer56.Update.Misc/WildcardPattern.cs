using System.Text.RegularExpressions;

namespace Sewer56.Update.Misc;

/// <summary>
/// Helper class for searching for file name wildcards.
/// Directly taken from Onova.
/// </summary>
public class WildcardPattern
{
    /// <summary>
    /// Finds a file name pattern in a given input string.
    /// </summary>
    public static bool IsMatch(string input, string pattern)
    {
        pattern = Regex.Escape(pattern);
        pattern = pattern.Replace("\\*", ".*?").Replace("\\?", ".");
        pattern = "^" + pattern + "$";

        return Regex.IsMatch(input, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}