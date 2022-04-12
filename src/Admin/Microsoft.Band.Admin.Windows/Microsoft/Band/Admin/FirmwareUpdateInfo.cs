using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class FirmwareUpdateInfo : IFirmwareUpdateInfo
{
    [DataMember]
    internal string DeviceFamily { get; set; }

    [DataMember]
    public string UniqueVersion { get; set; }

    [DataMember]
    public string FirmwareVersion { get; internal set; }

    [DataMember]
    internal string PrimaryUrl { get; set; }

    [DataMember]
    internal string FallbackUrl { get; set; }

    [DataMember]
    internal string MirrorUrl { get; set; }

    [DataMember]
    internal string HashMd5 { get; set; }

    [DataMember]
    internal string SizeInBytes { get; set; }

    [DataMember]
    public bool IsFirmwareUpdateAvailable { get; internal set; }
}
