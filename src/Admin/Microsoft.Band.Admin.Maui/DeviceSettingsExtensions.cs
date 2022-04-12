using System;

namespace Microsoft.Band.Admin;

internal static class DeviceSettingsExtensions
{
    internal static CloudDeviceSettings ToCloudDeviceSettings(this DeviceSettings deviceSettings, int Version)
    {
        CloudDeviceSettings cloudDeviceSettings = new CloudDeviceSettings();
        cloudDeviceSettings.DeviceId = Guid.Empty;
        cloudDeviceSettings.SerialNumber = null;
        cloudDeviceSettings.DeviceProfileVersion = Version;
        cloudDeviceSettings.DeviceName = deviceSettings.DeviceName;
        cloudDeviceSettings.LocaleSettings = deviceSettings.LocaleSettings.ToCloudLocaleSettings();
        cloudDeviceSettings.LastReset = deviceSettings.LastReset;
        cloudDeviceSettings.LastSuccessfulSync = deviceSettings.LastSuccessfulSync;
        cloudDeviceSettings.TelemetryEnabled = deviceSettings.TelemetryEnabled;
        cloudDeviceSettings.RunDisplayUnits = Convert.ToByte(deviceSettings.RunDisplayUnits);
        cloudDeviceSettings.AdditionalSettings = deviceSettings.AdditionalSettings;
        if (deviceSettings.FirmwareByteArray != null && deviceSettings.FirmwareByteArray.Length != 0)
        {
            cloudDeviceSettings.FirmwareByteArray = Convert.ToBase64String(deviceSettings.FirmwareByteArray);
        }
        if (deviceSettings.Reserved != null && deviceSettings.Reserved.Length != 0)
        {
            cloudDeviceSettings.FirmwareReserved = Convert.ToBase64String(deviceSettings.Reserved);
        }
        return cloudDeviceSettings;
    }
}
