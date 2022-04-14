using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct FitnessProfile
{
    internal byte MinHr;

    internal byte MaxHr;

    internal int MaxMET;
}
