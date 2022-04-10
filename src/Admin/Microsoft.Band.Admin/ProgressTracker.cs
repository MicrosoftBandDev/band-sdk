// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.ProgressTracker
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
    public abstract class ProgressTracker
    {
        internal ProgressTrackerPrimitive[] children;
        private double lastReportedProgress;
        internal double? cachedPercentage;

        internal ProgressTracker(ProgressTrackerPrimitive[] children) => this.children = children;

        internal virtual double PercentageCompletion
        {
            get
            {
                double? nullable = this.cachedPercentage;
                if (!nullable.HasValue)
                {
                    double val1 = 0.0;
                    foreach (ProgressTrackerPrimitive child in this.children)
                        val1 += child.PercentageComplete * child.Weight;
                    if (val1 < 0.0 || val1 > 100.0)
                        Logger.Log(LogLevel.Warning, "ProgressTracker.PercentageCompletion.get: progress: {0}", (object)val1);
                    double num = Math.Min(val1, 100.0);
                    if (num > this.lastReportedProgress)
                    {
                        this.lastReportedProgress = num;
                        nullable = new double?(num);
                    }
                    else
                        nullable = new double?(this.lastReportedProgress);
                    this.cachedPercentage = nullable;
                }
                return nullable.Value;
            }
        }

        internal abstract void ChildUpdated();
    }
}
