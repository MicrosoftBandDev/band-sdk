// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.KeyboardCmdSample
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin
{
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  internal struct KeyboardCmdSample
  {
    internal const int MAX_NUM_OF_CANDIDATES = 4;
    internal const int MAX_KBDCMD_DATA_LEN = 400;
    internal KeyboardMessageType KeyboardMsgType;
    internal byte NumOfCandidates;
    internal byte WordIndex;
    internal uint DataLength;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 400)]
    internal byte[] Datafield;
  }
}
