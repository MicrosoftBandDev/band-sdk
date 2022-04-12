// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CloudDataResource
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin
{
  [DataContract]
  internal sealed class CloudDataResource
  {
    [DataMember(EmitDefaultValue = false)]
    public string UploadId { get; set; }

    public LogFileTypes LogType { get; set; }

    [DataMember(EmitDefaultValue = false, Name = "LogType")]
    public string LogTypeString
    {
      get => this.LogType == LogFileTypes.Unknown ? (string) null : this.LogType.ToString();
      set
      {
        LogFileTypes result;
        this.LogType = Enum.TryParse<LogFileTypes>(value, true, out result) ? result : LogFileTypes.Unknown;
      }
    }

    [DataMember(EmitDefaultValue = false)]
    public string Location { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public bool Committed { get; set; }

    [DataMember(EmitDefaultValue = false)]
    public UploadMetaData UploadMetadata { get; set; }

    public LogUploadStatus UploadStatus { get; set; }

    [DataMember(EmitDefaultValue = false, Name = "UploadStatus")]
    private string UploadStatusString
    {
      get => this.UploadStatus == LogUploadStatus.Unknown ? (string) null : this.UploadStatus.ToString();
      set
      {
        LogUploadStatus result;
        this.UploadStatus = Enum.TryParse<LogUploadStatus>(value, true, out result) ? result : LogUploadStatus.Unknown;
      }
    }

    [DataMember(EmitDefaultValue = false)]
    public float UploadSizeInKb { get; set; }
  }
}
