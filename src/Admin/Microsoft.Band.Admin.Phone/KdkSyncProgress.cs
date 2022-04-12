// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.KdkSyncProgress
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Collections.Generic;

namespace Microsoft.Band.Admin
{
  internal sealed class KdkSyncProgress : ProgressTracker
  {
    private static readonly Dictionary<SyncTasks, double> TaskWeights = new Dictionary<SyncTasks, double>();
    private static readonly double TotalTaskWeight;
    private IProgress<SyncProgress> reporter;
    private SyncState state;

    static KdkSyncProgress()
    {
      KdkSyncProgress.TaskWeights.Add(SyncTasks.TimeAndTimeZone, 4.0);
      KdkSyncProgress.TaskWeights.Add(SyncTasks.EphemerisFile, 9.0);
      KdkSyncProgress.TaskWeights.Add(SyncTasks.TimeZoneFile, 8.0);
      KdkSyncProgress.TaskWeights.Add(SyncTasks.DeviceCrashDump, 8.0);
      KdkSyncProgress.TaskWeights.Add(SyncTasks.DeviceInstrumentation, 9.0);
      KdkSyncProgress.TaskWeights.Add(SyncTasks.UserProfileFirmwareBytes | SyncTasks.UserProfile, 4.0);
      KdkSyncProgress.TaskWeights.Add(SyncTasks.SensorLog, 58.0);
      KdkSyncProgress.TaskWeights.Add(SyncTasks.WebTiles | SyncTasks.WebTilesForced, 0.0);
      foreach (KeyValuePair<SyncTasks, double> taskWeight in KdkSyncProgress.TaskWeights)
        KdkSyncProgress.TotalTaskWeight += taskWeight.Value;
    }

    internal SyncState State
    {
      get => this.state;
      private set
      {
        if (this.state == value)
          return;
        this.state = value;
        this.reporter.Report(new SyncProgress(this.PercentageCompletion, this.State));
      }
    }

    internal KdkSyncProgress(IProgress<SyncProgress> reporter, SyncTasks syncTasks)
      : base(new ProgressTrackerPrimitive[8])
    {
      KdkSyncProgress kdkSyncProgress = this;
      this.reporter = reporter == null ? (IProgress<SyncProgress>) new DummyProgress<SyncProgress>() : reporter;
      this.State = SyncState.NotStarted;
      Func<SyncTasks, bool> Need = (Func<SyncTasks, bool>) (tasksToCheckIfIncluded => (uint) (syncTasks & tasksToCheckIfIncluded) > 0U);
      double weightOfRequested = 0.0;
      Action<SyncTasks> action = (Action<SyncTasks>) (tasksToCheckIfIncluded => weightOfRequested += Need(tasksToCheckIfIncluded) ? KdkSyncProgress.TaskWeights[tasksToCheckIfIncluded] : 0.0);
      action(SyncTasks.TimeAndTimeZone);
      action(SyncTasks.EphemerisFile);
      action(SyncTasks.TimeZoneFile);
      action(SyncTasks.DeviceCrashDump);
      action(SyncTasks.DeviceInstrumentation);
      action(SyncTasks.UserProfileFirmwareBytes | SyncTasks.UserProfile);
      action(SyncTasks.SensorLog);
      action(SyncTasks.WebTiles | SyncTasks.WebTilesForced);
      Func<SyncTasks, ProgressTrackerPrimitive> func = (Func<SyncTasks, ProgressTrackerPrimitive>) (tasksToCheckIfIncluded => new ProgressTrackerPrimitive((ProgressTracker) closure_0, Need(tasksToCheckIfIncluded) ? KdkSyncProgress.TaskWeights[tasksToCheckIfIncluded] * KdkSyncProgress.TotalTaskWeight / weightOfRequested : 0.0));
      this.CurrentTimeAndTimeZoneProgress = func(SyncTasks.TimeAndTimeZone);
      this.EphemerisProgress = func(SyncTasks.EphemerisFile);
      this.TimeZoneProgress = func(SyncTasks.TimeZoneFile);
      this.DeviceCrashDumpProgress = func(SyncTasks.DeviceCrashDump);
      this.DeviceInstrumentationProgress = func(SyncTasks.DeviceInstrumentation);
      this.UserProfileProgress = func(SyncTasks.UserProfileFirmwareBytes | SyncTasks.UserProfile);
      this.LogSyncProgress = func(SyncTasks.SensorLog);
      this.WebTilesProgress = func(SyncTasks.WebTiles | SyncTasks.WebTilesForced);
    }

    internal override void ChildUpdated()
    {
      this.cachedPercentage = new double?();
      this.reporter.Report(new SyncProgress(this.PercentageCompletion, this.State));
    }

    internal void SetState(SyncState newState) => this.State = newState;

    internal ProgressTrackerPrimitive CurrentTimeAndTimeZoneProgress
    {
      get => this.children[0];
      private set => this.children[0] = value;
    }

    internal ProgressTrackerPrimitive EphemerisProgress
    {
      get => this.children[1];
      private set => this.children[1] = value;
    }

    internal ProgressTrackerPrimitive TimeZoneProgress
    {
      get => this.children[2];
      private set => this.children[2] = value;
    }

    internal ProgressTrackerPrimitive DeviceCrashDumpProgress
    {
      get => this.children[3];
      private set => this.children[3] = value;
    }

    internal ProgressTrackerPrimitive DeviceInstrumentationProgress
    {
      get => this.children[4];
      private set => this.children[4] = value;
    }

    internal ProgressTrackerPrimitive UserProfileProgress
    {
      get => this.children[5];
      private set => this.children[5] = value;
    }

    internal ProgressTrackerPrimitive LogSyncProgress
    {
      get => this.children[6];
      private set => this.children[6] = value;
    }

    internal ProgressTrackerPrimitive WebTilesProgress
    {
      get => this.children[7];
      private set => this.children[7] = value;
    }
  }
}
