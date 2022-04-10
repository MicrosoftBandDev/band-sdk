// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.RegexMatcher
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Text.RegularExpressions;

namespace Microsoft.Band.Admin.WebTiles
{
    public class RegexMatcher
    {
        private readonly Regex regex;

        public RegexMatcher(string regex, RegexOptions options) => this.regex = new Regex(string.Format("\\G{0}", new object[1]
        {
      (object) regex
        }), options);

        public int Match(string input, int startat = 0)
        {
            System.Text.RegularExpressions.Match match = this.regex.Match(input, startat);
            return !match.Success ? 0 : match.Length;
        }

        public override string ToString() => this.regex.ToString();
    }
}
