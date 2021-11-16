using System.Runtime.InteropServices;
using Xunit;

namespace Sewer56.Update.Tests.TestUtilities;

/// <summary>
/// An XUnit Replacement for Fact that ignores tests on a specific OS.
/// </summary>
public class WindowsOnlyFact : FactAttribute
{
    public WindowsOnlyFact()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Skip = "This test is only supported on Windows.";
    }
}