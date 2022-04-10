// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoCall
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Google.Protobuf;
using Microsoft.Band.Protobuf;
using System;

namespace Microsoft.Band.Admin
{
    public sealed class CargoCall : NotificationBase
    {
        private static readonly Guid tileGuid = new Guid("22b1c099-f2be-4bac-8ed8-2d6b0b3c25d1");
        public string callerName;
        private NotificationFlags flags;
        private const AllowedResponseTypes DefaultIncomingCallResponseTypes = AllowedResponseTypes.Keyboard | AllowedResponseTypes.Canned;
        private const AllowedDefaultButtonTypes DefaultIncomingCallDefaultButtonTypes = AllowedDefaultButtonTypes.Ignore | AllowedDefaultButtonTypes.ReplyPanel;

        public CargoCall(uint callId, string Name, DateTime timestamp, NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings)
          : base(CargoCall.tileGuid)
        {
            this.Call_Id = callId;
            this.CallerName = Name;
            this.Timestamp = timestamp;
            this.Flags = flagbits;
            Logger.Log(LogLevel.Verbose, "[CargoCall.CargoCall()] Object constructed. Flags {0}", (object)this.flags);
        }

        public uint Call_Id { get; set; }

        public string CallerName
        {
            get => this.callerName;
            set => this.callerName = value != null ? value : throw new ArgumentNullException(nameof(value));
        }

        public DateTime Timestamp { get; set; }

        public NotificationFlags Flags
        {
            get => this.flags;
            set => this.flags = value <= NotificationFlags.MaxValue ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        internal CargoCall.PhoneCallType CallType { get; set; }

        internal override int GetSerializedByteCount() => base.GetSerializedByteCount() + 2 + 4 + CargoFileTime.GetSerializedByteCount() + 1 + 1 + this.CallerName.LengthTruncatedTrimDanglingHighSurrogate(20) * 2;

        internal override void SerializeToBand(ICargoWriter writer)
        {
            int maxLength = this.CallerName.LengthTruncatedTrimDanglingHighSurrogate(20);
            base.SerializeToBand(writer);
            writer.WriteUInt16((ushort)(maxLength * 2));
            writer.WriteUInt32(this.Call_Id);
            CargoFileTime.SerializeToBandFromDateTimeOffset(writer, (DateTimeOffset)this.Timestamp);
            writer.WriteByte((byte)this.Flags);
            writer.WriteByte((byte)0);
            if (maxLength <= 0)
                return;
            writer.WriteStringWithTruncation(this.CallerName, maxLength);
        }

        internal override int GetSerializedProtobufByteCount()
        {
            int protobufByteCount = 0 + (2 + ProtoFileTime.GetSerializedProtobufByteCount()) + (2 + ProtoGuid.GetSerializedProtobufByteCount()) + (1 + CodedOutputStream.ComputeInt32Size((int)this.Call_Id));
            if (!string.IsNullOrWhiteSpace(this.CallerName))
                protobufByteCount += 1 + CodedOutputStreamExtensions.ComputeStringSize(this.CallerName, 40);
            if (this.Flags != NotificationFlags.UnmodifiedNotificationSettings)
                protobufByteCount += 1 + CodedOutputStream.ComputeUInt32Size((uint)this.Flags);
            if (this.CallType != CargoCall.PhoneCallType.None)
                protobufByteCount += 1 + CodedOutputStream.ComputeEnumSize((int)this.CallType);
            if (this.CallType == CargoCall.PhoneCallType.Incoming)
                protobufByteCount = protobufByteCount + (1 + CodedOutputStream.ComputeUInt32Size(17U)) + (1 + CodedOutputStream.ComputeUInt32Size(24U));
            return protobufByteCount;
        }

        internal override void SerializeProtobufToBand(CodedOutputStream output)
        {
            output.WriteRawTag((byte)10);
            output.WriteLength(ProtoFileTime.GetSerializedProtobufByteCount());
            ProtoFileTime.SerializeProtobufToBand(output, this.Timestamp);
            output.WriteRawTag((byte)18);
            output.WriteLength(ProtoGuid.GetSerializedProtobufByteCount());
            ProtoGuid.SerializeProtobufToBand(output, this.TileId);
            output.WriteRawTag((byte)24);
            output.WriteInt32((int)this.Call_Id);
            if (!string.IsNullOrWhiteSpace(this.CallerName))
            {
                output.WriteRawTag((byte)50);
                output.WriteString(this.CallerName, 40);
            }
            if (this.Flags != NotificationFlags.UnmodifiedNotificationSettings)
            {
                output.WriteRawTag((byte)80);
                output.WriteUInt32((uint)this.Flags);
            }
            if (this.CallType != CargoCall.PhoneCallType.None)
            {
                output.WriteRawTag((byte)88);
                output.WriteEnum((int)this.CallType);
            }
            if (this.CallType != CargoCall.PhoneCallType.Incoming)
                return;
            output.WriteRawTag((byte)104);
            output.WriteUInt32(17U);
            output.WriteRawTag((byte)112);
            output.WriteUInt32(24U);
        }

        internal enum PhoneCallType
        {
            None,
            Incoming,
            Answered,
            Missed,
            Hangup,
            VoiceMail,
        }
    }
}
