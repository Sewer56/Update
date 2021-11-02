using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Exceptions;

namespace Sewer56.Update.Packaging.Structures.ReleaseBuilder;

/// <summary>
/// Used to add an existing package to <see cref="ReleaseBuilder{T}"/>
/// </summary>
public class ExistingPackageBuilderItem
{
    /// <summary>
    /// The folder where the current existing package is located.
    /// If it is a folder, the builder assumes it's unpacked, else it assumes it is packed if it is a file.
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary />
    public void Validate()
    {
        ThrowHelpers.ThrowIfNullOrEmpty(Path, () => new BuilderValidationFailedException($"Builder Item Should specify a folder path ({nameof(ExistingPackageBuilderItem)}.{nameof(Path)}) for the package, but does not."));
    }
}