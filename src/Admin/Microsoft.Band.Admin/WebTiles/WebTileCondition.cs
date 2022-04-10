// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.WebTileCondition
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Band.Admin.WebTiles
{
    public class WebTileCondition
    {
        private static TokenDefinition[] expressionTokenDefinitions = new TokenDefinition[13]
        {
      new TokenDefinition("\\s+"),
      new TokenDefinition("contains", new CreateTokenDelegate(ContainsOperator.Create)),
      new TokenDefinition("==", new CreateTokenDelegate(EqualOperator.Create)),
      new TokenDefinition("!=", new CreateTokenDelegate(NotEqualOperator.Create)),
      new TokenDefinition("<=", new CreateTokenDelegate(LessThanOrEqualOperator.Create)),
      new TokenDefinition("<", new CreateTokenDelegate(LessThanOperator.Create)),
      new TokenDefinition(">=", new CreateTokenDelegate(GreaterThanOrEqualOperator.Create)),
      new TokenDefinition(">", new CreateTokenDelegate(GreaterThanOperator.Create)),
      new TokenDefinition("true", new CreateTokenDelegate(BooleanTrue.Create)),
      new TokenDefinition("[-+]?\\d*\\.\\d+([eE][-+]?\\d+)?", new CreateTokenDelegate(NumberOperand.Create)),
      new TokenDefinition("[-+]?\\d+", new CreateTokenDelegate(NumberOperand.Create)),
      new TokenDefinition("([\"'])(?:\\\\\\1|.)*?\\1", new CreateTokenDelegate(StringOperand.Create)),
      new TokenDefinition("{{[a-zA-Z_]\\w*}}", new CreateTokenDelegate(VariableOperand.Create))
        };
        private Dictionary<string, string> variableValues;
        private StringTokenizer tokenizedExpression;
        private int nextTokenIndex;
        private Token currentToken;

        public WebTileCondition(Dictionary<string, string> variableValues)
        {
            this.variableValues = variableValues;
            this.tokenizedExpression = new StringTokenizer(WebTileCondition.expressionTokenDefinitions);
        }

        public bool ComputeValue(string expression)
        {
            this.tokenizedExpression.Tokenize(expression);
            this.nextTokenIndex = 0;
            return this.GetTokenizedExpressionValue();
        }

        private bool NextToken()
        {
            if (this.nextTokenIndex >= this.tokenizedExpression.TokenList.Count)
                return false;
            this.currentToken = this.tokenizedExpression.TokenList[this.nextTokenIndex++];
            return true;
        }

        private bool GetTokenizedExpressionValue()
        {
            if (!this.NextToken())
                throw new InvalidDataException(CommonSR.WTExpressionHasNoTokens);
            if (this.currentToken is BooleanTrue)
            {
                if (this.NextToken())
                    throw new InvalidDataException(CommonSR.WTExtraInputAfterTrue);
                return true;
            }
            Operand operand1 = this.currentToken is Operand ? (Operand)this.currentToken : throw new InvalidDataException(CommonSR.WTExpressionMissingFirstOperand);
            string matchedString1 = this.currentToken.MatchedString;
            BinaryOperator binaryOperator = this.NextToken() && this.currentToken is BinaryOperator ? this.currentToken as BinaryOperator : throw new InvalidDataException(CommonSR.WTExpressionMissingOperator);
            Operand operand2 = this.NextToken() && this.currentToken is Operand ? (Operand)this.currentToken : throw new InvalidDataException(CommonSR.WTExpressionMissingSecondOperand);
            string matchedString2 = this.currentToken.MatchedString;
            if (this.NextToken())
                throw new InvalidDataException(CommonSR.WTExpressionHasExtraneousInput);
            bool stringRequired = binaryOperator is ContainsOperator;
            return binaryOperator.Compare(operand1.GetValue(this.variableValues, stringRequired), operand2.GetValue(this.variableValues, stringRequired));
        }
    }
}
