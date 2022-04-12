using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

internal struct FitnessPlanWorkout
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal byte[] Id;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
    internal byte[] Name;

    internal byte NameLength;

    internal ushort CircuitCount;

    internal CompletionType CompletionType;

    internal uint CompletionValue;
}
