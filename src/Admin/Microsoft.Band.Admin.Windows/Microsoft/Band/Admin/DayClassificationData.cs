using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct DayClassificationData
{
    internal DayClassificationType PredictedClass;

    internal byte SequenceId;
}
