// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.BatteryGaugeAlertFlags
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  [Flags]
  internal enum BatteryGaugeAlertFlags : ushort
  {
    LowVoltage = 1,
    CriticalVoltage = 2,
    TerminationVoltage = 4,
    WirelessFWUpdateAllowed = 8,
    MotorNotAllowed = 16, // 0x0010
    SampleNotAvailable = 32, // 0x0020
  }
}
