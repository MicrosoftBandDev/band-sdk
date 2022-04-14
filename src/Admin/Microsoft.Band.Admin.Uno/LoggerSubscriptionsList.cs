using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct LoggerSubscriptionsList
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    internal byte[] ActiveSubscriptions;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    internal byte[] PassiveSubscritpions;
}
