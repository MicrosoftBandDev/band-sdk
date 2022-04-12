using System;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal struct DeviceLinkIds
{
    [DataMember(EmitDefaultValue = false)]
    internal Guid ApplicationId;

    [DataMember(EmitDefaultValue = false)]
    internal Guid? PairedDeviceId;

    [DataMember(EmitDefaultValue = false)]
    internal Guid? DeviceId;

    [DataMember(EmitDefaultValue = false)]
    internal string SerialNumber;
}
