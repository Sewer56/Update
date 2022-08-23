using System.Threading;
using System.Threading.Tasks;
using Sewer56.Update.Packaging.Structures;

namespace Sewer56.Update.Interfaces.Extensions;

/// <summary>
/// Extension for package resolvers that allows for (latest) release metadata to be obtained.
/// </summary>
public interface IPackageResolverGetLatestReleaseMetadata
{
    /// <summary>
    /// Obtains the release metadata for this package.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <remarks>Uses the highest version of package. Usually all releases are contained inside a single metadata file, but in some cases where a source may have its own releases mechanism (e.g. GitHub), users may choose to use the integrated release system. As such, this function returns the release metadata for the latest package.</remarks>
    public ValueTask<ReleaseMetadata?> GetReleaseMetadataAsync(CancellationToken token);
}