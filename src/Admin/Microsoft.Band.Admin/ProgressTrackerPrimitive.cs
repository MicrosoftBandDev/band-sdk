// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.ProgressTrackerPrimitive
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  public class ProgressTrackerPrimitive : ProgressTrackerPrimitiveBase
  {
    private ProgressTracker parent;
    private int stepsTotal;
    private int stepsCompleted;
    private double weight;
    private double? cachedPercentage;

    public ProgressTrackerPrimitive(ProgressTracker parent, double weight = 1.0)
    {
      this.parent = parent != null ? parent : throw new ArgumentException(nameof (parent));
      this.weight = weight;
    }

    public double Weight
    {
      get => this.weight;
      set
      {
        if (this.weight == value)
          return;
        this.weight = value;
        this.cachedPercentage = new double?();
        this.parent.ChildUpdated();
      }
    }

    public double PercentageComplete
    {
      get
      {
        double? nullable = this.cachedPercentage;
        if (!nullable.HasValue)
        {
          if (this.stepsTotal == 0)
            return 0.0;
          if (this.stepsCompleted > this.stepsTotal)
          {
            Logger.Log(LogLevel.Warning, "ProgressTrackerPrimitive.PercentageComplete.get: StepsTotal: {0}, StepsCompleted: {1}", (object) this.stepsTotal, (object) this.stepsCompleted);
            return 1.0;
          }
          nullable = new double?((double) this.stepsCompleted / (double) this.stepsTotal);
          this.cachedPercentage = nullable;
        }
        return nullable.Value;
      }
    }

    public override void AddStepsTotal(int steps) => this.AddSteps(ref this.stepsTotal, steps);

    public override void AddStepsCompleted(int steps) => this.AddSteps(ref this.stepsCompleted, steps);

    private void AddSteps(ref int stepsToUpdate, int steps)
    {
      if (steps == 0)
        return;
      int num = stepsToUpdate + steps;
      stepsToUpdate = num >= 0 ? num : throw new ArgumentException(nameof (steps));
      this.cachedPercentage = new double?();
      this.parent.ChildUpdated();
    }

    public override void Complete()
    {
      if (this.stepsCompleted >= this.stepsTotal && this.stepsTotal != 0)
        return;
      if (this.stepsTotal == 0)
        this.stepsTotal = 1;
      this.stepsCompleted = this.stepsTotal;
      this.cachedPercentage = new double?();
      this.parent.ChildUpdated();
    }
  }
}
