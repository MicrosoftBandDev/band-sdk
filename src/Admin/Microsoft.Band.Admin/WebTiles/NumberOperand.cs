// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.NumberOperand
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Band.Admin.WebTiles
{
  public class NumberOperand : Operand
  {
    private NumberOperand(string tokenValue, int position)
      : base(tokenValue, position)
    {
    }

    public static NumberOperand Create(string tokenValue, int position) => new NumberOperand(tokenValue, position);

    public override object GetValue(Dictionary<string, string> variableValues, bool stringRequired)
    {
      if (stringRequired)
        throw new InvalidDataException(CommonSR.WTContainsOperatorOnNumeric);
      return (object) Operand.RoundDoubleTo16SignificantDigits(double.Parse(this.MatchedString));
    }
  }
}
