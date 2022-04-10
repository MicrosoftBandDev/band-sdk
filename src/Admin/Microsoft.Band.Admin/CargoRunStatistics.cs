// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoRunStatistics
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  public sealed class CargoRunStatistics
  {
    private static readonly int serializedByteCount = CargoFileTime.GetSerializedByteCount() + 2 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + CargoFileTime.GetSerializedByteCount() + 4;

    private CargoRunStatistics()
    {
    }

    public DateTime Timestamp { get; private set; }

    public ushort Version { get; private set; }

    public uint Duration { get; private set; }

    public uint Distance { get; private set; }

    public uint AveragePace { get; private set; }

    public uint BestSplit { get; private set; }

    public uint Calories { get; private set; }

    public uint AverageHeartrate { get; private set; }

    public uint MaximumHeartrate { get; private set; }

    public DateTime EndTime { get; private set; }

    public FeelingType Feeling { get; private set; }

    internal static int GetSerializedByteCount() => CargoRunStatistics.serializedByteCount;

    internal static CargoRunStatistics DeserializeFromBand(ICargoReader reader) => new CargoRunStatistics()
    {
      Timestamp = CargoFileTime.DeserializeFromBandAsDateTime(reader),
      Version = reader.ReadUInt16(),
      Duration = reader.ReadUInt32(),
      Distance = reader.ReadUInt32(),
      AveragePace = reader.ReadUInt32(),
      BestSplit = reader.ReadUInt32(),
      Calories = reader.ReadUInt32(),
      AverageHeartrate = reader.ReadUInt32(),
      MaximumHeartrate = reader.ReadUInt32(),
      EndTime = CargoFileTime.DeserializeFromBandAsDateTime(reader),
      Feeling = (FeelingType) reader.ReadUInt32()
    };
  }
}
