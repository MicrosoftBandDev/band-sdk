using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct SleepClassificationData
{
    internal SleepClassificationType PredictedClass;
}
