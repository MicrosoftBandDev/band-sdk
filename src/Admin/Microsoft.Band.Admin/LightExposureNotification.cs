// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LightExposureNotification
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
    public class LightExposureNotification
    {
        private string header;
        private string body;
        private static readonly int SerializedByteCount = 6 + NotificationTime.GetSerializedByteCount() * 7 + 1 + 160 + 80 + 10;

        private LightExposureNotification()
        {
        }

        public LightExposureNotification(string header, string body)
        {
            this.ValidateStringProperty(header, nameof(header));
            this.ValidateStringProperty(body, nameof(body));
            this.header = header;
            this.body = body;
        }

        public NotificationTime Monday { get; set; }

        public NotificationTime Tuesday { get; set; }

        public NotificationTime Wednesday { get; set; }

        public NotificationTime Thursday { get; set; }

        public NotificationTime Friday { get; set; }

        public NotificationTime Saturday { get; set; }

        public NotificationTime Sunday { get; set; }

        public int RequiredExposureLuxValue { get; set; }

        public int RequiredExposureDuration { get; set; }

        public string Header
        {
            get => this.header;
            set
            {
                this.ValidateStringProperty(value, nameof(value));
                this.header = value;
            }
        }

        public string Body
        {
            get => this.body;
            set
            {
                this.ValidateStringProperty(value, nameof(value));
                this.body = value;
            }
        }

        private void ValidateStringProperty(string value, string argName)
        {
            if (value == null)
                throw new ArgumentNullException(argName);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentOutOfRangeException(argName);
        }

        internal static int GetSerializedByteCount() => LightExposureNotification.SerializedByteCount;

        internal static LightExposureNotification DeserializeFromBand(
          ICargoReader reader)
        {
            uint num1 = reader.ReadUInt32();
            ushort num2 = reader.ReadUInt16();
            NotificationTime notificationTime1 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime2 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime3 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime4 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime5 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime6 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime7 = NotificationTime.DeserializeFromBand(reader);
            LightExposureNotification.DayActivatedBitFlags activatedBitFlags = (LightExposureNotification.DayActivatedBitFlags)reader.ReadByte();
            notificationTime1.Enabled = activatedBitFlags.HasFlag((Enum)LightExposureNotification.DayActivatedBitFlags.Monday);
            notificationTime2.Enabled = activatedBitFlags.HasFlag((Enum)LightExposureNotification.DayActivatedBitFlags.Tuesday);
            notificationTime3.Enabled = activatedBitFlags.HasFlag((Enum)LightExposureNotification.DayActivatedBitFlags.Wednesday);
            notificationTime4.Enabled = activatedBitFlags.HasFlag((Enum)LightExposureNotification.DayActivatedBitFlags.Thursday);
            notificationTime5.Enabled = activatedBitFlags.HasFlag((Enum)LightExposureNotification.DayActivatedBitFlags.Friday);
            notificationTime6.Enabled = activatedBitFlags.HasFlag((Enum)LightExposureNotification.DayActivatedBitFlags.Saturday);
            notificationTime7.Enabled = activatedBitFlags.HasFlag((Enum)LightExposureNotification.DayActivatedBitFlags.Sunday);
            LightExposureNotification exposureNotification = new LightExposureNotification(reader.ReadString(80), reader.ReadString(40));
            exposureNotification.RequiredExposureLuxValue = (int)num1;
            exposureNotification.RequiredExposureDuration = (int)num2;
            exposureNotification.Monday = notificationTime1;
            exposureNotification.Tuesday = notificationTime2;
            exposureNotification.Wednesday = notificationTime3;
            exposureNotification.Thursday = notificationTime4;
            exposureNotification.Friday = notificationTime5;
            exposureNotification.Saturday = notificationTime6;
            exposureNotification.Sunday = notificationTime7;
            reader.ReadExactAndDiscard(10);
            return exposureNotification;
        }

        internal void SerializeToBand(ICargoWriter writer)
        {
            writer.WriteUInt32((uint)this.RequiredExposureLuxValue);
            writer.WriteUInt16((ushort)this.RequiredExposureDuration);
            LightExposureNotification.DayActivatedBitFlags bitMask = LightExposureNotification.DayActivatedBitFlags.Zero;
            LightExposureNotification.SerializeTimeAndUpdateBitMask(writer, this.Monday, ref bitMask, LightExposureNotification.DayActivatedBitFlags.Monday);
            LightExposureNotification.SerializeTimeAndUpdateBitMask(writer, this.Tuesday, ref bitMask, LightExposureNotification.DayActivatedBitFlags.Tuesday);
            LightExposureNotification.SerializeTimeAndUpdateBitMask(writer, this.Wednesday, ref bitMask, LightExposureNotification.DayActivatedBitFlags.Wednesday);
            LightExposureNotification.SerializeTimeAndUpdateBitMask(writer, this.Thursday, ref bitMask, LightExposureNotification.DayActivatedBitFlags.Thursday);
            LightExposureNotification.SerializeTimeAndUpdateBitMask(writer, this.Friday, ref bitMask, LightExposureNotification.DayActivatedBitFlags.Friday);
            LightExposureNotification.SerializeTimeAndUpdateBitMask(writer, this.Saturday, ref bitMask, LightExposureNotification.DayActivatedBitFlags.Saturday);
            LightExposureNotification.SerializeTimeAndUpdateBitMask(writer, this.Sunday, ref bitMask, LightExposureNotification.DayActivatedBitFlags.Sunday);
            writer.WriteByte((byte)bitMask);
            writer.WriteStringWithPadding(this.header, 80);
            writer.WriteStringWithPadding(this.body, 40);
            writer.WriteByte((byte)0, 10);
        }

        private static void SerializeTimeAndUpdateBitMask(
          ICargoWriter writer,
          NotificationTime time,
          ref LightExposureNotification.DayActivatedBitFlags bitMask,
          LightExposureNotification.DayActivatedBitFlags bit)
        {
            if (time != null)
            {
                if (time.Enabled)
                    bitMask |= bit;
                time.SerializeToBand(writer);
            }
            else
                writer.WriteByte((byte)0, NotificationTime.GetSerializedByteCount());
        }

        [Flags]
        private enum DayActivatedBitFlags
        {
            Zero = 0,
            Monday = 128, // 0x00000080
            Tuesday = 64, // 0x00000040
            Wednesday = 32, // 0x00000020
            Thursday = 16, // 0x00000010
            Friday = 8,
            Saturday = 4,
            Sunday = 2,
        }
    }
}
