namespace Microsoft.Band.Admin;

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
