using System;

namespace Microsoft.Band.Admin;

[Flags]
internal enum BatteryGaugeAlertFlags : ushort
{
    LowVoltage = 1,
    CriticalVoltage = 2,
    TerminationVoltage = 4,
    WirelessFWUpdateAllowed = 8,
    MotorNotAllowed = 0x10,
    SampleNotAvailable = 0x20
}
