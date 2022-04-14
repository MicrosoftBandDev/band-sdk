using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct Recovery
{
    internal ushort RecoveryTime;

    internal FitnessProfile FitnessProfile;
}
