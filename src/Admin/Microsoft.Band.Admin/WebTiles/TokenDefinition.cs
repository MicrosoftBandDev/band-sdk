// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.TokenDefinition
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Text.RegularExpressions;

namespace Microsoft.Band.Admin.WebTiles
{
    public class TokenDefinition
    {
        public RegexMatcher Matcher { get; private set; }

        public CreateTokenDelegate CreateToken { get; private set; }

        public TokenDefinition(string regex, CreateTokenDelegate createToken = null, RegexOptions options = RegexOptions.None)
        {
            this.Matcher = new RegexMatcher(regex, options);
            this.CreateToken = createToken;
        }
    }
}
