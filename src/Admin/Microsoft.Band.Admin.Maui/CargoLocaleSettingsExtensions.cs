using System;

namespace Microsoft.Band.Admin;

internal static class CargoLocaleSettingsExtensions
{
    public static CloudLocaleSettings ToCloudLocaleSettings(this CargoLocaleSettings cargoLocaleSettings)
    {
        CloudLocaleSettings result = default(CloudLocaleSettings);
        result.DateFormat = Convert.ToByte(cargoLocaleSettings.DateFormat);
        result.DateSeparator = cargoLocaleSettings.DateSeparator;
        result.DecimalSeparator = cargoLocaleSettings.DecimalSeparator;
        result.DistanceLongUnits = Convert.ToByte(cargoLocaleSettings.DistanceLongUnits);
        result.DistanceShortUnits = Convert.ToByte(cargoLocaleSettings.DistanceShortUnits);
        result.EnergyUnits = Convert.ToByte(cargoLocaleSettings.EnergyUnits);
        result.MassUnits = Convert.ToByte(cargoLocaleSettings.MassUnits);
        result.TemperatureUnits = Convert.ToByte(cargoLocaleSettings.TemperatureUnits);
        result.VolumeUnits = Convert.ToByte(cargoLocaleSettings.VolumeUnits);
        result.Language = (ushort)cargoLocaleSettings.Language;
        result.LocaleId = (ushort)cargoLocaleSettings.LocaleId;
        result.LocaleName = cargoLocaleSettings.LocaleName;
        result.NumberSeparator = cargoLocaleSettings.NumberSeparator;
        result.TimeFormat = cargoLocaleSettings.TimeFormat.ToTimeFormatByte();
        return result;
    }

    private static byte ToTimeFormatByte(this DisplayTimeFormat format)
    {
        return format switch
        {
            DisplayTimeFormat.HHmmss => 1, 
            DisplayTimeFormat.Hmmss => 2, 
            DisplayTimeFormat.hhmmss => 3, 
            DisplayTimeFormat.hmmss => 4, 
            _ => 0, 
        };
    }
}
