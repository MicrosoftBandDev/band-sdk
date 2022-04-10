// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LogProcessing.LogProcessingUpdatedEventArgs
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;

namespace Microsoft.Band.Admin.LogProcessing
{
    public sealed class LogProcessingUpdatedEventArgs : EventArgs
    {
        public List<LogProcessingStatus> CompletedFiles;
        public List<LogProcessingStatus> ProcessingFiles;
        public List<LogProcessingStatus> NotRecognizedFiles;

        public LogProcessingUpdatedEventArgs(
          IEnumerable<LogProcessingStatus> Completed,
          IEnumerable<LogProcessingStatus> Processing,
          IEnumerable<LogProcessingStatus> NotRecognized)
        {
            this.CompletedFiles = Completed == null ? new List<LogProcessingStatus>() : new List<LogProcessingStatus>(Completed);
            this.ProcessingFiles = Processing == null ? new List<LogProcessingStatus>() : new List<LogProcessingStatus>(Processing);
            this.NotRecognizedFiles = NotRecognized == null ? new List<LogProcessingStatus>() : new List<LogProcessingStatus>(NotRecognized);
        }
    }
}
