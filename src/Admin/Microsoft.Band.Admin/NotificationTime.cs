// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.NotificationTime
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  public class NotificationTime
  {
    private byte hour;
    private byte minute;
    private const int SerializedByteCount = 2;

    public NotificationTime(byte hour, byte minute, bool enabled = false)
    {
      this.ValidateHour(hour, nameof (hour));
      this.ValidateMinute(minute, nameof (minute));
      this.hour = hour;
      this.minute = minute;
      this.Enabled = enabled;
    }

    public byte Hour
    {
      get => this.hour;
      set
      {
        this.ValidateHour(value, nameof (value));
        this.hour = value;
      }
    }

    public byte Minute
    {
      get => this.minute;
      set
      {
        this.ValidateMinute(value, nameof (value));
        this.minute = value;
      }
    }

    public bool Enabled { get; set; }

    private void ValidateHour(byte hour, string argName) => this.ValidateField(hour, (byte) 23, argName);

    private void ValidateMinute(byte minute, string argName) => this.ValidateField(minute, (byte) 59, argName);

    private void ValidateField(byte value, byte maxValue, string argName)
    {
      if ((int) value > (int) maxValue)
        throw new ArgumentOutOfRangeException(argName);
    }

    internal static int GetSerializedByteCount() => 2;

    internal static NotificationTime DeserializeFromBand(ICargoReader reader) => new NotificationTime(reader.ReadByte(), reader.ReadByte());

    internal void SerializeToBand(ICargoWriter writer)
    {
      writer.WriteByte(this.hour);
      writer.WriteByte(this.minute);
    }
  }
}
