using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin.WebTiles;

[DataContract]
internal class WebTileCacheInfo
{
    private Dictionary<string, WebTileResourceCacheInfo> resourceCacheInfo;

    private Dictionary<string, string> variableMappings;

    [DataMember(Name = "ResourceCacheInfo")]
    public Dictionary<string, WebTileResourceCacheInfo> ResourceCacheInfo
    {
        get
        {
            return resourceCacheInfo;
        }
        set
        {
            resourceCacheInfo = value;
        }
    }

    [DataMember(Name = "VariableMappings", EmitDefaultValue = false)]
    public Dictionary<string, string> VariableMappings
    {
        get
        {
            return variableMappings;
        }
        set
        {
            variableMappings = value;
        }
    }

    [DataMember(Name = "LastUpdateError")]
    public bool LastUpdateError { get; set; }
}
