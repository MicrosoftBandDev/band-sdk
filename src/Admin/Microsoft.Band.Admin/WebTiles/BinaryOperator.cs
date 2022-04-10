// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.BinaryOperator
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.IO;

namespace Microsoft.Band.Admin.WebTiles
{
    internal abstract class BinaryOperator : Token
    {
        protected BinaryOperator(string tokenValue, int position)
          : base(tokenValue, position)
        {
        }

        private static bool TryConvertToNumber(string input, out object result)
        {
            double result1;
            bool number;
            if (double.TryParse(input, out result1))
            {
                result = (object)Operand.RoundDoubleTo16SignificantDigits(result1);
                number = true;
            }
            else
            {
                result = (object)input;
                number = false;
            }
            return number;
        }

        public abstract bool Compare(object leftOperand, object rightOperand);

        protected bool Compare(
          object leftOperand,
          object rightOperand,
          BinaryOperator.CompareOperation compareOperation)
        {
            if ((object)leftOperand.GetType() != (object)rightOperand.GetType())
            {
                bool flag = false;
                if (leftOperand is string)
                    flag = BinaryOperator.TryConvertToNumber((string)leftOperand, out leftOperand);
                else if (rightOperand is string)
                    flag = BinaryOperator.TryConvertToNumber((string)rightOperand, out rightOperand);
                if (!flag)
                    return !compareOperation(0) && compareOperation(-1) && compareOperation(1);
            }
            int difference;
            switch (leftOperand)
            {
                case double _ when rightOperand is double num2:
                    double num1 = (double)leftOperand - num2;
                    difference = num1 != 0.0 ? (num1 >= 0.0 ? 1 : -1) : 0;
                    break;
                case string _ when rightOperand is string:
                    difference = string.Compare((string)leftOperand, (string)rightOperand);
                    break;
                default:
                    throw new InvalidDataException(CommonSR.WTUnexpectedTypeInCompare);
            }
            return compareOperation(difference);
        }

        public delegate bool CompareOperation(int difference);
    }
}
