// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.NotificationBase
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Google.Protobuf;
using System;

namespace Microsoft.Band.Admin
{
  public abstract class NotificationBase
  {
    public NotificationBase(Guid tileId) => this.TileId = tileId;

    public Guid TileId { get; private set; }

    internal virtual int GetSerializedByteCount() => 16;

    internal virtual void SerializeToBand(ICargoWriter writer) => writer.WriteGuid(this.TileId);

    internal abstract int GetSerializedProtobufByteCount();

    internal abstract void SerializeProtobufToBand(CodedOutputStream output);
  }
}
