// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.VariableOperand
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Band.Admin.WebTiles
{
  public class VariableOperand : Operand
  {
    private VariableOperand(string tokenValue, int position)
      : base(tokenValue, position)
    {
    }

    public static VariableOperand Create(string tokenValue, int position) => new VariableOperand(tokenValue, position);

    public override object GetValue(Dictionary<string, string> variableValues, bool stringRequired)
    {
      if (variableValues == null)
        return (object) "0";
      string key = this.MatchedString.Substring(2, this.MatchedString.Length - 4);
      string s = variableValues.ContainsKey(key) ? variableValues[key] : throw new InvalidDataException(string.Format(CommonSR.WTUndefinedVariable, new object[1]
      {
        (object) key
      }));
      double result;
      return !stringRequired && double.TryParse(s, out result) ? (object) Operand.RoundDoubleTo16SignificantDigits(result) : (object) s;
    }
  }
}
