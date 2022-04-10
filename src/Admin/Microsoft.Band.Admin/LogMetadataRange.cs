// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LogMetadataRange
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin
{
  internal sealed class LogMetadataRange
  {
    private const int serializedByteCount = 12;

    private LogMetadataRange()
    {
    }

    public uint StartingSeqNumber { get; private set; }

    public uint EndingSeqNumber { get; private set; }

    public uint ByteCount { get; private set; }

    internal static int GetSerializedByteCount() => 12;

    internal static LogMetadataRange DeserializeFromBand(ICargoReader reader) => new LogMetadataRange()
    {
      StartingSeqNumber = reader.ReadUInt32(),
      EndingSeqNumber = reader.ReadUInt32(),
      ByteCount = reader.ReadUInt32()
    };

    internal void SerializeToBand(ICargoWriter writer)
    {
      writer.WriteUInt32(this.StartingSeqNumber);
      writer.WriteUInt32(this.EndingSeqNumber);
      writer.WriteUInt32(this.ByteCount);
    }

    internal byte[] SerializeToByteArray()
    {
      int offset1 = 0;
      byte[] buffer = new byte[LogMetadataRange.GetSerializedByteCount()];
      BandBitConverter.GetBytes(this.StartingSeqNumber, buffer, offset1);
      int offset2 = offset1 + 4;
      BandBitConverter.GetBytes(this.EndingSeqNumber, buffer, offset2);
      int offset3 = offset2 + 4;
      BandBitConverter.GetBytes(this.ByteCount, buffer, offset3);
      return buffer;
    }
  }
}
