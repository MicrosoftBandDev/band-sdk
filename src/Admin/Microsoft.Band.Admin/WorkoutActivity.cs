// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WorkoutActivity
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  public class WorkoutActivity
  {
    private string name;
    private static readonly int SerializedByteCount = 114;

    public WorkoutActivity(Guid id, string name)
    {
      this.ValidateName(name, nameof (name));
      this.Id = id;
      this.Name = name;
    }

    public Guid Id { get; set; }

    public string Name
    {
      get => this.name;
      set
      {
        this.ValidateName(value, nameof (value));
        this.name = value;
      }
    }

    public uint Flags { get; set; }

    public uint AlgorithmFlags { get; set; }

    public byte TrackingAlgorithmId { get; set; }

    private void ValidateName(string value, string argName)
    {
      if (value == null)
        throw new ArgumentNullException(argName);
      if (string.IsNullOrWhiteSpace(value))
        throw new ArgumentOutOfRangeException(argName);
    }

    internal static int GetSerializedByteCount() => WorkoutActivity.SerializedByteCount;

    internal static WorkoutActivity DeserializeFromBand(ICargoReader reader)
    {
      WorkoutActivity workoutActivity = (WorkoutActivity) null;
      string str = reader.ReadString(40);
      if (str.Length > 0)
      {
        string name = str;
        workoutActivity = new WorkoutActivity(reader.ReadGuid(), name)
        {
          Flags = reader.ReadUInt32(),
          AlgorithmFlags = reader.ReadUInt32(),
          TrackingAlgorithmId = reader.ReadByte()
        };
      }
      else
      {
        reader.ReadExactAndDiscard(16);
        reader.ReadExactAndDiscard(4);
        reader.ReadExactAndDiscard(4);
        reader.ReadExactAndDiscard(1);
      }
      reader.ReadExactAndDiscard(9);
      return workoutActivity;
    }

    internal void SerializeToBand(ICargoWriter writer)
    {
      writer.WriteStringWithPadding(this.Name, 40);
      writer.WriteGuid(this.Id);
      writer.WriteUInt32(this.Flags);
      writer.WriteUInt32(this.AlgorithmFlags);
      writer.WriteByte(this.TrackingAlgorithmId);
      writer.WriteByte((byte) 0, 9);
    }
  }
}
