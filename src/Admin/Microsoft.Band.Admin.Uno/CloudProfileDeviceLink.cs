using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class CloudProfileDeviceLink
{
    [DataMember(EmitDefaultValue = true)]
    internal string LastKDKSyncUpdateOn;

    [DataMember]
    internal DeviceLinkIds ApplicationSettings;

    [DataMember]
    internal DeviceLinkIds DeviceSettings;
}
