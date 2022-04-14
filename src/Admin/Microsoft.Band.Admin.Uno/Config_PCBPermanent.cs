using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct Config_PCBPermanent
{
    internal byte PCBID;

    internal byte ConfigurationRecordVersion;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal byte[] SerialNumber;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    internal byte[] BTMACAddress;

    [MarshalAs(UnmanagedType.Bool)]
    internal bool IsPermanentyConfigValid;

    [MarshalAs(UnmanagedType.Bool)]
    internal bool IsLocked;
}
