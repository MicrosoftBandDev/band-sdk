// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.WebTileCacheInfo
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin.WebTiles
{
  [DataContract]
  internal class WebTileCacheInfo
  {
    private Dictionary<string, WebTileResourceCacheInfo> resourceCacheInfo;
    private Dictionary<string, string> variableMappings;

    [DataMember(Name = "ResourceCacheInfo")]
    public Dictionary<string, WebTileResourceCacheInfo> ResourceCacheInfo
    {
      get => this.resourceCacheInfo;
      set => this.resourceCacheInfo = value;
    }

    [DataMember(EmitDefaultValue = false, Name = "VariableMappings")]
    public Dictionary<string, string> VariableMappings
    {
      get => this.variableMappings;
      set => this.variableMappings = value;
    }

    [DataMember(Name = "LastUpdateError")]
    public bool LastUpdateError { get; set; }
  }
}
