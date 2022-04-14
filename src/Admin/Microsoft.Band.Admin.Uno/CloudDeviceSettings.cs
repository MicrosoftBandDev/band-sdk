using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class CloudDeviceSettings
{
    [DataMember(EmitDefaultValue = true)]
    internal Guid DeviceId { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal string SerialNumber { get; set; }

    [DataMember(Name = "FirmwareDeviceName", EmitDefaultValue = false)]
    internal string DeviceName { get; set; }

    [DataMember(Name = "FirmwareProfileVersion", EmitDefaultValue = false)]
    internal int DeviceProfileVersion { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal string FirmwareByteArray { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal string FirmwareReserved { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal DateTime? LastReset { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal DateTime? LastSuccessfulSync { get; set; }

    [DataMember(Name = "FirmwareLocale", EmitDefaultValue = false)]
    internal CloudLocaleSettings? LocaleSettings { get; set; }

    [DataMember(Name = "RunDisplayUnits", EmitDefaultValue = true)]
    internal byte RunDisplayUnits { get; set; }

    [DataMember(Name = "IsTelemetryEnabled", EmitDefaultValue = false)]
    internal bool? TelemetryEnabled { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal Dictionary<string, string> AdditionalSettings { get; set; }
}
