using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Band.Admin;

internal static class CloudDeviceSettingsExtensions
{
    internal static DeviceSettings ToDeviceSettings(this CloudDeviceSettings cloudDeviceSettings)
    {
        DeviceSettings deviceSettings = new DeviceSettings();
        if (cloudDeviceSettings == null)
        {
            deviceSettings.LocaleSettings = new CargoLocaleSettings();
            deviceSettings.AdditionalSettings = new Dictionary<string, string>();
            return deviceSettings;
        }
        if (cloudDeviceSettings.DeviceName == null)
        {
            cloudDeviceSettings.DeviceName = string.Empty;
        }
        else
        {
            cloudDeviceSettings.DeviceName = cloudDeviceSettings.DeviceName.TruncateTrimDanglingHighSurrogate(16);
        }
        deviceSettings.DeviceId = cloudDeviceSettings.DeviceId;
        deviceSettings.SerialNumber = cloudDeviceSettings.SerialNumber;
        deviceSettings.DeviceName = cloudDeviceSettings.DeviceName;
        deviceSettings.ProfileDeviceVersion = cloudDeviceSettings.DeviceProfileVersion;
        deviceSettings.LocaleSettings = cloudDeviceSettings.LocaleSettings.ToCargoLocaleSettings();
        deviceSettings.RunDisplayUnits = (RunMeasurementUnitType)cloudDeviceSettings.RunDisplayUnits;
        deviceSettings.TelemetryEnabled = cloudDeviceSettings.TelemetryEnabled.HasValue && cloudDeviceSettings.TelemetryEnabled.Value;
        deviceSettings.LastReset = cloudDeviceSettings.LastReset;
        deviceSettings.LastSuccessfulSync = cloudDeviceSettings.LastSuccessfulSync;
        deviceSettings.AdditionalSettings = cloudDeviceSettings.AdditionalSettings;
        if (cloudDeviceSettings.FirmwareReserved != null)
        {
            deviceSettings.Reserved = Convert.FromBase64String(cloudDeviceSettings.FirmwareReserved);
        }
        if (cloudDeviceSettings.FirmwareByteArray != null)
        {
            deviceSettings.FirmwareByteArray = Convert.FromBase64String(cloudDeviceSettings.FirmwareByteArray);
        }
        return deviceSettings;
    }

    internal static IDictionary<Guid, DeviceSettings> ToAllDeviceSettings(this IDictionary<Guid, CloudDeviceSettings> allCloudDeviceSettings)
    {
        if (allCloudDeviceSettings == null || allCloudDeviceSettings.Count == 0 || allCloudDeviceSettings.First().Value == null)
        {
            return new Dictionary<Guid, DeviceSettings>();
        }
        return allCloudDeviceSettings.ToDictionary((KeyValuePair<Guid, CloudDeviceSettings> p) => p.Key, (KeyValuePair<Guid, CloudDeviceSettings> p) => p.Value.ToDeviceSettings());
    }
}
