using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class CloudDeviceSettingsFirmwareBytes
{
    [DataMember]
    internal string FirmwareByteArray;
}
