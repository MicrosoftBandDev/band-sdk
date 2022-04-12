// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LoggerMetadata
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin
{
  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  internal struct LoggerMetadata
  {
    internal uint ChunkSize;
    internal uint SequenceNumber;
  }
}
