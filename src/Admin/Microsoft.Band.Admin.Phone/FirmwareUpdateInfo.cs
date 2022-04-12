// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.FirmwareUpdateInfo
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Runtime.Serialization;

namespace Microsoft.Band.Admin
{
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
}
