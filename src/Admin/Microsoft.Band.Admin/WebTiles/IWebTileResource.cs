// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.IWebTileResource
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Band.Admin.WebTiles
{
    public interface IWebTileResource
    {
        bool AllowInvalidValues { get; set; }

        Dictionary<string, string> PropertyErrors { get; }

        string Url { get; set; }

        ResourceStyle Style { get; set; }

        Dictionary<string, string> Content { get; set; }

        Task<List<Dictionary<string, string>>> ResolveFeedContentMappingsAsync();

        string Username { get; set; }

        string Password { get; set; }

        Task<bool> AuthenticateAsync();

        IWebTileResourceCacheInfo CacheInfo { get; set; }

        HeaderNameValuePair[] RequestHeaders { get; set; }
    }
}
