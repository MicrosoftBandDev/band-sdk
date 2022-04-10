// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.TraceListenerBase
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;

namespace Microsoft.Band.Admin
{
    public abstract class TraceListenerBase
    {
        public virtual void Log(LogLevel level, string message, params object[] args)
        {
        }

        public virtual void LogException(
          LogLevel level,
          Exception e,
          string message,
          params object[] args)
        {
        }

        public virtual void PerfStart(string eventName)
        {
        }

        public virtual void PerfEnd(string eventName)
        {
        }

        public virtual void TelemetryEvent(
          string eventName,
          IDictionary<string, string> properties,
          IDictionary<string, double> metrics)
        {
        }

        public virtual void TelemetryPageView(string pagePath)
        {
        }

        public virtual ICancellableTransaction TelemetryTimedEvent(
          string eventName,
          IDictionary<string, string> properties,
          IDictionary<string, double> metrics)
        {
            return (ICancellableTransaction)null;
        }
    }
}
