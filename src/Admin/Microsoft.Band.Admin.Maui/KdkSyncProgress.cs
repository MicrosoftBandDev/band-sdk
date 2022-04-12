using System;
using System.Collections.Generic;

namespace Microsoft.Band.Admin;

internal sealed class KdkSyncProgress : ProgressTracker
{
    private static readonly Dictionary<SyncTasks, double> TaskWeights;

    private static readonly double TotalTaskWeight;

    private IProgress<SyncProgress> reporter;

    private SyncState state;

    internal SyncState State
    {
        get
        {
            return state;
        }
        private set
        {
            if (state != value)
            {
                state = value;
                reporter.Report(new SyncProgress(PercentageCompletion, State));
            }
        }
    }

    internal ProgressTrackerPrimitive CurrentTimeAndTimeZoneProgress
    {
        get
        {
            return children[0];
        }
        private set
        {
            children[0] = value;
        }
    }

    internal ProgressTrackerPrimitive EphemerisProgress
    {
        get
        {
            return children[1];
        }
        private set
        {
            children[1] = value;
        }
    }

    internal ProgressTrackerPrimitive TimeZoneProgress
    {
        get
        {
            return children[2];
        }
        private set
        {
            children[2] = value;
        }
    }

    internal ProgressTrackerPrimitive DeviceCrashDumpProgress
    {
        get
        {
            return children[3];
        }
        private set
        {
            children[3] = value;
        }
    }

    internal ProgressTrackerPrimitive DeviceInstrumentationProgress
    {
        get
        {
            return children[4];
        }
        private set
        {
            children[4] = value;
        }
    }

    internal ProgressTrackerPrimitive UserProfileProgress
    {
        get
        {
            return children[5];
        }
        private set
        {
            children[5] = value;
        }
    }

    internal ProgressTrackerPrimitive LogSyncProgress
    {
        get
        {
            return children[6];
        }
        private set
        {
            children[6] = value;
        }
    }

    internal ProgressTrackerPrimitive WebTilesProgress
    {
        get
        {
            return children[7];
        }
        private set
        {
            children[7] = value;
        }
    }

    static KdkSyncProgress()
    {
        TaskWeights = new Dictionary<SyncTasks, double>();
        TaskWeights.Add(SyncTasks.TimeAndTimeZone, 4.0);
        TaskWeights.Add(SyncTasks.EphemerisFile, 9.0);
        TaskWeights.Add(SyncTasks.TimeZoneFile, 8.0);
        TaskWeights.Add(SyncTasks.DeviceCrashDump, 8.0);
        TaskWeights.Add(SyncTasks.DeviceInstrumentation, 9.0);
        TaskWeights.Add(SyncTasks.UserProfileFirmwareBytes | SyncTasks.UserProfile, 4.0);
        TaskWeights.Add(SyncTasks.SensorLog, 58.0);
        TaskWeights.Add(SyncTasks.WebTiles | SyncTasks.WebTilesForced, 0.0);
        foreach (KeyValuePair<SyncTasks, double> taskWeight in TaskWeights)
        {
            TotalTaskWeight += taskWeight.Value;
        }
    }

    internal KdkSyncProgress(IProgress<SyncProgress> reporter, SyncTasks syncTasks)
        : base(new ProgressTrackerPrimitive[8])
    {
        KdkSyncProgress parent = this;
        if (reporter != null)
        {
            this.reporter = reporter;
        }
        else
        {
            this.reporter = new DummyProgress<SyncProgress>();
        }
        State = SyncState.NotStarted;
        Func<SyncTasks, bool> Need = (SyncTasks tasksToCheckIfIncluded) => (syncTasks & tasksToCheckIfIncluded) != 0;
        double weightOfRequested = 0.0;
        Action<SyncTasks> obj = delegate(SyncTasks tasksToCheckIfIncluded)
        {
            weightOfRequested += (Need(tasksToCheckIfIncluded) ? TaskWeights[tasksToCheckIfIncluded] : 0.0);
        };
        obj(SyncTasks.TimeAndTimeZone);
        obj(SyncTasks.EphemerisFile);
        obj(SyncTasks.TimeZoneFile);
        obj(SyncTasks.DeviceCrashDump);
        obj(SyncTasks.DeviceInstrumentation);
        obj(SyncTasks.UserProfileFirmwareBytes | SyncTasks.UserProfile);
        obj(SyncTasks.SensorLog);
        obj(SyncTasks.WebTiles | SyncTasks.WebTilesForced);
        Func<SyncTasks, ProgressTrackerPrimitive> func = delegate(SyncTasks tasksToCheckIfIncluded)
        {
            double weight = (Need(tasksToCheckIfIncluded) ? (TaskWeights[tasksToCheckIfIncluded] * TotalTaskWeight / weightOfRequested) : 0.0);
            return new ProgressTrackerPrimitive(parent, weight);
        };
        CurrentTimeAndTimeZoneProgress = func(SyncTasks.TimeAndTimeZone);
        EphemerisProgress = func(SyncTasks.EphemerisFile);
        TimeZoneProgress = func(SyncTasks.TimeZoneFile);
        DeviceCrashDumpProgress = func(SyncTasks.DeviceCrashDump);
        DeviceInstrumentationProgress = func(SyncTasks.DeviceInstrumentation);
        UserProfileProgress = func(SyncTasks.UserProfileFirmwareBytes | SyncTasks.UserProfile);
        LogSyncProgress = func(SyncTasks.SensorLog);
        WebTilesProgress = func(SyncTasks.WebTiles | SyncTasks.WebTilesForced);
    }

    internal override void ChildUpdated()
    {
        cachedPercentage = null;
        reporter.Report(new SyncProgress(PercentageCompletion, State));
    }

    internal void SetState(SyncState newState)
    {
        State = newState;
    }
}
