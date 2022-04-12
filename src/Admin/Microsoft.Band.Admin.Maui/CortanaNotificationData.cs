using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
internal struct CortanaNotificationData
{
    internal CortanaStatus Status;

    internal ushort StringLengthInBytes;

    internal byte RSVD1;

    internal byte RSVD2;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 320)]
    internal string String;
}
