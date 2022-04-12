using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct ButtonEvent
{
    internal ButtonType Button;

    internal byte ButtonEventDown;
}
