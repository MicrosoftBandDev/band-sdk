// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CalendarEvent
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Google.Protobuf;
using Microsoft.Band.Protobuf;
using System;

namespace Microsoft.Band.Admin
{
  public sealed class CalendarEvent : NotificationBase
  {
    private static readonly Guid tileGuid = new Guid("ec149021-ce45-40e9-aeee-08f86e4746a7");
    private static readonly ushort DefaultNotificationTime = 15;
    private string title;
    private ushort? notificationTime;
    private static readonly int baseSerializedByteCount = 4 + CargoFileTime.GetSerializedByteCount() + 2 + 2 + 2 + 1 + 1 + 1 + 1;

    public CalendarEvent(string title, DateTime startTime, DateTime endTime)
      : base(CalendarEvent.tileGuid)
    {
      if (title == null)
        throw new ArgumentNullException(nameof (title));
      if (endTime < startTime)
        throw new ArgumentException(CommonSR.InvalidEventEndTime);
      this.title = title;
      this.StartTime = startTime;
      this.Duration = (ushort) (endTime - startTime).TotalMinutes;
      this.NotificationTime = new ushort?(CalendarEvent.DefaultNotificationTime);
    }

    public CalendarEvent(string title, string location, DateTime startTime, DateTime endTime)
      : this(title, startTime, endTime)
    {
      this.Location = location;
    }

    public string Title
    {
      get => this.title;
      set => this.title = value != null ? value : throw new ArgumentNullException(nameof (value));
    }

    public string Location { get; set; }

    public DateTime StartTime { get; set; }

    public ushort? NotificationTime
    {
      get => this.notificationTime;
      set
      {
        ushort? nullable1 = value;
        int? nullable2 = nullable1.HasValue ? new int?((int) nullable1.GetValueOrDefault()) : new int?();
        int maxValue = (int) ushort.MaxValue;
        if ((nullable2.GetValueOrDefault() == maxValue ? (nullable2.HasValue ? 1 : 0) : 0) != 0)
          throw new ArgumentOutOfRangeException(nameof (value));
        this.notificationTime = value;
      }
    }

    public ushort Duration { get; set; }

    public CalendarEventAcceptedState AcceptedState { get; set; }

    public bool AllDay { get; set; }

    public byte EventCategory { get; set; }

    internal override int GetSerializedByteCount()
    {
      int num = base.GetSerializedByteCount() + CalendarEvent.baseSerializedByteCount;
      return string.IsNullOrWhiteSpace(this.Location) ? num + this.Title.LengthTruncatedTrimDanglingHighSurrogate(160) * 2 : num + (this.Title.LengthTruncatedTrimDanglingHighSurrogate(20) * 2 + this.Location.LengthTruncatedTrimDanglingHighSurrogate(160) * 2);
    }

    internal override void SerializeToBand(ICargoWriter writer)
    {
      int maxLength1;
      int maxLength2;
      if (!string.IsNullOrWhiteSpace(this.Location))
      {
        maxLength1 = this.Title.LengthTruncatedTrimDanglingHighSurrogate(20);
        maxLength2 = this.Location.LengthTruncatedTrimDanglingHighSurrogate(160);
      }
      else
      {
        maxLength1 = this.Title.LengthTruncatedTrimDanglingHighSurrogate(160);
        maxLength2 = 0;
      }
      base.SerializeToBand(writer);
      if (maxLength2 > 0)
      {
        writer.WriteUInt16((ushort) (maxLength1 * 2));
        writer.WriteUInt16((ushort) (maxLength2 * 2));
      }
      else
      {
        writer.WriteUInt16((ushort) 0);
        writer.WriteUInt16((ushort) (maxLength1 * 2));
      }
      CargoFileTime.SerializeToBandFromDateTimeOffset(writer, (DateTimeOffset) this.StartTime);
      ICargoWriter cargoWriter = writer;
      ushort? notificationTime = this.NotificationTime;
      int maxValue;
      if (!notificationTime.HasValue)
      {
        maxValue = (int) ushort.MaxValue;
      }
      else
      {
        notificationTime = this.NotificationTime;
        maxValue = (int) notificationTime.Value;
      }
      cargoWriter.WriteUInt16((ushort) maxValue);
      writer.WriteUInt16(this.Duration);
      writer.WriteUInt16((ushort) this.AcceptedState);
      writer.WriteByte(this.AllDay ? (byte) 1 : (byte) 0);
      writer.WriteByte(this.EventCategory);
      writer.WriteByte((byte) 0);
      writer.WriteByte((byte) 0);
      writer.WriteStringWithTruncation(this.Title, maxLength1);
      if (maxLength2 <= 0)
        return;
      writer.WriteStringWithTruncation(this.Location, maxLength2);
    }

