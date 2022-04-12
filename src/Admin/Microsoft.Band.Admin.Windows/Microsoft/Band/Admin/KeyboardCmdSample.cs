using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct KeyboardCmdSample
{
    internal const int MAX_NUM_OF_CANDIDATES = 4;

    internal const int MAX_KBDCMD_DATA_LEN = 400;

    internal KeyboardMessageType KeyboardMsgType;

    internal byte NumOfCandidates;

    internal byte WordIndex;

    internal uint DataLength;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 400)]
    internal byte[] Datafield;
}
