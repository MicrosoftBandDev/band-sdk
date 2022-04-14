using System.Collections.Generic;

namespace Microsoft.Band.Admin;

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
