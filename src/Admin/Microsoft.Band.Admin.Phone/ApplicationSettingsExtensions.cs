// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.ApplicationSettingsExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  internal static class ApplicationSettingsExtensions
  {
    internal static CloudApplicationSettings ToCloudApplicationSettings(
      this ApplicationSettings applicationSettings)
    {
      CloudApplicationSettings applicationSettings1 = new CloudApplicationSettings();
      if (applicationSettings.ApplicationId != Guid.Empty)
        applicationSettings1.ApplicationId = new Guid?(applicationSettings.ApplicationId);
      if (applicationSettings.PairedDeviceId != Guid.Empty)
        applicationSettings1.PairedDeviceId = new Guid?(applicationSettings.PairedDeviceId);
      applicationSettings1.Locale = applicationSettings.Locale;
      applicationSettings1.AllowPersonalization = applicationSettings.AllowPersonalization;
      applicationSettings1.AllowToRunInBackground = applicationSettings.AllowToRunInBackground;
      applicationSettings1.SyncInterval = applicationSettings.SyncInterval;
      applicationSettings1.AvatarFileURL = applicationSettings.AvatarFileURL;
      applicationSettings1.HomeScreenWallpaperURL = applicationSettings.HomeScreenWallpaperURL;
      applicationSettings1.ThemeColor = applicationSettings.ThemeColor;
      applicationSettings1.TemperatureDisplayType = applicationSettings.TemperatureDisplayType;
      applicationSettings1.MeasurementDisplayType = applicationSettings.MeasurementDisplayType;
      applicationSettings1.AdditionalSettings = applicationSettings.AdditionalSettings;
      applicationSettings1.ThirdPartyPartnersPortalEndpoint = applicationSettings.ThirdPartyPartnersPortalEndpoint;
      applicationSettings1.PreferredLocale = applicationSettings.PreferredLocale;
      applicationSettings1.PreferredRegion = applicationSettings.PreferredRegion;
      return applicationSettings1;
    }
  }
}
