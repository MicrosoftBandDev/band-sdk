// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LogSyncResult
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Collections.Generic;

namespace Microsoft.Band.Admin
{
  internal sealed class LogSyncResult
  {
    internal long DownloadedSensorLogBytes { get; set; }

    internal long UploadedSensorLogBytes { get; set; }

    internal double DownloadKbitsPerSecond { get; set; }

    internal double DownloadKbytesPerSecond { get; set; }

    internal double UploadKbitsPerSecond { get; set; }

    internal double UploadKbytesPerSecond { get; set; }

    internal long DevicePendingSensorLogBytes { get; set; }

    internal bool RanToCompletion { get; set; }

    internal long UploadTime { get; set; }

    internal long DownloadTime { get; set; }

    internal List<LogProcessingStatus> LogFilesProcessing { get; set; }
  }
}
