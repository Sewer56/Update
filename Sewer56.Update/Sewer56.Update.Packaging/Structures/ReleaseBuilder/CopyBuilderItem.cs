using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Exceptions;
using Sewer56.Update.Packaging.Interfaces;

namespace Sewer56.Update.Packaging.Structures.ReleaseBuilder;

/// <summary />
public class CopyBuilderItem<T> : BaseBuilderItem<T> where T : class
{
    /// <summary>
    /// Where the current version of the item is located.
    /// </summary>
    public string FolderPath { get; set; } = "";

    /// <summary />
    public override void Validate()
    {
        base.Validate();
        ThrowHelpers.ThrowIfNullOrEmpty(FolderPath, () => new BuilderValidationFailedException($"Builder Item Should specify a folder path ({nameof(CopyBuilderItem<T>)}.{nameof(FolderPath)}) for the package, but does not."));
    }
}