// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoLocaleSettings
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin
{
    public sealed class CargoLocaleSettings
    {
        private const LocaleLanguage DefaultLanguage = LocaleLanguage.English_US;
        private const int DeserializeDeviceMasteredFieldsFromBand_FastForward1 = 14;
        private const int DeserializeDeviceMasteredFieldsFromBand_FastForward2 = 6;
        private const int DeserializeDeviceMasteredFieldsFromBand_FastForward3 = 6;

        public static CargoLocaleSettings Default() => new CargoLocaleSettings()
        {
            DateFormat = DisplayDateFormat.Mdyyyy,
            DateSeparator = '/',
            DecimalSeparator = '.',
            DistanceLongUnits = DistanceUnitType.Imperial,
            DistanceShortUnits = DistanceUnitType.Imperial,
            EnergyUnits = EnergyUnitType.Imperial,
            MassUnits = MassUnitType.Imperial,
            TemperatureUnits = TemperatureUnitType.Imperial,
            VolumeUnits = VolumeUnitType.Imperial,
            Language = LocaleLanguage.English_US,
            LocaleName = LocaleLanguage.English_US.ToLanguageCultureName(),
            LocaleId = Locale.UnitedStates,
            NumberSeparator = ',',
            TimeFormat = DisplayTimeFormat.hmmss
        };

        public string LocaleName { get; set; }

        public Locale LocaleId { get; set; }

        public LocaleLanguage Language { get; set; }

        public DistanceUnitType DistanceShortUnits { get; set; }

        public DistanceUnitType DistanceLongUnits { get; set; }

        public MassUnitType MassUnits { get; set; }

        public VolumeUnitType VolumeUnits { get; set; }

        public EnergyUnitType EnergyUnits { get; set; }

        public TemperatureUnitType TemperatureUnits { get; set; }

        public DisplayTimeFormat TimeFormat { get; set; }

        public DisplayDateFormat DateFormat { get; set; }

        public char DateSeparator { get; set; }

        public char NumberSeparator { get; set; }

        public char DecimalSeparator { get; set; }

        internal static CargoLocaleSettings DeserializeFromBand(ICargoReader reader) => new CargoLocaleSettings()
        {
            LocaleName = reader.ReadString(6),
            LocaleId = (Locale)reader.ReadInt16(),
            Language = (LocaleLanguage)reader.ReadInt16(),
            DateSeparator = (char)reader.ReadUInt16(),
            NumberSeparator = (char)reader.ReadUInt16(),
            DecimalSeparator = (char)reader.ReadUInt16(),
            TimeFormat = CargoLocaleSettings.ToDisplayTimeFormat(reader.ReadByte()),
            DateFormat = (DisplayDateFormat)reader.ReadByte(),
            DistanceShortUnits = (DistanceUnitType)reader.ReadByte(),
            DistanceLongUnits = (DistanceUnitType)reader.ReadByte(),
            MassUnits = (MassUnitType)reader.ReadByte(),
            VolumeUnits = (VolumeUnitType)reader.ReadByte(),
            EnergyUnits = (EnergyUnitType)reader.ReadByte(),
            TemperatureUnits = (TemperatureUnitType)reader.ReadByte()
        };

        internal void DeserializeDeviceMasteredFieldsFromBand(ICargoReader reader, bool forExplicitSave)
        {
            if (forExplicitSave)
            {
                reader.ReadExactAndDiscard(14);
            }
            else
            {
                this.LocaleName = reader.ReadString(6);
                this.LocaleId = (Locale)reader.ReadInt16();
            }
            this.Language = (LocaleLanguage)reader.ReadInt16();
            reader.ReadExactAndDiscard(6);
            if (forExplicitSave)
                reader.ReadExactAndDiscard(1);
            else
                this.TimeFormat = CargoLocaleSettings.ToDisplayTimeFormat(reader.ReadByte());
            if (forExplicitSave)
                reader.ReadExactAndDiscard(1);
            else
                this.DateFormat = (DisplayDateFormat)reader.ReadByte();
            reader.ReadExactAndDiscard(6);
        }

        internal void SerializeToBand(ICargoWriter writer)
        {
            writer.WriteStringWithPadding(this.LocaleName, 6);
            writer.WriteInt16((short)this.LocaleId);
            writer.WriteInt16((short)this.Language);
            writer.WriteUInt16((ushort)this.DateSeparator);
            writer.WriteUInt16((ushort)this.NumberSeparator);
            writer.WriteUInt16((ushort)this.DecimalSeparator);
            writer.WriteByte(CargoLocaleSettings.ToByte(this.TimeFormat));
            writer.WriteByte((byte)this.DateFormat);
            writer.WriteByte((byte)this.DistanceShortUnits);
            writer.WriteByte((byte)this.DistanceLongUnits);
            writer.WriteByte((byte)this.MassUnits);
            writer.WriteByte((byte)this.VolumeUnits);
            writer.WriteByte((byte)this.EnergyUnits);
            writer.WriteByte((byte)this.TemperatureUnits);
        }

        private static DisplayTimeFormat ToDisplayTimeFormat(byte format)
        {
            switch (format)
            {
                case 1:
                    return DisplayTimeFormat.HHmmss;
                case 2:
                    return DisplayTimeFormat.Hmmss;
                case 3:
                    return DisplayTimeFormat.hhmmss;
                case 4:
                    return DisplayTimeFormat.hmmss;
                default:
                    return DisplayTimeFormat.Undefined;
            }
        }

        private static byte ToByte(DisplayTimeFormat format)
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
