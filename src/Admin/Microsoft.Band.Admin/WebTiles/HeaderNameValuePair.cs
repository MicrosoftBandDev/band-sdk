// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.HeaderNameValuePair
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin.WebTiles
{
  public struct HeaderNameValuePair
  {
    public string name;
    public string value;

    public HeaderNameValuePair(string name, string value)
    {
      this.name = name;
      this.value = value;
    }
  }
}
