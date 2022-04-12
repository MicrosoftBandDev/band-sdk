// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CloudDeviceSettings
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin
{
  [DataContract]
  internal sealed class CloudDeviceSettings
  {
    [DataMember(EmitDefaultValue = true)]
    internal Guid DeviceId { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal string SerialNumber { get; set; }

    [DataMember(EmitDefaultValue = false, Name = "FirmwareDeviceName")]
    internal string DeviceName { get; set; }

    [DataMember(EmitDefaultValue = false, Name = "FirmwareProfileVersion")]
    internal int DeviceProfileVersion { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal string FirmwareByteArray { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal string FirmwareReserved { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal DateTime? LastReset { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal DateTime? LastSuccessfulSync { get; set; }

    [DataMember(EmitDefaultValue = false, Name = "FirmwareLocale")]
    internal CloudLocaleSettings? LocaleSettings { get; set; }

    [DataMember(EmitDefaultValue = true, Name = "RunDisplayUnits")]
    internal byte RunDisplayUnits { get; set; }

    [DataMember(EmitDefaultValue = false, Name = "IsTelemetryEnabled")]
    internal bool? TelemetryEnabled { get; set; }

    [DataMember(EmitDefaultValue = false)]
    internal Dictionary<string, string> AdditionalSettings { get; set; }
  }
}
