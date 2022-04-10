// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.NotificationEmail
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Google.Protobuf;
using Microsoft.Band.Protobuf;
using System;

namespace Microsoft.Band.Admin
{
    internal class NotificationEmail : NotificationBase
    {
        private static readonly Guid tileGuid = new Guid("823ba55a-7c98-4261-ad5e-929031289c6e");

        public NotificationEmail()
          : base(NotificationEmail.tileGuid)
        {
        }

        public string Name { get; set; }

        public string Subject { get; set; }

        public DateTime TimeStamp { get; set; }

        internal override int GetSerializedByteCount() => base.GetSerializedByteCount() + 2 + 2 + CargoFileTime.GetSerializedByteCount() + 1 + 1 + this.Name.LengthTruncatedTrimDanglingHighSurrogate(40) * 2 + this.Subject.LengthTruncatedTrimDanglingHighSurrogate(36) * 2;

        internal override void SerializeToBand(ICargoWriter writer)
        {
            int maxLength1 = this.Name.LengthTruncatedTrimDanglingHighSurrogate(40);
            int maxLength2 = this.Subject.LengthTruncatedTrimDanglingHighSurrogate(36);
            base.SerializeToBand(writer);
            writer.WriteUInt16((ushort)(maxLength1 * 2));
            writer.WriteUInt16((ushort)(maxLength2 * 2));
            CargoFileTime.SerializeToBandFromDateTime(writer, this.TimeStamp);
            writer.WriteByte((byte)0);
            writer.WriteByte((byte)0);
            if (maxLength1 > 0)
                writer.WriteStringWithTruncation(this.Name, maxLength1);
            if (maxLength2 <= 0)
                return;
            writer.WriteStringWithTruncation(this.Subject, maxLength2);
        }

        internal override int GetSerializedProtobufByteCount()
        {
            int protobufByteCount = 0 + (2 + ProtoFileTime.GetSerializedProtobufByteCount()) + (2 + ProtoGuid.GetSerializedProtobufByteCount());
            if (!string.IsNullOrWhiteSpace(this.Name))
                protobufByteCount += 1 + CodedOutputStreamExtensions.ComputeStringSize(this.Name, 40);
            if (!string.IsNullOrWhiteSpace(this.Subject))
                protobufByteCount += 1 + CodedOutputStreamExtensions.ComputeStringSize(this.Subject, 320);
            return protobufByteCount;
        }

        internal override void SerializeProtobufToBand(CodedOutputStream output)
        {
            output.WriteRawTag((byte)10);
            output.WriteLength(ProtoFileTime.GetSerializedProtobufByteCount());
            ProtoFileTime.SerializeProtobufToBand(output, this.TimeStamp);
            output.WriteRawTag((byte)18);
            output.WriteLength(ProtoGuid.GetSerializedProtobufByteCount());
            ProtoGuid.SerializeProtobufToBand(output, this.TileId);
            if (!string.IsNullOrWhiteSpace(this.Name))
            {
                output.WriteRawTag((byte)42);
                output.WriteString(this.Name, 40);
            }
            if (string.IsNullOrWhiteSpace(this.Subject))
                return;
            output.WriteRawTag((byte)50);
            output.WriteString(this.Subject, 320);
        }
    }
}
