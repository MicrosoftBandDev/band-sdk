// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.Token
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin.WebTiles
{
  public class Token
  {
    public string MatchedString { get; private set; }

    public int Position { get; private set; }

    public Token(string matchedString, int position)
    {
      this.MatchedString = matchedString;
      this.Position = position;
    }
  }
}
