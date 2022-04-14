using System;

namespace Microsoft.Band.Admin;

internal static class ApplicationSettingsExtensions
{
    internal static CloudApplicationSettings ToCloudApplicationSettings(this ApplicationSettings applicationSettings)
    {
        CloudApplicationSettings cloudApplicationSettings = new CloudApplicationSettings();
        if (applicationSettings.ApplicationId != Guid.Empty)
        {
            cloudApplicationSettings.ApplicationId = applicationSettings.ApplicationId;
        }
        if (applicationSettings.PairedDeviceId != Guid.Empty)
        {
            cloudApplicationSettings.PairedDeviceId = applicationSettings.PairedDeviceId;
        }
        cloudApplicationSettings.Locale = applicationSettings.Locale;
        cloudApplicationSettings.AllowPersonalization = applicationSettings.AllowPersonalization;
        cloudApplicationSettings.AllowToRunInBackground = applicationSettings.AllowToRunInBackground;
        cloudApplicationSettings.SyncInterval = applicationSettings.SyncInterval;
        cloudApplicationSettings.AvatarFileURL = applicationSettings.AvatarFileURL;
        cloudApplicationSettings.HomeScreenWallpaperURL = applicationSettings.HomeScreenWallpaperURL;
        cloudApplicationSettings.ThemeColor = applicationSettings.ThemeColor;
        cloudApplicationSettings.TemperatureDisplayType = applicationSettings.TemperatureDisplayType;
        cloudApplicationSettings.MeasurementDisplayType = applicationSettings.MeasurementDisplayType;
        cloudApplicationSettings.AdditionalSettings = applicationSettings.AdditionalSettings;
        cloudApplicationSettings.ThirdPartyPartnersPortalEndpoint = applicationSettings.ThirdPartyPartnersPortalEndpoint;
        cloudApplicationSettings.PreferredLocale = applicationSettings.PreferredLocale;
        cloudApplicationSettings.PreferredRegion = applicationSettings.PreferredRegion;
        return cloudApplicationSettings;
    }
}
