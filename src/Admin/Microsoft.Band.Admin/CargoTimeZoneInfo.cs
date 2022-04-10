// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoTimeZoneInfo
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
    public class CargoTimeZoneInfo
    {
        private string name;
        private static readonly int serializedByteCount = 64 + CargoSystemTime.GetSerializedByteCount() + CargoSystemTime.GetSerializedByteCount();

        public string Name
        {
            get => this.name;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(Name));
                if (value.Length > 30)
                {
                    Logger.Log(LogLevel.Warning, string.Format(BandResources.GenericLengthExceeded, new object[1]
                    {
            (object) nameof (Name)
                    }));
                    this.name = value.Substring(0, 30);
                }
                else
                    this.name = value;
            }
        }

        public short ZoneOffsetMinutes { get; set; }

        public short DaylightOffsetMinutes { get; set; }

        public CargoSystemTime StandardDate { get; set; }

        public CargoSystemTime DaylightDate { get; set; }

        internal static int GetSerializedByteCount() => CargoTimeZoneInfo.serializedByteCount;

        internal static CargoTimeZoneInfo DeserializeFromBand(ICargoReader reader) => new CargoTimeZoneInfo()
        {
            name = reader.ReadString(30),
            ZoneOffsetMinutes = reader.ReadInt16(),
            DaylightOffsetMinutes = reader.ReadInt16(),
            StandardDate = CargoSystemTime.DeserializeFromBand(reader),
            DaylightDate = CargoSystemTime.DeserializeFromBand(reader)
        };

        internal void SerializeToBand(ICargoWriter writer)
        {
            writer.WriteStringWithPadding(this.Name ?? "", 30);
            writer.WriteInt16(this.ZoneOffsetMinutes);
            writer.WriteInt16(this.DaylightOffsetMinutes);
            this.StandardDate.SerializeToBand(writer);
            this.DaylightDate.SerializeToBand(writer);
        }
    }
}
