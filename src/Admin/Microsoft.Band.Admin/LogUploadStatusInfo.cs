// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LogUploadStatusInfo
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Runtime.Serialization;

namespace Microsoft.Band.Admin
{
    [DataContract]
    internal sealed class LogUploadStatusInfo
    {
        public LogUploadStatusInfo() => this.UploadStatus = LogUploadStatus.Unknown;

        [DataMember(Name = "UploadStatus")]
        public string UploadStatusDeserializer
        {
            set
            {
                if (value == null)
                    return;
                this.UploadStatus = LogUploadStatusInfo.LogProcessingResponseContentToUploadStatus(value);
            }
        }

        public LogUploadStatus UploadStatus { get; private set; }

        private static LogUploadStatus LogProcessingResponseContentToUploadStatus(
          string content)
        {
            switch (content.ToLower())
            {
                case "activitiesprocessingdone":
                    return LogUploadStatus.ActivitiesProcessingDone;
                case "eventsprocessingblocked":
                    return LogUploadStatus.EventsProcessingBlocked;
                case "eventsprocessingdone":
                    return LogUploadStatus.EventsProcessingDone;
                case "queuedforetl":
                    return LogUploadStatus.QueuedForETL;
                case "uploaddone":
                    return LogUploadStatus.UploadDone;
                case "uploadpathsent":
                    return LogUploadStatus.UploadPathSent;
                default:
                    return LogUploadStatus.Unknown;
            }
        }
    }
}
