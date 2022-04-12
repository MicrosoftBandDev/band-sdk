// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.EphemerisCloudVersion
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin
{
  [DataContract]
  internal sealed class EphemerisCloudVersion
  {
    private static readonly string[] DateTimeFormats = new string[9]
    {
      "o",
      "yyyy-MM-ddTHH:mm:sszzz",
      "yyyy-MM-ddTHH:mm:ss.fzzz",
      "yyyy-MM-ddTHH:mm:ss.ffzzz",
      "yyyy-MM-ddTHH:mm:ss.fffzzz",
      "yyyy-MM-ddTHH:mm:ssZ",
      "yyyy-MM-ddTHH:mm:ss.fZ",
      "yyyy-MM-ddTHH:mm:ss.ffZ",
      "yyyy-MM-ddTHH:mm:ss.fffZ"
    };
    private DateTime? lastFileUpdatedTime;

    [DataMember]
    public string EphemerisFileHeaderDataUrl { get; set; }

    [DataMember]
    public string EphemerisProcessedFileDataUrl { get; set; }

    [DataMember(Name = "LastFileUpdatedTime")]
    private string LastFileUpdatedTimeSerialized
    {
      get => this.GetSerializedDateTimeValue(this.lastFileUpdatedTime);
      set => this.SetDeserializedDateTimeValue(value, out this.lastFileUpdatedTime);
    }

    public DateTime? LastFileUpdatedTime
    {
      get => this.lastFileUpdatedTime;
      set => this.lastFileUpdatedTime = value;
    }

    private string GetSerializedDateTimeValue(DateTime? deserialized) => !deserialized.HasValue ? (string) null : deserialized.Value.ToString(EphemerisCloudVersion.DateTimeFormats[0]);

    private void SetDeserializedDateTimeValue(string serialized, out DateTime? deserialized)
    {
      DateTime result;
      if (DateTime.TryParseExact(serialized, EphemerisCloudVersion.DateTimeFormats, (IFormatProvider) CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out result))
        deserialized = new DateTime?(result);
      else
        deserialized = new DateTime?();
    }
  }
}
