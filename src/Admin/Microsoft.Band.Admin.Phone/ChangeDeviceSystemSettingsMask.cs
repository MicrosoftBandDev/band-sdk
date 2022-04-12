// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.ChangeDeviceSystemSettingsMask
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  [Flags]
  internal enum ChangeDeviceSystemSettingsMask
  {
    SYSSET_CHANGE_NONE = 0,
    SYSSET_CHANGE_BACKLIGHT_VALUE = 1,
    SYSSET_CHANGE_BACKLIGHT_MODE = 2,
    SYSSET_CHANGE_BT_BOOT_STATUS = 4,
    SYSSET_CHANGE_AIRPLANE_MODE = 8,
    SYSSET_CHANGE_DND_MODE = 16, // 0x00000010
    SYSSET_CHANGE_GPS_ENABLED_FOR_RUNS = 32, // 0x00000020
    SYSSET_CHANGE_BIOMETRIC_SENSORS = 64, // 0x00000040
    SYSSET_CHANGE_GLANCE_MODE = 128, // 0x00000080
    SYSSET_CHANGE_MILITARY_TIME = 8192, // 0x00002000
    SYSSET_CHANGE_ALL = 131071, // 0x0001FFFF
  }
}
