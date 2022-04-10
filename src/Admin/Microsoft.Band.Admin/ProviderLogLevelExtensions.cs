// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.ProviderLogLevelExtensions
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  internal static class ProviderLogLevelExtensions
  {
    internal static LogLevel ToLogLevel(this ProviderLogLevel level)
    {
      switch (level)
      {
        case ProviderLogLevel.Off:
          return LogLevel.Off;
        case ProviderLogLevel.Fatal:
          return LogLevel.Fatal;
        case ProviderLogLevel.Error:
          return LogLevel.Error;
        case ProviderLogLevel.Warning:
          return LogLevel.Warning;
        case ProviderLogLevel.Info:
          return LogLevel.Info;
        case ProviderLogLevel.Performance:
          return LogLevel.Performance;
        case ProviderLogLevel.Verbose:
          return LogLevel.Verbose;
        default:
          throw new ArgumentException("Unknown LogLevel value.");
      }
    }
  }
}
