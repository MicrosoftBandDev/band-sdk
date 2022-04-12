// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CloudApplicationSettingsExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  internal static class CloudApplicationSettingsExtensions
  {
    internal static ApplicationSettings ToApplicationSettings(
      this CloudApplicationSettings cloudApplicationSettings)
    {
      ApplicationSettings applicationSettings = new ApplicationSettings();
      Guid? nullable = cloudApplicationSettings.ApplicationId;
      applicationSettings.ApplicationId = nullable ?? Guid.Empty;
      nullable = cloudApplicationSettings.PairedDeviceId;
      applicationSettings.PairedDeviceId = nullable ?? Guid.Empty;
      applicationSettings.Locale = cloudApplicationSettings.Locale;
      applicationSettings.AllowPersonalization = cloudApplicationSettings.AllowPersonalization;
      applicationSettings.AllowToRunInBackground = cloudApplicationSettings.AllowToRunInBackground;
      applicationSettings.SyncInterval = cloudApplicationSettings.SyncInterval;
      applicationSettings.AvatarFileURL = cloudApplicationSettings.AvatarFileURL;
      applicationSettings.HomeScreenWallpaperURL = cloudApplicationSettings.HomeScreenWallpaperURL;
      applicationSettings.ThemeColor = cloudApplicationSettings.ThemeColor;
      applicationSettings.TemperatureDisplayType = cloudApplicationSettings.TemperatureDisplayType;
      applicationSettings.MeasurementDisplayType = cloudApplicationSettings.MeasurementDisplayType;
      applicationSettings.AdditionalSettings = cloudApplicationSettings.AdditionalSettings;
      applicationSettings.ThirdPartyPartnersPortalEndpoint = cloudApplicationSettings.ThirdPartyPartnersPortalEndpoint;
      applicationSettings.PreferredLocale = cloudApplicationSettings.PreferredLocale;
      applicationSettings.PreferredRegion = cloudApplicationSettings.PreferredRegion;
      return applicationSettings;
    }
  }
}
