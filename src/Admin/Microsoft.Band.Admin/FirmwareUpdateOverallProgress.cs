// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.FirmwareUpdateOverallProgress
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  public sealed class FirmwareUpdateOverallProgress : ProgressTracker
  {
    private IProgress<FirmwareUpdateProgress> reporter;
    private FirmwareUpdateState state;
    private bool inConstructor;

    public FirmwareUpdateOperation Operation { get; private set; }

    public FirmwareUpdateState State
    {
      get => this.state;
      private set
      {
        if (this.state == value)
          return;
        this.state = value;
        this.reporter.Report(new FirmwareUpdateProgress(this.PercentageCompletion, this.State));
      }
    }

    public FirmwareUpdateOverallProgress(
      IProgress<FirmwareUpdateProgress> reporter,
      FirmwareUpdateOperation op)
      : base(new ProgressTrackerPrimitive[7])
    {
      this.inConstructor = true;
      this.reporter = reporter == null ? (IProgress<FirmwareUpdateProgress>) new DummyProgress<FirmwareUpdateProgress>() : reporter;
      this.Operation = op;
      this.State = FirmwareUpdateState.NotStarted;
      switch (op)
      {
        case FirmwareUpdateOperation.DownloadOnly:
          this.DownloadFirmwareProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 100.0);
          this.DownloadFirmwareProgress.AddStepsTotal(1);
          this.Send2UpUpdateToDeviceProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          this.WaitToConnectAfter2UpUpdateProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          this.LogSyncProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          this.BootToUpdateModeProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          this.SendUpdateToDeviceProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          this.WaitToConnectAfterUpdateProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          break;
        case FirmwareUpdateOperation.UpdateOnly:
          this.DownloadFirmwareProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          this.Send2UpUpdateToDeviceProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          this.WaitToConnectAfter2UpUpdateProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          this.LogSyncProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 20.0);
          this.BootToUpdateModeProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 10.0);
          this.BootToUpdateModeProgress.AddStepsTotal(1);
          this.SendUpdateToDeviceProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 30.0);
          this.WaitToConnectAfterUpdateProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 40.0);
          break;
        case FirmwareUpdateOperation.DownloadAndUpdate:
          this.DownloadFirmwareProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 5.0);
          this.DownloadFirmwareProgress.AddStepsTotal(1);
          this.Send2UpUpdateToDeviceProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          this.WaitToConnectAfter2UpUpdateProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 0.0);
          this.LogSyncProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 20.0);
          this.BootToUpdateModeProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 10.0);
          this.BootToUpdateModeProgress.AddStepsTotal(1);
          this.SendUpdateToDeviceProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 30.0);
          this.WaitToConnectAfterUpdateProgress = new ProgressTrackerPrimitive((ProgressTracker) this, 35.0);
          break;
      }
      this.inConstructor = false;
    }

    internal void SetTo2UpUpdate()
    {
      if (this.Operation == FirmwareUpdateOperation.DownloadOnly)
        throw new InvalidOperationException();
      switch (this.Operation)
      {
        case FirmwareUpdateOperation.UpdateOnly:
          this.DownloadFirmwareProgress.Weight = 0.0;
          this.Send2UpUpdateToDeviceProgress.Weight = 12.0;
          this.WaitToConnectAfter2UpUpdateProgress.Weight = 47.0;
          this.LogSyncProgress.Weight = 0.0;
          this.BootToUpdateModeProgress.Weight = 12.0;
          this.SendUpdateToDeviceProgress.Weight = 12.0;
          this.WaitToConnectAfterUpdateProgress.Weight = 17.0;
          break;
        case FirmwareUpdateOperation.DownloadAndUpdate:
          this.DownloadFirmwareProgress.Weight = 5.0;
          this.Send2UpUpdateToDeviceProgress.Weight = 11.0;
          this.WaitToConnectAfter2UpUpdateProgress.Weight = 45.0;
          this.LogSyncProgress.Weight = 0.0;
          this.BootToUpdateModeProgress.Weight = 11.0;
          this.SendUpdateToDeviceProgress.Weight = 11.0;
          this.WaitToConnectAfterUpdateProgress.Weight = 17.0;
          break;
      }
      this.reporter.Report(new FirmwareUpdateProgress(this.PercentageCompletion, this.State));
    }

    internal override void ChildUpdated()
    {
      if (this.inConstructor)
        return;
      this.cachedPercentage = new double?();
      this.reporter.Report(new FirmwareUpdateProgress(this.PercentageCompletion, this.State));
    }

    public void SetState(FirmwareUpdateState newState) => this.State = newState;

    public ProgressTrackerPrimitive DownloadFirmwareProgress
    {
      get => this.children[0];
      private set => this.children[0] = value;
    }

    internal ProgressTrackerPrimitive Send2UpUpdateToDeviceProgress
    {
      get => this.children[1];
      private set => this.children[1] = value;
    }

    internal ProgressTrackerPrimitive WaitToConnectAfter2UpUpdateProgress
    {
      get => this.children[2];
      private set => this.children[2] = value;
    }

    public ProgressTrackerPrimitive LogSyncProgress
    {
      get => this.children[3];
      private set => this.children[3] = value;
    }

    public ProgressTrackerPrimitive BootToUpdateModeProgress
    {
      get => this.children[4];
      private set => this.children[4] = value;
    }

    public ProgressTrackerPrimitive SendUpdateToDeviceProgress
    {
      get => this.children[5];
      private set => this.children[5] = value;
    }

    public ProgressTrackerPrimitive WaitToConnectAfterUpdateProgress
    {
      get => this.children[6];
      private set => this.children[6] = value;
    }
  }
}
