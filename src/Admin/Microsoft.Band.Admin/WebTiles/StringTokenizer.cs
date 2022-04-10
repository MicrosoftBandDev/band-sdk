// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.StringTokenizer
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Band.Admin.WebTiles
{
  public class StringTokenizer
  {
    private TokenDefinition[] tokenDefinitions;
    private string input;
    private int nextTokenPosition;

    public StringTokenizer(TokenDefinition[] tokenDefinitions) => this.tokenDefinitions = tokenDefinitions;

    public int CurrentTokenPosition { get; private set; }

    public string CurrentTokenValue { get; private set; }

    public TokenDefinition CurrentMatchedDefinition { get; private set; }

    public bool GetNextToken()
    {
      if (this.input == null || this.nextTokenPosition >= this.input.Length)
        return false;
      foreach (TokenDefinition tokenDefinition in this.tokenDefinitions)
      {
        int length = tokenDefinition.Matcher.Match(this.input, this.nextTokenPosition);
        if (length > 0)
        {
          this.CurrentMatchedDefinition = tokenDefinition;
          this.CurrentTokenPosition = this.nextTokenPosition;
          this.CurrentTokenValue = this.input.Substring(this.nextTokenPosition, length);
          this.nextTokenPosition += length;
          return true;
        }
      }
      throw new InvalidDataException(string.Format(CommonSR.WTUnrecognizedToken, new object[1]
      {
        (object) this.nextTokenPosition
      }));
    }

    public List<Token> TokenList { get; private set; }

    public void Tokenize(string input)
    {
      this.TokenList = new List<Token>();
      this.input = input;
      this.nextTokenPosition = 0;
      while (this.GetNextToken())
      {
        if (this.CurrentMatchedDefinition.CreateToken != null)
          this.TokenList.Add(this.CurrentMatchedDefinition.CreateToken(this.CurrentTokenValue, this.CurrentTokenPosition));
      }
    }

    public static string RemoveEscapes(string input) => Regex.Replace(input, "\\\\(.)", (MatchEvaluator) (m => m.Groups[1].Value));
  }
}