    internal override int GetSerializedProtobufByteCount()
    {
      int num = 0 + (2 + ProtoFileTime.GetSerializedProtobufByteCount()) + (1 + CodedOutputStream.ComputeUInt32Size((uint) (this.NotificationTime.HasValue ? this.NotificationTime : new ushort?(ushort.MaxValue)).Value));
      if (this.Duration != (ushort) 0)
        num += 1 + CodedOutputStream.ComputeUInt32Size((uint) this.Duration);
      int protobufByteCount = string.IsNullOrWhiteSpace(this.Location) ? num + (1 + CodedOutputStreamExtensions.ComputeStringSize(this.Title, 320)) : num + (1 + CodedOutputStreamExtensions.ComputeStringSize(this.Title, 40)) + (1 + CodedOutputStreamExtensions.ComputeStringSize(this.Location, 320));
      if (this.AcceptedState != CalendarEventAcceptedState.Accepted)
        protobufByteCount += 1 + CodedOutputStream.ComputeEnumSize((int) this.AcceptedState);
      CalendarEvent.ProtobufFlags protobufFlags = this.GetProtobufFlags();
      if (protobufFlags != CalendarEvent.ProtobufFlags.None)
        protobufByteCount += 1 + CodedOutputStream.ComputeUInt32Size((uint) protobufFlags);
      return protobufByteCount;
    }

    internal override void SerializeProtobufToBand(CodedOutputStream output)
    {
      output.WriteRawTag((byte) 10);
      output.WriteLength(ProtoFileTime.GetSerializedProtobufByteCount());
      ProtoFileTime.SerializeProtobufToBand(output, this.StartTime);
      output.WriteRawTag((byte) 16);
      output.WriteUInt32((uint) (this.NotificationTime.HasValue ? this.NotificationTime : new ushort?(ushort.MaxValue)).Value);
      if (this.Duration != (ushort) 0)
      {
        output.WriteRawTag((byte) 24);
        output.WriteUInt32((uint) this.Duration);
      }
      if (!string.IsNullOrWhiteSpace(this.Location))
      {
        output.WriteRawTag((byte) 34);
        output.WriteString(this.Title, 40);
        output.WriteRawTag((byte) 42);
        output.WriteString(this.Location, 320);
      }
      else
      {
        output.WriteRawTag((byte) 42);
        output.WriteString(this.Title, 320);
      }
      if (this.AcceptedState != CalendarEventAcceptedState.Accepted)
      {
        output.WriteRawTag((byte) 48);
        output.WriteEnum((int) this.AcceptedState);
      }
      CalendarEvent.ProtobufFlags protobufFlags = this.GetProtobufFlags();
      if (protobufFlags == CalendarEvent.ProtobufFlags.None)
        return;
      output.WriteRawTag((byte) 56);
      output.WriteUInt32((uint) protobufFlags);
    }

    private CalendarEvent.ProtobufFlags GetProtobufFlags()
    {
      CalendarEvent.ProtobufFlags protobufFlags = CalendarEvent.ProtobufFlags.None;
      if (this.AllDay)
        protobufFlags |= CalendarEvent.ProtobufFlags.AllDay;
      if (this.EventCategory == (byte) 1)
        protobufFlags |= CalendarEvent.ProtobufFlags.Cancelled;
      if (this.EventCategory == (byte) 2)
        protobufFlags |= CalendarEvent.ProtobufFlags.MsHealth;
      return protobufFlags;
    }

    public static class CalendarEventTypes
    {
      public const byte NoSpecialFormatting = 0;
      public const byte CanceledMeeting = 1;
      public const byte MSHealth = 2;
    }

    [Flags]
    private enum ProtobufFlags
    {
      None = 0,
      Cancelled = 1,
      MsHealth = 2,
      AllDay = 4,
    }
  }
}
