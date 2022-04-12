using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class CloudProfileFirmwareBytes
{
    [DataMember]
    internal CloudDeviceSettingsFirmwareBytes DeviceSettings;
}
