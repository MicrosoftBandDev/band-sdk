// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Config_PCBPermanent
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin
{
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  internal struct Config_PCBPermanent
  {
    internal byte PCBID;
    internal byte ConfigurationRecordVersion;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    internal byte[] SerialNumber;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    internal byte[] BTMACAddress;
    [MarshalAs(UnmanagedType.Bool)]
    internal bool IsPermanentyConfigValid;
    [MarshalAs(UnmanagedType.Bool)]
    internal bool IsLocked;
  }
}
