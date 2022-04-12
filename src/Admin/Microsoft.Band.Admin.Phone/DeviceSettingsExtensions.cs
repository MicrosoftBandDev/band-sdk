// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.DeviceSettingsExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  internal static class DeviceSettingsExtensions
  {
    internal static CloudDeviceSettings ToCloudDeviceSettings(
      this DeviceSettings deviceSettings,
      int Version)
    {
      CloudDeviceSettings cloudDeviceSettings = new CloudDeviceSettings();
      cloudDeviceSettings.DeviceId = Guid.Empty;
      cloudDeviceSettings.SerialNumber = (string) null;
      cloudDeviceSettings.DeviceProfileVersion = Version;
      cloudDeviceSettings.DeviceName = deviceSettings.DeviceName;
      cloudDeviceSettings.LocaleSettings = new CloudLocaleSettings?(deviceSettings.LocaleSettings.ToCloudLocaleSettings());
      cloudDeviceSettings.LastReset = deviceSettings.LastReset;
      cloudDeviceSettings.LastSuccessfulSync = deviceSettings.LastSuccessfulSync;
      cloudDeviceSettings.TelemetryEnabled = new bool?(deviceSettings.TelemetryEnabled);
      cloudDeviceSettings.RunDisplayUnits = Convert.ToByte((object) deviceSettings.RunDisplayUnits);
      cloudDeviceSettings.AdditionalSettings = deviceSettings.AdditionalSettings;
      if (deviceSettings.FirmwareByteArray != null && deviceSettings.FirmwareByteArray.Length != 0)
        cloudDeviceSettings.FirmwareByteArray = Convert.ToBase64String(deviceSettings.FirmwareByteArray);
      if (deviceSettings.Reserved != null && deviceSettings.Reserved.Length != 0)
        cloudDeviceSettings.FirmwareReserved = Convert.ToBase64String(deviceSettings.Reserved);
      return cloudDeviceSettings;
    }
  }
}
