// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.NotificationGenericClearPage
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Google.Protobuf;
using Microsoft.Band.Protobuf;
using System;

namespace Microsoft.Band.Admin
{
    internal sealed class NotificationGenericClearPage : NotificationBase
    {
        public NotificationGenericClearPage(Guid tileId, Guid pageId)
          : base(tileId)
        {
            this.PageId = pageId;
        }

        public Guid PageId { get; private set; }

        internal override int GetSerializedByteCount() => base.GetSerializedByteCount() + 16 + 1 + 1;

        internal override void SerializeToBand(ICargoWriter writer)
        {
            base.SerializeToBand(writer);
            writer.WriteGuid(this.PageId);
            writer.WriteByte((byte)0);
            writer.WriteByte((byte)0);
        }

        internal override int GetSerializedProtobufByteCount() => 0 + 2 + ProtoGuid.GetSerializedProtobufByteCount() + 2 + ProtoGuid.GetSerializedProtobufByteCount() + (1 + CodedOutputStream.ComputeEnumSize(2));

        internal override void SerializeProtobufToBand(CodedOutputStream output)
        {
            output.WriteRawTag((byte)10);
            output.WriteLength(ProtoGuid.GetSerializedProtobufByteCount());
            ProtoGuid.SerializeProtobufToBand(output, this.TileId);
            output.WriteRawTag((byte)18);
            output.WriteLength(ProtoGuid.GetSerializedProtobufByteCount());
            ProtoGuid.SerializeProtobufToBand(output, this.PageId);
            output.WriteRawTag((byte)24);
            output.WriteEnum(2);
        }
    }
}
