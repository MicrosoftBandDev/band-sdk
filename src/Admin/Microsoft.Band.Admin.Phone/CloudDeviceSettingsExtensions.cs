// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CloudDeviceSettingsExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Band.Admin
{
  internal static class CloudDeviceSettingsExtensions
  {
    internal static DeviceSettings ToDeviceSettings(
      this CloudDeviceSettings cloudDeviceSettings)
    {
      DeviceSettings deviceSettings1 = new DeviceSettings();
      if (cloudDeviceSettings == null)
      {
        deviceSettings1.LocaleSettings = new CargoLocaleSettings();
        deviceSettings1.AdditionalSettings = new Dictionary<string, string>();
        return deviceSettings1;
      }
      cloudDeviceSettings.DeviceName = cloudDeviceSettings.DeviceName != null ? cloudDeviceSettings.DeviceName.TruncateTrimDanglingHighSurrogate(16) : string.Empty;
      deviceSettings1.DeviceId = cloudDeviceSettings.DeviceId;
      deviceSettings1.SerialNumber = cloudDeviceSettings.SerialNumber;
      deviceSettings1.DeviceName = cloudDeviceSettings.DeviceName;
      deviceSettings1.ProfileDeviceVersion = cloudDeviceSettings.DeviceProfileVersion;
      deviceSettings1.LocaleSettings = cloudDeviceSettings.LocaleSettings.ToCargoLocaleSettings();
      deviceSettings1.RunDisplayUnits = (RunMeasurementUnitType) cloudDeviceSettings.RunDisplayUnits;
      DeviceSettings deviceSettings2 = deviceSettings1;
      bool? telemetryEnabled = cloudDeviceSettings.TelemetryEnabled;
      int num;
      if (telemetryEnabled.HasValue)
      {
        telemetryEnabled = cloudDeviceSettings.TelemetryEnabled;
        num = telemetryEnabled.Value ? 1 : 0;
      }
      else
        num = 0;
      deviceSettings2.TelemetryEnabled = num != 0;
      deviceSettings1.LastReset = cloudDeviceSettings.LastReset;
      deviceSettings1.LastSuccessfulSync = cloudDeviceSettings.LastSuccessfulSync;
      deviceSettings1.AdditionalSettings = cloudDeviceSettings.AdditionalSettings;
      if (cloudDeviceSettings.FirmwareReserved != null)
        deviceSettings1.Reserved = Convert.FromBase64String(cloudDeviceSettings.FirmwareReserved);
      if (cloudDeviceSettings.FirmwareByteArray != null)
        deviceSettings1.FirmwareByteArray = Convert.FromBase64String(cloudDeviceSettings.FirmwareByteArray);
      return deviceSettings1;
    }

    internal static IDictionary<Guid, DeviceSettings> ToAllDeviceSettings(
      this IDictionary<Guid, CloudDeviceSettings> allCloudDeviceSettings)
    {
      return allCloudDeviceSettings == null || allCloudDeviceSettings.Count == 0 || allCloudDeviceSettings.First<KeyValuePair<Guid, CloudDeviceSettings>>().Value == null ? (IDictionary<Guid, DeviceSettings>) new Dictionary<Guid, DeviceSettings>() : (IDictionary<Guid, DeviceSettings>) allCloudDeviceSettings.ToDictionary<KeyValuePair<Guid, CloudDeviceSettings>, Guid, DeviceSettings>((Func<KeyValuePair<Guid, CloudDeviceSettings>, Guid>) (p => p.Key), (Func<KeyValuePair<Guid, CloudDeviceSettings>, DeviceSettings>) (p => p.Value.ToDeviceSettings()));
    }
  }
}
