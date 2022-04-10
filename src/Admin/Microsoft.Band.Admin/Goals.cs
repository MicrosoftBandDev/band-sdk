// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Goals
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  public class Goals
  {
    public Goals() => this.Timestamp = DateTime.UtcNow;

    public Goals(
      bool stepsEnabled,
      bool caloriesEnabled,
      bool distanceEnabled,
      uint stepsGoal,
      uint caloriesGoal,
      uint distanceGoal,
      DateTime timestamp)
      : this(stepsEnabled, caloriesEnabled, distanceEnabled, false, stepsGoal, caloriesGoal, distanceGoal, 0U, timestamp)
    {
    }

    public Goals(
      bool stepsEnabled,
      bool caloriesEnabled,
      bool distanceEnabled,
      bool floorCountEnabled,
      uint stepsGoal,
      uint caloriesGoal,
      uint distanceGoal,
      uint floorCountGoal,
      DateTime timestamp)
    {
      this.StepsEnabled = stepsEnabled;
      this.CaloriesEnabled = caloriesEnabled;
      this.DistanceEnabled = distanceEnabled;
      this.FloorCountEnabled = floorCountEnabled;
      this.StepsGoal = stepsGoal;
      this.CaloriesGoal = caloriesGoal;
      this.DistanceGoal = distanceGoal;
      this.FloorCountGoal = floorCountGoal;
      this.Timestamp = timestamp;
    }

    public bool StepsEnabled { get; set; }

    public bool CaloriesEnabled { get; set; }

    public bool DistanceEnabled { get; set; }

    public bool FloorCountEnabled { get; set; }

    public uint StepsGoal { get; set; }

    public uint CaloriesGoal { get; set; }

    public uint DistanceGoal { get; set; }

    public uint FloorCountGoal { get; set; }

    public DateTime Timestamp { get; set; }

    internal static int GetSerializedByteCount(DynamicAdminBandConstants constants)
    {
      switch (constants.BandGoalsSerializedVersion)
      {
        case 1:
          return 76;
        default:
          return 32;
      }
    }

    internal void SerializeToBand(ICargoWriter writer, DynamicAdminBandConstants constants)
    {
      if (constants.BandGoalsSerializedVersion > (ushort) 0)
        writer.WriteInt32((int) constants.BandGoalsSerializedVersion);
      writer.WriteInt32(this.StepsEnabled ? 1 : 0);
      writer.WriteInt32(this.CaloriesEnabled ? 1 : 0);
      writer.WriteInt32(this.DistanceEnabled ? 1 : 0);
      if (constants.BandGoalsSerializedVersion > (ushort) 0)
      {
        writer.WriteInt32(this.FloorCountEnabled ? 1 : 0);
        for (int index = 0; index < 4; ++index)
          writer.WriteInt32(0);
      }
      writer.WriteUInt32(this.StepsGoal);
      writer.WriteUInt32(this.CaloriesGoal);
      writer.WriteUInt32(this.DistanceGoal);
      if (constants.BandGoalsSerializedVersion > (ushort) 0)
      {
        writer.WriteUInt32(this.FloorCountGoal);
        for (int index = 0; index < 4; ++index)
          writer.WriteInt32(0);
      }
      CargoFileTime.SerializeToBandFromDateTime(writer, this.Timestamp);
    }
  }
}
