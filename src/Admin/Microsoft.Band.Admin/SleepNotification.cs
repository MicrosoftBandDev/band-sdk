// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.SleepNotification
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
    public class SleepNotification
    {
        private string header;
        private string body;
        private static readonly int SerializedByteCount = NotificationTime.GetSerializedByteCount() * 7 + 1 + 1 + 160 + 80 + 8;

        private SleepNotification()
        {
        }

        public SleepNotification(string header, string body)
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

        internal static int GetSerializedByteCount() => SleepNotification.SerializedByteCount;

        internal static SleepNotification DeserializeFromBand(ICargoReader reader)
        {
            NotificationTime notificationTime1 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime2 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime3 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime4 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime5 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime6 = NotificationTime.DeserializeFromBand(reader);
            NotificationTime notificationTime7 = NotificationTime.DeserializeFromBand(reader);
            SleepNotification.DayActivatedBitFlags activatedBitFlags = (SleepNotification.DayActivatedBitFlags)reader.ReadByte();
            reader.ReadExactAndDiscard(1);
            notificationTime1.Enabled = activatedBitFlags.HasFlag((Enum)SleepNotification.DayActivatedBitFlags.Monday);
            notificationTime2.Enabled = activatedBitFlags.HasFlag((Enum)SleepNotification.DayActivatedBitFlags.Tuesday);
            notificationTime3.Enabled = activatedBitFlags.HasFlag((Enum)SleepNotification.DayActivatedBitFlags.Wednesday);
            notificationTime4.Enabled = activatedBitFlags.HasFlag((Enum)SleepNotification.DayActivatedBitFlags.Thursday);
            notificationTime5.Enabled = activatedBitFlags.HasFlag((Enum)SleepNotification.DayActivatedBitFlags.Friday);
            notificationTime6.Enabled = activatedBitFlags.HasFlag((Enum)SleepNotification.DayActivatedBitFlags.Saturday);
            notificationTime7.Enabled = activatedBitFlags.HasFlag((Enum)SleepNotification.DayActivatedBitFlags.Sunday);
            SleepNotification sleepNotification = new SleepNotification(reader.ReadString(80), reader.ReadString(40));
            sleepNotification.Monday = notificationTime1;
            sleepNotification.Tuesday = notificationTime2;
            sleepNotification.Wednesday = notificationTime3;
            sleepNotification.Thursday = notificationTime4;
            sleepNotification.Friday = notificationTime5;
            sleepNotification.Saturday = notificationTime6;
            sleepNotification.Sunday = notificationTime7;
            reader.ReadExactAndDiscard(8);
            return sleepNotification;
        }

        internal void SerializeToBand(ICargoWriter writer)
        {
            SleepNotification.DayActivatedBitFlags bitMask = SleepNotification.DayActivatedBitFlags.Zero;
            SleepNotification.SerializeTimeAndUpdateBitMask(writer, this.Monday, ref bitMask, SleepNotification.DayActivatedBitFlags.Monday);
            SleepNotification.SerializeTimeAndUpdateBitMask(writer, this.Tuesday, ref bitMask, SleepNotification.DayActivatedBitFlags.Tuesday);
            SleepNotification.SerializeTimeAndUpdateBitMask(writer, this.Wednesday, ref bitMask, SleepNotification.DayActivatedBitFlags.Wednesday);
            SleepNotification.SerializeTimeAndUpdateBitMask(writer, this.Thursday, ref bitMask, SleepNotification.DayActivatedBitFlags.Thursday);
            SleepNotification.SerializeTimeAndUpdateBitMask(writer, this.Friday, ref bitMask, SleepNotification.DayActivatedBitFlags.Friday);
            SleepNotification.SerializeTimeAndUpdateBitMask(writer, this.Saturday, ref bitMask, SleepNotification.DayActivatedBitFlags.Saturday);
            SleepNotification.SerializeTimeAndUpdateBitMask(writer, this.Sunday, ref bitMask, SleepNotification.DayActivatedBitFlags.Sunday);
            writer.WriteByte((byte)bitMask);
            writer.WriteByte((byte)0);
            writer.WriteStringWithPadding(this.header, 80);
            writer.WriteStringWithPadding(this.body, 40);
            writer.WriteByte((byte)0, 8);
        }

        private static void SerializeTimeAndUpdateBitMask(
          ICargoWriter writer,
          NotificationTime time,
          ref SleepNotification.DayActivatedBitFlags bitMask,
          SleepNotification.DayActivatedBitFlags bit)
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
