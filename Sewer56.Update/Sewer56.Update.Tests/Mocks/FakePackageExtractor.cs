using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sewer56.Update.Interfaces;
using Sewer56.Update.Packaging.Interfaces;

namespace Sewer56.Update.Tests.Mocks;

public class FakePackageExtractor : IPackageExtractor
{
    public Task ExtractPackageAsync(string sourceFilePath, string destDirPath, IProgress<double> progress = null, CancellationToken cancellationToken = default)
    {
        var sourceFileName = Path.GetFileName(sourceFilePath)!;
        File.Copy(sourceFilePath, Path.Combine(destDirPath, sourceFileName));

        return Task.CompletedTask;
    }
}