using System;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
public sealed class UploadMetaData
{
    [DataMember]
    public string HostOS { get; set; }

    [DataMember]
    public string HostOSVersion { get; set; }

    [DataMember]
    public string HostAppVersion { get; set; }

    [DataMember]
    public string DeviceMetadataHint { get; set; }

    [DataMember]
    public string DeviceVersion { get; set; }

    public LogCompressionAlgorithm? CompressionAlgorithm { get; set; }

    [DataMember(Name = "CompressionAlgorithm")]
    private string CompressionAlgorithmString
    {
        get
        {
            if (!CompressionAlgorithm.HasValue)
            {
                return null;
            }
            return CompressionAlgorithm.ToString();
        }
        set
        {
            CompressionAlgorithm = (Enum.TryParse<LogCompressionAlgorithm>(value, ignoreCase: true, out var result) ? new LogCompressionAlgorithm?(result) : null);
        }
    }

    [DataMember]
    public string CompressedFileCRC { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public int? StartSequenceId { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public int? EndSequenceId { get; set; }

    [DataMember]
    public string DeviceId { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public string DeviceSerialNumber { get; set; }

    [DataMember]
    public int? LogVersion { get; set; }

    [DataMember]
    public int? UTCTimeZoneOffsetInMinutes { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public string PcbId { get; set; }

    public UploadMetaData()
    {
        CompressionAlgorithm = LogCompressionAlgorithm.uncompressed;
    }
}
