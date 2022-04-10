// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoSleepStatistics
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
    public sealed class CargoSleepStatistics
    {
        private static readonly int serializedByteCount = CargoFileTime.GetSerializedByteCount() + 2 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + CargoFileTime.GetSerializedByteCount() + 4 + 4;

        private CargoSleepStatistics()
        {
        }

        public DateTime Timestamp { get; private set; }

        public ushort Version { get; private set; }

        public uint Duration { get; private set; }

        public uint TimesWokeUp { get; private set; }

        public uint TimeAwake { get; private set; }

        public uint TimeAsleep { get; private set; }

        public uint CaloriesBurned { get; private set; }

        public uint RestingHeartrate { get; private set; }

        public DateTime EndTime { get; private set; }

        public uint TimeToFallAsleep { get; private set; }

        public FeelingType Feeling { get; private set; }

        internal static int GetSerializedByteCount() => CargoSleepStatistics.serializedByteCount;

        internal static CargoSleepStatistics DeserializeFromBand(ICargoReader reader) => new CargoSleepStatistics()
        {
            Timestamp = CargoFileTime.DeserializeFromBandAsDateTime(reader),
            Version = reader.ReadUInt16(),
            Duration = reader.ReadUInt32(),
            TimesWokeUp = reader.ReadUInt32(),
            TimeAwake = reader.ReadUInt32(),
            TimeAsleep = reader.ReadUInt32(),
            CaloriesBurned = reader.ReadUInt32(),
            RestingHeartrate = CargoSleepStatistics.ReadUInt32DiscardNext(reader),
            EndTime = CargoFileTime.DeserializeFromBandAsDateTime(reader),
            TimeToFallAsleep = reader.ReadUInt32(),
            Feeling = (FeelingType)reader.ReadUInt32()
        };

        private static uint ReadUInt32DiscardNext(ICargoReader reader)
        {
            int num = (int)reader.ReadUInt32();
            reader.ReadExactAndDiscard(4);
            return (uint)num;
        }
    }
}
