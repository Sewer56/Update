using System;
using System.IO;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace Sewer56.Update.Resolvers.GameBanana;

/// <summary>
/// Sanitizes any file name to be used with GameBanana.
/// </summary>
public static class GameBananaUtilities
{
    private static Regex GbFileNameRegex = new Regex("[^a-z0-9_-]");

    /// <summary>
    /// Maximum file name length accepted by GameBanana.
    /// </summary>
    public const int MaxFileNameLength = 100;

    /// <summary>
    /// Maximum file name length GameBanana accepts without editing the file name.
    /// </summary>
    public const int MaxUnmodifiedFileNameLength = MaxFileNameLength - 6; // _xxxxx

    /// <summary>
    /// Removes characters stripped out by GameBanana's file name filter.
    /// </summary>
    /// <param name="fileName">The file name to be used with GameBanana.</param>
    /// <returns>The result file name.</returns>
    public static string SanitizeFileName(string fileName) => SanitizeFileName(fileName, true);

    /// <summary>
    /// Removes characters stripped out by GameBanana's file name filter.
    /// </summary>
    /// <param name="fileName">The file name to be used with GameBanana.</param>
    /// <param name="replaceDot">True to replace the dot character (current), else false (legacy).</param>
    /// <returns>The result file name.</returns>
    public static string SanitizeFileName(string fileName, bool replaceDot)
    {
        // Replace . with _ for the sake of semantic versions with numbers above >9
        if (replaceDot)
            fileName = fileName.Replace('.', '_');

        var noExtension = Path.GetFileNameWithoutExtension(fileName).ToLower();
        var extension   = Path.GetExtension(fileName);
        var replaced    = GbFileNameRegex.Replace(noExtension, "");

        var excessChars = (replaced.Length + extension.Length) - MaxUnmodifiedFileNameLength;
        if (excessChars <= 0)
            return replaced + extension;

        return replaced.Substring(excessChars, (MaxUnmodifiedFileNameLength - extension.Length)) + extension;
    }

    /// <summary>
    /// Gets the part of the string that GameBanana wouldn't edit out of the file name post upload.
    /// (Sanitizes and trims the string if necessary).
    /// </summary>
    /// <param name="fileName">The file name to be used with GameBanana.</param>
    /// <returns>The result file name.</returns>
    public static string[] GetFileNameStarts(string fileName)
    {
        return new[]
        {
            GetFileNameStart_Internal(fileName, false),
            GetFileNameStart_Internal(fileName, true),
        };
    }

    /// <summary>
    /// Gets the part of the string that GameBanana wouldn't edit out of the file name post upload.
    /// (Sanitizes and trims the string if necessary).
    /// </summary>
    /// <param name="fileName">The file name to be used with GameBanana.</param>
    /// <param name="replaceDot">True to replace the dot, else false.</param>
    /// <returns>The result file name.</returns>
    internal static string GetFileNameStart_Internal(string fileName, bool replaceDot)
    {
        var sanitized = SanitizeFileName(fileName, replaceDot);
        var noExtensionChars = Path.GetFileNameWithoutExtension(sanitized);
        var extension = Path.GetExtension(sanitized);

        var excessChars = (noExtensionChars.Length + extension.Length) - MaxUnmodifiedFileNameLength;
        if (excessChars <= 0)
            return noExtensionChars;

        return noExtensionChars.Substring(0, MaxUnmodifiedFileNameLength - excessChars);
    }
}