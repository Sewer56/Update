using Sewer56.Update.Misc;
using Sewer56.Update.Packaging.Exceptions;
using Sewer56.Update.Packaging.Interfaces;

namespace Sewer56.Update.Packaging.Structures.ReleaseBuilder;

/// <summary />
public class DeltaBuilderItem<T> : BaseBuilderItem<T> where T : class
{
    /// <summary>
    /// Directory of the current version of the item.
    /// </summary>
    public string FolderPath { get; set; } = "";

    /// <summary>
    /// Directory of the previous version of the item.
    /// </summary>
    public string PreviousVersionFolder { get; set; } = "";

    /// <summary>
    /// Version number of the previous version.
    /// </summary>
    public string PreviousVersion { get; set; } = "";

    /// <summary />
    public override void Validate()
    {
        base.Validate();
        ThrowHelpers.ThrowIfNullOrEmpty(FolderPath, () => new BuilderValidationFailedException($"Builder Item Should specify a folder path for the current package ({nameof(DeltaBuilderItem<T>)}.{nameof(FolderPath)}), but does not."));
        ThrowHelpers.ThrowIfNullOrEmpty(PreviousVersion, () => new BuilderValidationFailedException($"Builder Item Should specify a folder path for the previous package ({nameof(DeltaBuilderItem<T>)}.{nameof(PreviousVersionFolder)}), but does not."));
        ThrowHelpers.ThrowIfNullOrEmpty(PreviousVersion, () => new BuilderValidationFailedException($"Builder Item Should specify a version for the previous package ({nameof(CopyBuilderItem<T>)}.{nameof(PreviousVersion)}), but does not."));
    }
}