using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct Gesture
{
    internal ushort PegMessageType;

    internal short TouchPointX;

    internal short TouchPointY;

    internal short FlickPointX;

    internal short FlickPointY;

    internal short FlickVelocity;

    internal short FlickAcceleration;

    internal uint TimeStart;

    internal uint TimeEnd;
}
