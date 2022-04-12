// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CloudApplicationSettings
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin
{
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
}
