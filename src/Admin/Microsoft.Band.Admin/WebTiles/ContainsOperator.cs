// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.ContainsOperator
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin.WebTiles
{
    internal class ContainsOperator : BinaryOperator
    {
        private ContainsOperator(string tokenValue, int position)
          : base(tokenValue, position)
        {
        }

        public static ContainsOperator Create(string tokenValue, int position) => new ContainsOperator(tokenValue, position);

        public override bool Compare(object leftOperand, object rightOperand)
        {
            if (!(leftOperand is string))
                leftOperand = (object)leftOperand.ToString();
            if (!(rightOperand is string))
                rightOperand = (object)rightOperand.ToString();
            return ((string)leftOperand).Contains((string)rightOperand);
        }
    }
}
