using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sewer56.Update.Resolvers.NuGet.Utilities;

internal static class NuGetExtensions
{
    internal static IEnumerable<string> RemoveDirectories(this IEnumerable<string> files) => files.Where(x => !x.EndsWith('/') && !x.EndsWith('\\'));
}