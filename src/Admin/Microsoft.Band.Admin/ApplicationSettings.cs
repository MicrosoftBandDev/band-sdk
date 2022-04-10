// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.ApplicationSettings
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;

namespace Microsoft.Band.Admin
{
  public sealed class ApplicationSettings
  {
    public Guid ApplicationId { get; set; }

    public Guid PairedDeviceId { get; set; }

    public string Locale { get; set; }

    public bool? AllowPersonalization { get; set; }

    public bool? AllowToRunInBackground { get; set; }

    public TimeSpan? SyncInterval { get; set; }

    public string AvatarFileURL { get; set; }

    public string HomeScreenWallpaperURL { get; set; }

    public ApplicationThemeColor? ThemeColor { get; set; }

    public TemperatureUnitType? TemperatureDisplayType { get; set; }

    public MeasurementUnitType? MeasurementDisplayType { get; set; }

    public IDictionary<string, string> AdditionalSettings { get; set; }

    public string ThirdPartyPartnersPortalEndpoint { get; set; }

    public string PreferredLocale { get; set; }

    public string PreferredRegion { get; set; }
  }
}
