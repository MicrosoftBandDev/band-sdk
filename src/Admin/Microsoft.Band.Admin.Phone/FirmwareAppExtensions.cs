// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.FirmwareAppExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  internal static class FirmwareAppExtensions
  {
    internal static RunningAppType ToRunningAppType(this FirmwareApp firmwareApp)
    {
      switch (firmwareApp)
      {
        case FirmwareApp.OneBL:
          return RunningAppType.OneBL;
        case FirmwareApp.TwoUp:
          return RunningAppType.TwoUp;
        case FirmwareApp.App:
          return RunningAppType.App;
        case FirmwareApp.UpApp:
          return RunningAppType.UpApp;
        case FirmwareApp.Invalid:
          return RunningAppType.Invalid;
        default:
          throw new ArgumentException("Unknown FirmwareApp value.");
      }
    }
  }
}
