// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LoggerProvider
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;

namespace Microsoft.Band.Admin
{
  internal class LoggerProvider : ILoggerProvider
  {
    public void Log(ProviderLogLevel level, string message, object[] args, [CallerMemberName] string callerName = null) => Logger.Log(level.ToLogLevel(), message, args);

    public void LogException(ProviderLogLevel level, Exception e, [CallerMemberName] string callerName = null) => Logger.LogException(level.ToLogLevel(), e);

    public void LogWebException(ProviderLogLevel level, WebException e, [CallerMemberName] string callerName = null) => Logger.LogWebException(level.ToLogLevel(), e);

    public void LogException(
      ProviderLogLevel level,
      Exception e,
      string message,
      object[] args,
      [CallerMemberName] string callerName = null)
    {
      Logger.LogException(level.ToLogLevel(), e, message, args);
    }

    public void PerfStart(string eventName) => Logger.PerfStart(eventName);

    public void PerfEnd(string eventName) => Logger.PerfEnd(eventName);

    public void TelemetryEvent(
      string eventName,
      IDictionary<string, string> properties,
      IDictionary<string, double> metrics)
    {
      Logger.TelemetryEvent(eventName, properties, metrics);
    }
  }
}
