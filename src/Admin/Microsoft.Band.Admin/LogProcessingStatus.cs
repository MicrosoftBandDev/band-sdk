// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LogProcessingStatus
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
    public sealed class LogProcessingStatus
    {
        public LogProcessingStatus(string uploadId, DateTime knownStatus)
        {
            this.UploadId = uploadId;
            this.KnownStatus = knownStatus;
        }

        public LogFileTypes FileType => LogFileTypes.Sensor;

        public string UploadId { get; private set; }

        public DateTime KnownStatus { get; set; }
    }
}
