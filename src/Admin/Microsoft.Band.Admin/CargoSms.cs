// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoSms
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Google.Protobuf;
using Microsoft.Band.Protobuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Band.Admin
{
    public sealed class CargoSms : NotificationBase
    {
        private static readonly Guid tileGuid = new Guid("b4edbc35-027b-4d10-a797-1099cd2ad98a");
        public uint call_Id;
        private string sender;
        private string body;
        private NotificationFlags flags;
        private const string participantDelimiter = ", ";
        private const AllowedResponseTypes DefaultResponseTypes = AllowedResponseTypes.Keyboard | AllowedResponseTypes.Dictation | AllowedResponseTypes.Smart | AllowedResponseTypes.Canned;
        private const AllowedDefaultButtonTypes DefaultDefaultButtonTypes = AllowedDefaultButtonTypes.ReplyPanel;

        public CargoSms(
          uint callId,
          string sender,
          string body,
          DateTime timestamp,
          NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings)
          : base(CargoSms.tileGuid)
        {
            this.Call_Id = callId;
            this.Sender = sender;
            this.Body = body;
            this.Timestamp = timestamp;
            this.Flags = flagbits;
            this.Multimedia = NotificationMmsType.None;
            this.Participants = (IList<string>)new List<string>();
        }

        public uint Call_Id
        {
            get => this.call_Id;
            set => this.call_Id = value;
        }

        public string Sender
        {
            get => this.sender;
            set => this.sender = value != null ? value : throw new ArgumentNullException(nameof(value));
        }

        public string Body
        {
            get => this.body;
            set => this.body = value != null ? value : throw new ArgumentNullException(nameof(value));
        }

        public DateTime Timestamp { get; set; }

        public NotificationFlags Flags
        {
            get => this.flags;
            set => this.flags = value <= NotificationFlags.MaxValue ? value : throw new ArgumentOutOfRangeException(nameof(value));
        }

        public NotificationMmsType Multimedia { get; set; }

        public IList<string> Participants { get; set; }

        public CargoAutoResponseResult AutoResponses { get; set; }

        private int GetParticipantCount() => 2 + (this.Participants != null ? this.Participants.Count : 0);

        private string GetParticipantNames()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(this.Sender);
            if (this.Participants != null)
            {
                foreach (string participant in (IEnumerable<string>)this.Participants)
                    stringBuilder.AppendFormat("{0}{1}", new object[2]
                    {
            (object) ", ",
            (object) participant
                    });
            }
            return stringBuilder.ToString();
        }

        internal override int GetSerializedByteCount()
        {
            int num1 = this.Body.LengthTruncatedTrimDanglingHighSurrogate(160) * 2;
            int bytesAvailable = 320 - num1;
            int num2 = 0;
            if (this.AutoResponses != null)
                num2 = this.AutoResponses.GetSerializedByteCount(bytesAvailable);
            return base.GetSerializedByteCount() + 2 + 2 + 4 + 2 + CargoFileTime.GetSerializedByteCount() + 1 + 1 + 1 + this.GetParticipantNames().LengthTruncatedTrimDanglingHighSurrogate(20) * 2 + num1 + num2;
        }

        internal override void SerializeToBand(ICargoWriter writer)
        {
            string participantNames = this.GetParticipantNames();
            NotificationFlags flags = this.Flags;
            int maxLength1 = participantNames.LengthTruncatedTrimDanglingHighSurrogate(20);
            int maxLength2 = this.Body.LengthTruncatedTrimDanglingHighSurrogate(160);
            int i = maxLength2 * 2;
            int bytesAvailable = 320 - i;
            int num = 0;
            if (this.AutoResponses != null)
                num = this.AutoResponses.GetSerializedByteCount(bytesAvailable);
            if (num > 0)
                flags |= NotificationFlags.AutoResponseAvailable;
            base.SerializeToBand(writer);
            writer.WriteUInt16((ushort)(maxLength1 * 2));
            writer.WriteUInt16((ushort)i);
            writer.WriteUInt32(this.Call_Id);
            writer.WriteUInt16((ushort)this.GetParticipantCount());
            CargoFileTime.SerializeToBandFromDateTime(writer, this.Timestamp);
            writer.WriteByte((byte)this.Multimedia);
            writer.WriteByte((byte)flags);
            writer.WriteByte((byte)0);
            if (maxLength1 > 0)
                writer.WriteStringWithTruncation(participantNames, maxLength1);
            if (maxLength2 > 0)
                writer.WriteStringWithTruncation(this.Body, maxLength2);
            if (num <= 0)
                return;
            this.AutoResponses.SerializeToBand(writer, bytesAvailable);
        }

        internal override int GetSerializedProtobufByteCount()
        {
            int num1 = 0;
            NotificationFlags flags = this.Flags;
            int num2 = 0;
            string participantNames = this.GetParticipantNames();
            int num3 = num1 + (2 + ProtoFileTime.GetSerializedProtobufByteCount()) + (2 + ProtoGuid.GetSerializedProtobufByteCount());
            if (this.Call_Id > 0U)
                num3 += 1 + CodedOutputStream.ComputeInt32Size((int)this.Call_Id);
            if (!string.IsNullOrWhiteSpace(participantNames))
                num3 += 1 + CodedOutputStreamExtensions.ComputeStringSize(participantNames, 40);
            if (!string.IsNullOrWhiteSpace(this.Body))
                num3 += 1 + CodedOutputStreamExtensions.ComputeStringSize(this.Body, 320);
            int num4 = num3 + (1 + CodedOutputStream.ComputeUInt32Size((uint)this.GetParticipantCount()));
            if (this.AutoResponses != null)
                num2 = this.AutoResponses.GetSerializedProtobufByteCount();
            if (num2 > 0)
            {
                num4 += num2;
                flags |= NotificationFlags.AutoResponseAvailable;
            }
            if (flags != NotificationFlags.UnmodifiedNotificationSettings)
                num4 += 1 + CodedOutputStream.ComputeUInt32Size((uint)flags);
            if (this.Multimedia != NotificationMmsType.None)
                num4 += 1 + CodedOutputStream.ComputeEnumSize((int)this.Multimedia);
            return num4 + (1 + CodedOutputStream.ComputeUInt32Size(27U)) + (1 + CodedOutputStream.ComputeUInt32Size(16U));
        }

        internal override void SerializeProtobufToBand(CodedOutputStream output)
        {
            NotificationFlags flags = this.Flags;
            int num = 0;
            string participantNames = this.GetParticipantNames();
            output.WriteRawTag((byte)10);
            output.WriteLength(ProtoFileTime.GetSerializedProtobufByteCount());
            ProtoFileTime.SerializeProtobufToBand(output, this.Timestamp);
            output.WriteRawTag((byte)18);
            output.WriteLength(ProtoGuid.GetSerializedProtobufByteCount());
            ProtoGuid.SerializeProtobufToBand(output, this.TileId);
            if (this.Call_Id > 0U)
            {
                output.WriteRawTag((byte)24);
                output.WriteInt32((int)this.Call_Id);
            }
            if (!string.IsNullOrWhiteSpace(participantNames))
            {
                output.WriteRawTag((byte)42);
                output.WriteString(participantNames, 40);
            }
            if (!string.IsNullOrWhiteSpace(this.Body))
            {
                output.WriteRawTag((byte)50);
                output.WriteString(this.Body, 320);
            }
            output.WriteRawTag((byte)56);
            output.WriteUInt32((uint)this.GetParticipantCount());
            if (this.AutoResponses != null)
                num = this.AutoResponses.GetSerializedProtobufByteCount();
            if (num > 0)
            {
                this.AutoResponses.SerializeProtobufToBand(output);
                flags |= NotificationFlags.AutoResponseAvailable;
            }
            if (flags != NotificationFlags.UnmodifiedNotificationSettings)
            {
                output.WriteRawTag((byte)80);
                output.WriteUInt32((uint)flags);
            }
            if (this.Multimedia != NotificationMmsType.None)
            {
                output.WriteRawTag((byte)96);
                output.WriteEnum((int)this.Multimedia);
            }
            output.WriteRawTag((byte)104);
            output.WriteUInt32(27U);
            output.WriteRawTag((byte)112);
            output.WriteUInt32(16U);
        }
    }
}
