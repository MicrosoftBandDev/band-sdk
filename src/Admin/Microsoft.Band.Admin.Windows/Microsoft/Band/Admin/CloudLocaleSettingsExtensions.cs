using System;

namespace Microsoft.Band.Admin;

internal static class CloudLocaleSettingsExtensions
{
    internal static CargoLocaleSettings ToCargoLocaleSettings(this CloudLocaleSettings? cloudLocaleSettings)
    {
        if (!cloudLocaleSettings.HasValue)
        {
            return CargoLocaleSettings.Default();
        }
        return cloudLocaleSettings.Value.ToCargoLocaleSettings();
    }

    internal static CargoLocaleSettings ToCargoLocaleSettings(this CloudLocaleSettings cloudLocaleSettings)
    {
        CargoLocaleSettings obj = new CargoLocaleSettings
        {
            LocaleId = (Locale)cloudLocaleSettings.LocaleId,
            LocaleName = cloudLocaleSettings.LocaleName,
            Language = (LocaleLanguage)cloudLocaleSettings.Language
        };
        obj.DistanceLongUnits = (DistanceUnitType)ConvertEnumFromCloud(obj.DistanceLongUnits.GetType(), cloudLocaleSettings.DistanceLongUnits);
        obj.DistanceShortUnits = (DistanceUnitType)ConvertEnumFromCloud(obj.DistanceShortUnits.GetType(), cloudLocaleSettings.DistanceShortUnits);
        obj.EnergyUnits = (EnergyUnitType)ConvertEnumFromCloud(obj.EnergyUnits.GetType(), cloudLocaleSettings.EnergyUnits);
        obj.MassUnits = (MassUnitType)ConvertEnumFromCloud(obj.MassUnits.GetType(), cloudLocaleSettings.MassUnits);
        obj.TemperatureUnits = (TemperatureUnitType)ConvertEnumFromCloud(obj.TemperatureUnits.GetType(), cloudLocaleSettings.TemperatureUnits);
        obj.VolumeUnits = (VolumeUnitType)ConvertEnumFromCloud(obj.VolumeUnits.GetType(), cloudLocaleSettings.VolumeUnits);
        obj.TimeFormat = (DisplayTimeFormat)cloudLocaleSettings.TimeFormat;
        obj.DateFormat = (DisplayDateFormat)cloudLocaleSettings.DateFormat;
        obj.DateSeparator = cloudLocaleSettings.DateSeparator;
        obj.NumberSeparator = cloudLocaleSettings.NumberSeparator;
        obj.DecimalSeparator = cloudLocaleSettings.DecimalSeparator;
        return obj;
    }

    private static int ConvertEnumFromCloud(Type enumType, byte enumValue)
    {
        int num = Convert.ToInt32(enumValue);
        if (!Enum.IsDefined(enumType, num))
        {
            Array values = Enum.GetValues(enumType);
            if (values.Length > 0)
            {
                num = Convert.ToInt32(values.GetValue(new int[1]));
            }
        }
        return num;
    }
}
