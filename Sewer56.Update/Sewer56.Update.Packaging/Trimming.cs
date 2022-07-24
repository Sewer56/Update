#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sewer56.DeltaPatchGenerator.Lib.Model;

namespace Sewer56.Update.Packaging;

internal class Trimming
{
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    public static void Initialize()
    {

    }

    /// <summary>
    /// Dummy method used for telling the IL Linker to preserve the type in its entirety.
    /// </summary>
    /// <typeparam name="T">Type to preserve all implementation for.</typeparam>
    public static void PreserveMe<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
        T>() { }
}
#endif