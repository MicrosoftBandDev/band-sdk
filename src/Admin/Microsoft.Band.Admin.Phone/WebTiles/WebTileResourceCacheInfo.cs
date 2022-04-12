// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.WebTileResourceCacheInfo
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin.WebTiles
{
  [DataContract]
  public class WebTileResourceCacheInfo : IWebTileResourceCacheInfo
  {
    [DataMember(Name = "ETag")]
    public string ETag { get; set; }

    [DataMember(Name = "LastModified")]
    public string LastModified { get; set; }

    [DataMember(EmitDefaultValue = false, Name = "FeedItemIds")]
    public List<string> FeedItemIds { get; set; }
  }
}
