// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.FitnessPlanWorkout
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin
{
  internal struct FitnessPlanWorkout
  {
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal byte[] Id;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
    internal byte[] Name;
    internal byte NameLength;
    internal ushort CircuitCount;
    internal CompletionType CompletionType;
    internal uint CompletionValue;
  }
}
