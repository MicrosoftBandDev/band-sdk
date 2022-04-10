// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.SyncTasks
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  [Flags]
  internal enum SyncTasks
  {
    None = 0,
    TimeAndTimeZone = 1,
    EphemerisFile = 2,
    TimeZoneFile = 4,
    DeviceCrashDump = 8,
    DeviceInstrumentation = 16, // 0x00000010
    UserProfileFirmwareBytes = 32, // 0x00000020
    UserProfile = 64, // 0x00000040
    SensorLog = 128, // 0x00000080
    WebTiles = 256, // 0x00000100
    WebTilesForced = 512, // 0x00000200
  }
}
