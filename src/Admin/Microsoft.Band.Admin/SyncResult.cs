// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.SyncResult
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Collections.Generic;

namespace Microsoft.Band.Admin
{
  public sealed class SyncResult
  {
    public long DownloadedSensorLogBytes { get; set; }

    public long UploadedSensorLogBytes { get; set; }

    public double DownloadKbitsPerSecond { get; set; }

    public double DownloadKbytesPerSecond { get; set; }

    public double UploadKbitsPerSecond { get; set; }

    public double UploadKbytesPerSecond { get; set; }

    public long TotalTimeElapsed { get; set; }

    public long UploadTime { get; set; }

    public long DownloadTime { get; set; }

    public long DevicePendingSensorLogBytes { get; set; }

    public bool RanToCompletion { get; set; }

    public List<LogProcessingStatus> LogFilesProcessing { get; set; }

    public long? TimeZoneElapsed { get; set; }

    public long? EphemerisCheckElapsed { get; set; }

    public long? EphemerisUpdateElapsed { get; set; }

    public long? CrashDumpElapsed { get; set; }

    public long? WebTilesElapsed { get; set; }

    public long? SensorLogElapsed { get; set; }

    public long? UserProfileFirmwareBytesElapsed { get; set; }

    public long? UserProfileFullElapsed { get; set; }
  }
}
