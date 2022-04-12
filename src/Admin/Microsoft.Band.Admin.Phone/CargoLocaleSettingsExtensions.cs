// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoLocaleSettingsExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  internal static class CargoLocaleSettingsExtensions
  {
    public static CloudLocaleSettings ToCloudLocaleSettings(
      this CargoLocaleSettings cargoLocaleSettings)
    {
      return new CloudLocaleSettings()
      {
        DateFormat = Convert.ToByte((object) cargoLocaleSettings.DateFormat),
        DateSeparator = cargoLocaleSettings.DateSeparator,
        DecimalSeparator = cargoLocaleSettings.DecimalSeparator,
        DistanceLongUnits = Convert.ToByte((object) cargoLocaleSettings.DistanceLongUnits),
        DistanceShortUnits = Convert.ToByte((object) cargoLocaleSettings.DistanceShortUnits),
        EnergyUnits = Convert.ToByte((object) cargoLocaleSettings.EnergyUnits),
        MassUnits = Convert.ToByte((object) cargoLocaleSettings.MassUnits),
        TemperatureUnits = Convert.ToByte((object) cargoLocaleSettings.TemperatureUnits),
        VolumeUnits = Convert.ToByte((object) cargoLocaleSettings.VolumeUnits),
        Language = (ushort) cargoLocaleSettings.Language,
        LocaleId = (ushort) cargoLocaleSettings.LocaleId,
        LocaleName = cargoLocaleSettings.LocaleName,
        NumberSeparator = cargoLocaleSettings.NumberSeparator,
        TimeFormat = cargoLocaleSettings.TimeFormat.ToTimeFormatByte()
      };
    }

    private static byte ToTimeFormatByte(this DisplayTimeFormat format)
    {
      switch (format)
      {
        case DisplayTimeFormat.HHmmss:
          return 1;
        case DisplayTimeFormat.Hmmss:
          return 2;
        case DisplayTimeFormat.hhmmss:
          return 3;
        case DisplayTimeFormat.hmmss:
          return 4;
        default:
          return 0;
      }
    }
  }
}
