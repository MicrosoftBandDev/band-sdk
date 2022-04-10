// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Streaming.LogEntryUpdatedEventArgs
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin.Streaming
{
    public sealed class LogEntryUpdatedEventArgs : EventArgs
    {
        private LogEntryUpdatedEventArgs()
        {
        }

        public byte EntryType { get; private set; }

        public byte[] Data { get; private set; }

        internal static LogEntryUpdatedEventArgs DeserializeFromBand(
          ICargoReader reader)
        {
            byte num = reader.ReadByte();
            byte count = reader.ReadByte();
            byte[] numArray = reader.ReadExact((int)count);
            return new LogEntryUpdatedEventArgs()
            {
                EntryType = num,
                Data = numArray
            };
        }
    }
}
