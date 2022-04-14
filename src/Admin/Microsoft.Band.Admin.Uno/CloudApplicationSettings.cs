using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class CloudApplicationSettings
{
    [DataMember(EmitDefaultValue = false)]
    internal string PreferredLocale;

    [DataMember(EmitDefaultValue = false)]
    internal string PreferredRegion;

    [DataMember]
    internal Guid? ApplicationId { get; set; }

    [DataMember]
    internal Guid? PairedDeviceId { get; set; }

    [DataMember]
    internal string Locale { get; set; }

    [DataMember]
    internal bool? AllowPersonalization { get; set; }

    [DataMember]
    internal bool? AllowToRunInBackground { get; set; }

    [DataMember]
    internal TimeSpan? SyncInterval { get; set; }

    [DataMember]
    internal string AvatarFileURL { get; set; }

    [DataMember]
    internal string HomeScreenWallpaperURL { get; set; }

    [DataMember]
    internal ApplicationThemeColor? ThemeColor { get; set; }

    [DataMember]
    internal TemperatureUnitType? TemperatureDisplayType { get; set; }

    [DataMember]
    internal MeasurementUnitType? MeasurementDisplayType { get; set; }

    [DataMember]
    internal string ThirdPartyPartnersPortalEndpoint { get; set; }

    [DataMember]
    internal IDictionary<string, string> AdditionalSettings { get; set; }
}
