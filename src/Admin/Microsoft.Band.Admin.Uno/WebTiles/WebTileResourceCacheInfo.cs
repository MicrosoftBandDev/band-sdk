using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin.WebTiles;

[DataContract]
public class WebTileResourceCacheInfo : IWebTileResourceCacheInfo
{
    [DataMember(Name = "ETag")]
    public string ETag { get; set; }

    [DataMember(Name = "LastModified")]
    public string LastModified { get; set; }

    [DataMember(Name = "FeedItemIds", EmitDefaultValue = false)]
    public List<string> FeedItemIds { get; set; }
}
