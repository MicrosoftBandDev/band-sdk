// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.StringOperand
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Collections.Generic;

namespace Microsoft.Band.Admin.WebTiles
{
  public class StringOperand : Operand
  {
    private StringOperand(string tokenValue, int position)
      : base(tokenValue, position)
    {
    }

    public static StringOperand Create(string tokenValue, int position) => new StringOperand(tokenValue, position);

    public override object GetValue(Dictionary<string, string> variableValues, bool stringRequired) => (object) StringTokenizer.RemoveEscapes(this.MatchedString.Substring(1, this.MatchedString.Length - 2));
  }
}
