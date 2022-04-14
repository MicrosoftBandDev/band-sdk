using System;

namespace Microsoft.Band.Admin;

internal static class CloudApplicationSettingsExtensions
{
    internal static ApplicationSettings ToApplicationSettings(this CloudApplicationSettings cloudApplicationSettings)
    {
        return new ApplicationSettings
        {
            ApplicationId = (cloudApplicationSettings.ApplicationId ?? Guid.Empty),
            PairedDeviceId = (cloudApplicationSettings.PairedDeviceId ?? Guid.Empty),
            Locale = cloudApplicationSettings.Locale,
            AllowPersonalization = cloudApplicationSettings.AllowPersonalization,
            AllowToRunInBackground = cloudApplicationSettings.AllowToRunInBackground,
            SyncInterval = cloudApplicationSettings.SyncInterval,
            AvatarFileURL = cloudApplicationSettings.AvatarFileURL,
            HomeScreenWallpaperURL = cloudApplicationSettings.HomeScreenWallpaperURL,
            ThemeColor = cloudApplicationSettings.ThemeColor,
            TemperatureDisplayType = cloudApplicationSettings.TemperatureDisplayType,
            MeasurementDisplayType = cloudApplicationSettings.MeasurementDisplayType,
            AdditionalSettings = cloudApplicationSettings.AdditionalSettings,
            ThirdPartyPartnersPortalEndpoint = cloudApplicationSettings.ThirdPartyPartnersPortalEndpoint,
            PreferredLocale = cloudApplicationSettings.PreferredLocale,
            PreferredRegion = cloudApplicationSettings.PreferredRegion
        };
    }
}
