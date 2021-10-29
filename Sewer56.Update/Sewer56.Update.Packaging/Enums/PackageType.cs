using System;

namespace Sewer56.Update.Packaging.Enums;

/// <summary />
public enum PackageType
{
    /// <summary>
    /// Simply copies files only.
    /// </summary>
    Copy,

    /// <summary>
    /// Vcdiff based delta patching.
    /// </summary>
    Delta,
}

/// <summary/>
public static class PackageTypeExtensions
{
    /// <summary>
    /// Gets the priority of a given package type, with a higher number indicating a higher priority.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Unknown package type.</exception>
    public static int GetPackageTypePriority(this PackageType type)
    {
        return type switch
        {
            PackageType.Copy  => 0,
            PackageType.Delta => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}