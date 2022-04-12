// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LogSyncResultExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

namespace Microsoft.Band.Admin
{
  internal static class LogSyncResultExtensions
  {
    internal static void CopyToSyncResult(this LogSyncResult logSyncResult, SyncResult syncResult)
    {
      syncResult.DownloadedSensorLogBytes = logSyncResult.DownloadedSensorLogBytes;
      syncResult.UploadedSensorLogBytes = logSyncResult.UploadedSensorLogBytes;
      syncResult.DownloadKbitsPerSecond = logSyncResult.DownloadKbitsPerSecond;
      syncResult.DownloadKbytesPerSecond = logSyncResult.DownloadKbytesPerSecond;
      syncResult.UploadKbitsPerSecond = logSyncResult.UploadKbitsPerSecond;
      syncResult.UploadKbytesPerSecond = logSyncResult.UploadKbytesPerSecond;
      syncResult.DevicePendingSensorLogBytes = logSyncResult.DevicePendingSensorLogBytes;
      syncResult.RanToCompletion = logSyncResult.RanToCompletion;
      syncResult.LogFilesProcessing = logSyncResult.LogFilesProcessing;
      syncResult.UploadTime = logSyncResult.UploadTime;
      syncResult.DownloadTime = logSyncResult.DownloadTime;
    }
  }
}
