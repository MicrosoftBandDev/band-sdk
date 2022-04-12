using System;

namespace Microsoft.Band.Admin;

[Flags]
internal enum ChangeDeviceSystemSettingsMask
{
    SYSSET_CHANGE_NONE = 0,
    SYSSET_CHANGE_BACKLIGHT_VALUE = 1,
    SYSSET_CHANGE_BACKLIGHT_MODE = 2,
    SYSSET_CHANGE_BT_BOOT_STATUS = 4,
    SYSSET_CHANGE_AIRPLANE_MODE = 8,
    SYSSET_CHANGE_DND_MODE = 0x10,
    SYSSET_CHANGE_GPS_ENABLED_FOR_RUNS = 0x20,
    SYSSET_CHANGE_BIOMETRIC_SENSORS = 0x40,
    SYSSET_CHANGE_GLANCE_MODE = 0x80,
    SYSSET_CHANGE_MILITARY_TIME = 0x2000,
    SYSSET_CHANGE_ALL = 0x1FFFF
}
