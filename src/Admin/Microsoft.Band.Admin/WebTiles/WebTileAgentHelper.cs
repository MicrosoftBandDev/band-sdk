// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.WebTileAgentHelper
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;

namespace Microsoft.Band.Admin.WebTiles
{
    public class WebTileAgentHelper
    {
        private const string OrganizationMicrosoft = "Microsoft";
        private const string XWebTileAgentHeaderName = "X-WEBTILE-AGENT";
        private static Dictionary<string, HeaderNameValuePair[]> MSUrlToHeadersTable = new Dictionary<string, HeaderNameValuePair[]>()
    {
      {
        "prodcus0dep.blob.core.windows.net",
        new HeaderNameValuePair[1]
        {
          new HeaderNameValuePair("X-WEBTILE-AGENT", "Microsoft")
        }
      },
      {
        "intcus0devweb.blob.core.windows.net",
        new HeaderNameValuePair[1]
        {
          new HeaderNameValuePair("X-WEBTILE-AGENT", "Microsoft")
        }
      },
      {
        "intcus0dep.blob.core.windows.net",
        new HeaderNameValuePair[1]
        {
          new HeaderNameValuePair("X-WEBTILE-AGENT", "Microsoft")
        }
      }
    };

        public static HeaderNameValuePair[] GetAgentHeadersForUrl(
          string url,
          string organization)
        {
            if (organization != null && string.Compare(organization, "Microsoft", StringComparison.OrdinalIgnoreCase) == 0)
            {
                string host = new Uri(url).Host;
                if (host != null && WebTileAgentHelper.MSUrlToHeadersTable.ContainsKey(host))
                    return WebTileAgentHelper.MSUrlToHeadersTable[host];
            }
            return (HeaderNameValuePair[])null;
        }
    }
}
