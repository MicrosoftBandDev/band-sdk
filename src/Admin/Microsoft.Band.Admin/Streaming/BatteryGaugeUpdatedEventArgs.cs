// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Streaming.BatteryGaugeUpdatedEventArgs
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin.Streaming
{
  public sealed class BatteryGaugeUpdatedEventArgs : EventArgs
  {
    private const int serializedByteCount = 5;

    private BatteryGaugeUpdatedEventArgs()
    {
    }

    public byte PercentCharge { get; private set; }

    public ushort FilteredVoltage { get; private set; }

    public ushort BatteryGaugeAlerts { get; private set; }

    internal static int GetSerializedByteCount() => 5;

    internal static BatteryGaugeUpdatedEventArgs DeserializeFromBand(
      ICargoReader reader)
    {
      return new BatteryGaugeUpdatedEventArgs()
      {
        PercentCharge = reader.ReadByte(),
        FilteredVoltage = reader.ReadUInt16(),
        BatteryGaugeAlerts = reader.ReadUInt16()
      };
    }
  }
}
