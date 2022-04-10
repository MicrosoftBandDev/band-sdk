// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.BandClassExtensions
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  internal static class BandClassExtensions
  {
    internal static BandType ToBandType(this BandClass bandClass)
    {
      switch (bandClass)
      {
        case BandClass.Unknown:
          return BandType.Unknown;
        case BandClass.Cargo:
          return BandType.Cargo;
        case BandClass.Envoy:
          return BandType.Envoy;
        default:
          throw new ArgumentException("Unknown BandClass value.");
      }
    }
  }
}
