using System;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class CloudDataResource
{
    [DataMember(EmitDefaultValue = false)]
    public string UploadId { get; set; }

    public LogFileTypes LogType { get; set; }

    [DataMember(Name = "LogType", EmitDefaultValue = false)]
    public string LogTypeString
    {
        get
        {
            if (LogType == LogFileTypes.Unknown)
            {
                return null;
            }
            return LogType.ToString();
        }
        set
        {
            LogType = (Enum.TryParse<LogFileTypes>(value, ignoreCase: true, out var result) ? result : LogFileTypes.Unknown);
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public string Location { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public bool Committed { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public UploadMetaData UploadMetadata { get; set; }

    public LogUploadStatus UploadStatus { get; set; }

    [DataMember(Name = "UploadStatus", EmitDefaultValue = false)]
    private string UploadStatusString
    {
        get
        {
            if (UploadStatus == LogUploadStatus.Unknown)
            {
                return null;
            }
            return UploadStatus.ToString();
        }
        set
        {
            UploadStatus = (Enum.TryParse<LogUploadStatus>(value, ignoreCase: true, out var result) ? result : LogUploadStatus.Unknown);
        }
    }

    [DataMember(EmitDefaultValue = false)]
    public float UploadSizeInKb { get; set; }
}
