using System;

namespace Microsoft.Band.Admin;

internal static class FirmwareAppExtensions
{
    internal static RunningAppType ToRunningAppType(this FirmwareApp firmwareApp)
    {
        return firmwareApp switch
        {
            FirmwareApp.OneBL => RunningAppType.OneBL, 
            FirmwareApp.TwoUp => RunningAppType.TwoUp, 
            FirmwareApp.App => RunningAppType.App, 
            FirmwareApp.UpApp => RunningAppType.UpApp, 
            FirmwareApp.Invalid => RunningAppType.Invalid, 
            _ => throw new ArgumentException("Unknown FirmwareApp value."), 
        };
    }
}
