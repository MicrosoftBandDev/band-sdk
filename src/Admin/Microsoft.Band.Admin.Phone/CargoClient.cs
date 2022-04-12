// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CargoClient
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Google.Protobuf;
using Microsoft.Band.Admin.LogProcessing;
using Microsoft.Band.Admin.Phone;
using Microsoft.Band.Admin.Streaming;
using Microsoft.Band.Admin.WebTiles;
using Microsoft.Band.Notifications;
using Microsoft.Band.Personalization;
using Microsoft.Band.Sensors;
using Microsoft.Band.Store;
using Microsoft.Band.Tiles;
using Microsoft.Band.Tiles.Pages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Band.Admin
{
  internal sealed class CargoClient : BandClient, ICargoClient, IBandClient, IDisposable
  {
    private const int MaximumBluetoothRetryConnectRestricted = 2;
    internal const string PendingDataFolder = "PendingData";
    internal const string UserIdPrefix = "u_";
    internal const string DeviceIdPrefix = "d_";
    private const string DEVICE_FILE_SYNC_INFO_LOCAL_FILE = "DeviceFileSyncTimeInfo.json";
    private const string EphemerisFolder = "Ephemeris";
    private const string EphemerisVersionFileName = "EphemerisVersion.json";
    private const string EphemerisFileName = "EphemerisUpdate.bin";
    private const int MaxEphemerisFileAgeInHours = 12;
    private const double UsableEphemerisFileEffectiveRangeThreshold = 0.5;
    private const string TimeZoneDataFolder = "TimeZoneData";
    private const string TimeZoneDataVersionFile = "TimeZoneData.json";
    private const string TimeZoneUpdateFile = "TimeZoneUpdate.bin";
    private const string TimeZoneUpdateTempFile = "TimeZoneUpdateTemp.bin";
    internal const string FirmwareUpdateFolder = "FirmwareUpdate";
    internal const string FirmwareUpdateVersionFile = "FirmwareUpdate.json";
    internal const string FirmwareUpdateFile = "FirmwareUpdate.bin";
    internal const string FirmwareUpdateTempFile = "FirmwareUpdateTemp.bin";
    private const string TIMESTAMP_FORMAT_STRING_YEAR_TO_MILLISECOND = "yyyyMMddHHmmssfff";
    private const int MAX_ALLOWED_NUMBER_OF_DATA_FILES_IN_FOLDER = 20;
    private const int NUM_HOURS_BETWEEN_DEVICE_INSTRUMENTATION_FILE_DOWNLOADS = 168;
    private const int SENSOR_LOG_UPLOAD_INDEX = 0;
    private const string DeviceMetadataHint_Band = "band";
    private IPlatformProvider platformProvider;
    private IStorageProvider storageProvider;
    private CloudProvider cloudProvider;
    private object loggerLock;

    private CargoClient(
      IDeviceTransport transport,
      IApplicationPlatformProvider applicationPlatformProvider)
      : base(transport, (ILoggerProvider) new LoggerProvider(), applicationPlatformProvider)
    {
      this.disposed = false;
      this.runningFirmwareApp = FirmwareApp.Invalid;
      this.protocolLock = new object();
    }

    public static CargoClient CreateRestrictedClient(IBandInfo deviceInfo)
    {
      if (deviceInfo == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (deviceInfo));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (!(deviceInfo is BluetoothDeviceInfo))
      {
        Logger.Log(LogLevel.Error, "deviceInfo is not BluetoothDeviceInfo");
        throw new ArgumentException(nameof (deviceInfo));
      }
      CargoClient restrictedClient = (CargoClient) null;
      IDeviceTransport transport = (IDeviceTransport) BluetoothTransport.Create(deviceInfo, (ILoggerProvider) new LoggerProvider(), (ushort) 2);
      try
      {
        restrictedClient = new CargoClient(transport, StoreApplicationPlatformProvider.Current);
        restrictedClient.InitializeCachedProperties();
        Logger.Log(LogLevel.Info, "Created CargoClient (Restricted)");
      }
      catch
      {
        if (restrictedClient != null)
          restrictedClient.Dispose();
        else
          transport.Dispose();
        throw;
      }
      return restrictedClient;
    }

    public DynamicAdminBandConstants ConnectedAdminBandConstants
    {
      get
      {
        if (this.DeviceTransport == null)
          return (DynamicAdminBandConstants) null;
        switch (this.BandTypeConstants.BandType)
        {
          case BandType.Envoy:
            return DynamicAdminBandConstants.Envoy;
          default:
            return DynamicAdminBandConstants.Cargo;
        }
      }
    }

    public IDynamicBandConstants ConnectedBandConstants
    {
      get
      {
        if (this.DeviceTransport == null)
          return (IDynamicBandConstants) null;
        switch (this.BandTypeConstants.BandType)
        {
          case BandType.Envoy:
            return (IDynamicBandConstants) DynamicBandConstants.EnvoyConstants;
          default:
            return (IDynamicBandConstants) DynamicBandConstants.CargoConstants;
        }
      }
    }

    public Task<SyncResult> ObsoleteSyncDeviceToCloudAsync(
      CancellationToken cancellationToken,
      IProgress<SyncProgress> progress = null,
      bool logsOnly = false)
    {
      SyncTasks syncTasks = !logsOnly ? SyncTasks.TimeAndTimeZone | SyncTasks.EphemerisFile | SyncTasks.TimeZoneFile | SyncTasks.DeviceCrashDump | SyncTasks.DeviceInstrumentation | SyncTasks.UserProfile | SyncTasks.SensorLog | SyncTasks.WebTiles : SyncTasks.TimeAndTimeZone | SyncTasks.EphemerisFile | SyncTasks.UserProfileFirmwareBytes | SyncTasks.SensorLog | SyncTasks.WebTilesForced;
      return this.SyncDeviceToCloudAsync(cancellationToken, progress, syncTasks);
    }

    public Task<SyncResult> SyncRequiredBandInfoAsync(
      CancellationToken cancellationToken,
      IProgress<SyncProgress> progress = null)
    {
      SyncTasks syncTasks = SyncTasks.TimeAndTimeZone | SyncTasks.EphemerisFile | SyncTasks.UserProfileFirmwareBytes | SyncTasks.SensorLog;
      return this.SyncDeviceToCloudAsync(cancellationToken, progress, syncTasks);
    }

    public Task<SyncResult> SyncAuxiliaryBandInfoAsync(
      CancellationToken cancellationToken)
    {
      SyncTasks syncTasks = SyncTasks.TimeZoneFile | SyncTasks.DeviceCrashDump | SyncTasks.DeviceInstrumentation | SyncTasks.WebTilesForced;
      return this.SyncDeviceToCloudAsync(cancellationToken, (IProgress<SyncProgress>) null, syncTasks);
    }

    public Task<SyncResult> SyncAllBandInfoAsync(CancellationToken cancellationToken)
    {
      SyncTasks syncTasks = SyncTasks.TimeAndTimeZone | SyncTasks.EphemerisFile | SyncTasks.TimeZoneFile | SyncTasks.DeviceCrashDump | SyncTasks.DeviceInstrumentation | SyncTasks.UserProfile | SyncTasks.SensorLog | SyncTasks.WebTiles;
      return this.SyncDeviceToCloudAsync(cancellationToken, (IProgress<SyncProgress>) null, syncTasks);
    }

    internal Task<SyncResult> SyncDeviceToCloudAsync(
      CancellationToken cancellationToken,
      IProgress<SyncProgress> progress,
      SyncTasks syncTasks)
    {
      return Task.Run<SyncResult>((Func<SyncResult>) (() => this.SyncDeviceToCloud(cancellationToken, progress, syncTasks)));
    }

    private SyncResult SyncDeviceToCloud(
      CancellationToken cancellationToken,
      IProgress<SyncProgress> progress,
      SyncTasks syncTasks)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      KdkSyncProgress progressTracker = new KdkSyncProgress(progress, syncTasks);
      lock (this.loggerLock)
        return this.SyncDeviceToCloudCore(cancellationToken, progressTracker, syncTasks);
    }

    private SyncResult SyncDeviceToCloudCore(
      CancellationToken cancellationToken,
      KdkSyncProgress progressTracker,
      SyncTasks syncTasks)
    {
      bool doRethrows = false;
      Func<string, Action, string, long> LogDoAndMeasure = (Func<string, Action, string, long>) ((logFirst, code, logOnError) =>
      {
        Stopwatch stopwatch = Stopwatch.StartNew();
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.IsNullOrEmpty(logFirst))
          Logger.Log(LogLevel.Info, logFirst);
        try
        {
          code();
        }
        catch (OperationCanceledException ex)
        {
          throw;
        }
        catch (Exception ex)
        {
          Logger.LogException(LogLevel.Warning, ex, logOnError);
          if (doRethrows)
            throw;
        }
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
      });
      Func<Action, string, long> func = (Func<Action, string, long>) ((code, logOnError) => LogDoAndMeasure(string.Empty, code, logOnError));
      Stopwatch stopwatch1 = Stopwatch.StartNew();
      SyncResult syncResult = new SyncResult();
      try
      {
        if (syncTasks.HasFlag((Enum) SyncTasks.TimeAndTimeZone))
          syncResult.TimeZoneElapsed = new long?(func((Action) (() =>
          {
            progressTracker.SetState(SyncState.CurrentTimeAndTimeZone);
            this.SetCurrentTimeAndTimeZone(cancellationToken);
            progressTracker.CurrentTimeAndTimeZoneProgress.Complete();
          }), "Exception occurred in UpdateDeviceTimeAndTimeZone"));
        if (this.cloudProvider == null)
        {
          InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
          Logger.LogException(LogLevel.Error, (Exception) e);
          throw e;
        }
        if (syncTasks.HasFlag((Enum) SyncTasks.EphemerisFile))
        {
          bool updateWasDone = false;
          long num = LogDoAndMeasure("Updating ephemeris file (if needed)...", (Action) (() =>
          {
            progressTracker.SetState(SyncState.Ephemeris);
            updateWasDone = this.UpdateEphemeris(cancellationToken, (ProgressTrackerPrimitiveBase) progressTracker.EphemerisProgress, false);
          }), "Exception occurred when updating ephemeris data");
          if (updateWasDone)
            syncResult.EphemerisUpdateElapsed = new long?(num);
          else
            syncResult.EphemerisCheckElapsed = new long?(num);
        }
        if (syncTasks.HasFlag((Enum) SyncTasks.TimeZoneFile))
          syncResult.TimeZoneElapsed = new long?(LogDoAndMeasure("Updating time zone file (if needed)...", (Action) (() =>
          {
            progressTracker.SetState(SyncState.TimeZoneData);
            this.UpdateTimeZoneList(cancellationToken, progressTracker, false, (IUserProfile) null);
          }), "Exception occurred when updating time zone data"));
        bool crashdumpDownloaded = false;
        if (syncTasks.HasFlag((Enum) SyncTasks.DeviceCrashDump))
          syncResult.CrashDumpElapsed = new long?(LogDoAndMeasure("Syncing device crash dump...", (Action) (() =>
          {
            progressTracker.SetState(SyncState.DeviceCrashDump);
            crashdumpDownloaded = this.GetCrashDumpFileFromDeviceAndPushToCloud(progressTracker.DeviceCrashDumpProgress, cancellationToken);
          }), "Exception occurred when getting crashDump file from device and pushing it to the cloud"));
        if (syncTasks.HasFlag((Enum) SyncTasks.DeviceInstrumentation))
        {
          long num1 = LogDoAndMeasure("Syncing device instrumentation...", (Action) (() =>
          {
            progressTracker.SetState(SyncState.DeviceInstrumentation);
            this.GetInstrumentationFileFromDeviceAndPushToCloud(progressTracker.DeviceInstrumentationProgress, cancellationToken, !crashdumpDownloaded);
          }), "Exception occurred when getting instrumentation file from device and pushing it to the cloud");
        }
        if (syncTasks.HasFlag((Enum) SyncTasks.UserProfile))
          syncResult.UserProfileFullElapsed = new long?(func((Action) (() =>
          {
            progressTracker.SetState(SyncState.UserProfile);
            this.SyncUserProfile(cancellationToken);
            progressTracker.UserProfileProgress.Complete();
          }), "Exception occurred in SyncUserProfile"));
        else if (syncTasks.HasFlag((Enum) SyncTasks.UserProfileFirmwareBytes))
          syncResult.UserProfileFirmwareBytesElapsed = new long?(func((Action) (() =>
          {
            progressTracker.SetState(SyncState.UserProfile);
            this.SaveUserProfileFirmwareBytes(cancellationToken);
            progressTracker.UserProfileProgress.Complete();
          }), "Exception occurred in SaveUserProfileFirmwareBytes"));
        if (syncTasks.HasFlag((Enum) SyncTasks.SensorLog))
        {
          doRethrows = true;
          syncResult.SensorLogElapsed = new long?(func((Action) (() =>
          {
            progressTracker.SetState(SyncState.SensorLog);
            this.SyncSensorLog(cancellationToken, progressTracker.LogSyncProgress).CopyToSyncResult(syncResult);
          }), "Exception occurred in SyncSensorLog()"));
          doRethrows = false;
        }
        if (syncTasks.HasFlag((Enum) SyncTasks.WebTilesForced))
        {
          SyncResult syncResult1 = syncResult;
          long? webTilesElapsed = syncResult1.WebTilesElapsed;
          long num2 = func((Action) (() =>
          {
            progressTracker.SetState(SyncState.WebTiles);
            this.SyncWebTiles(true, CancellationToken.None);
            progressTracker.WebTilesProgress.Complete();
          }), "Exception occurred when syncing WebTiles");
          syncResult1.WebTilesElapsed = webTilesElapsed.HasValue ? new long?(webTilesElapsed.GetValueOrDefault() + num2) : new long?();
        }
        else if (syncTasks.HasFlag((Enum) SyncTasks.WebTiles))
        {
          SyncResult syncResult2 = syncResult;
          long? webTilesElapsed = syncResult2.WebTilesElapsed;
          long num3 = func((Action) (() =>
          {
            progressTracker.SetState(SyncState.WebTiles);
            this.SyncWebTiles(false, CancellationToken.None);
            progressTracker.WebTilesProgress.Complete();
          }), "Exception occurred when syncing WebTiles");
          syncResult2.WebTilesElapsed = webTilesElapsed.HasValue ? new long?(webTilesElapsed.GetValueOrDefault() + num3) : new long?();
        }
      }
      finally
      {
        stopwatch1.Stop();
        progressTracker.SetState(SyncState.Done);
      }
      Logger.Log(LogLevel.Info, "Sync completed");
      syncResult.TotalTimeElapsed = stopwatch1.ElapsedMilliseconds;
      return syncResult;
    }

    public Task<long> GetPendingLocalDataBytesAsync() => Task.Run<long>((Func<long>) (() => this.GetPendingLocalDataBytes()));

    public long GetPendingLocalDataBytes()
    {
      this.CheckIfDisposed();
      long pendingLocalDataBytes = 0;
      foreach (string file in this.storageProvider.GetFiles("PendingData"))
      {
        if (!file.EndsWith(".chunk.meta"))
        {
          string relativePath = Path.Combine(new string[2]
          {
            "PendingData",
            file
          });
          pendingLocalDataBytes += this.storageProvider.GetFileSize(relativePath);
        }
      }
      return pendingLocalDataBytes;
    }

    public Task<long> GetPendingDeviceDataBytesAsync() => Task.Run<long>((Func<long>) (() => this.GetPendingDeviceDataBytes()));

    public long GetPendingDeviceDataBytes()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      lock (this.loggerLock)
        return (long) this.RemainingDeviceLogDataChunks() * 4096L;
    }

    public Task<IUserProfile> GetUserProfileFromDeviceAsync() => Task.Run<IUserProfile>((Func<IUserProfile>) (() => this.GetUserProfileFromDevice()));

    public IUserProfile GetUserProfileFromDevice()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Obtaining the application profile from the band");
      int byteCount = UserProfile.GetAppDataSerializedByteCount(this.ConnectedAdminBandConstants);
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteInt32(byteCount));
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoProfileGetDataApp, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
      {
        UserProfile profileFromDevice = UserProfile.DeserializeAppDataFromBand((ICargoReader) reader, this.ConnectedAdminBandConstants);
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        profileFromDevice.DeviceSettings.DeviceId = this.DeviceUniqueId;
        profileFromDevice.DeviceSettings.SerialNumber = this.SerialNumber;
        return (IUserProfile) profileFromDevice;
      }
    }

    public Task<IUserProfile> GetUserProfileAsync() => Task.Run<IUserProfile>((Func<IUserProfile>) (() => this.GetUserProfile()));

    public IUserProfile GetUserProfile() => this.GetUserProfile(CancellationToken.None);

    public Task<IUserProfile> GetUserProfileAsync(
      CancellationToken cancellationToken)
    {
      return Task.Run<IUserProfile>((Func<IUserProfile>) (() => this.GetUserProfile(cancellationToken)));
    }

    public IUserProfile GetUserProfile(CancellationToken cancellationToken)
    {
      this.CheckIfDisposed();
      if (this.cloudProvider == null)
      {
        ArgumentNullException e = new ArgumentNullException("cloudProvider");
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      return (IUserProfile) new UserProfile(this.cloudProvider.GetUserProfile(cancellationToken), this.ConnectedAdminBandConstants);
    }

    public Task SaveUserProfileAsync(IUserProfile profile, DateTimeOffset? updateTime = null) => Task.Run((Action) (() => this.SaveUserProfile(profile, updateTime)));

    public void SaveUserProfile(IUserProfile profile, DateTimeOffset? updateTime = null) => this.SaveUserProfile(profile, CancellationToken.None, updateTime);

    public Task SaveUserProfileAsync(
      IUserProfile profile,
      CancellationToken cancellationToken,
      DateTimeOffset? updateTime = null)
    {
      return Task.Run((Action) (() => this.SaveUserProfile(profile, cancellationToken, updateTime)));
    }

    public void SaveUserProfile(
      IUserProfile profile,
      CancellationToken cancellationToken,
      DateTimeOffset? updateTimeN = null)
    {
      UserProfile profileImplementation = this.GetUserProfileImplementation(profile);
      this.CheckIfDisposed();
      if (this.cloudProvider == null)
      {
        ArgumentNullException e = new ArgumentNullException("cloudProvider");
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      this.SetUserProfileUpdateTime(profileImplementation, updateTimeN.HasValue ? updateTimeN.Value.ToUniversalTime() : (DateTimeOffset) DateTime.UtcNow);
      try
      {
        this.CheckIfDisconnectedOrUpdateMode();
        this.GetDeviceMasteredUserProfileProperties(profileImplementation, true);
        this.ProfileSetAppData(profileImplementation);
      }
      catch
      {
        Logger.Log(LogLevel.Info, "SaveUserProfile -- Connection to Device Unavailable.  Saving to Cloud only.");
      }
      this.cloudProvider.SaveUserProfile(profileImplementation.ToCloudProfile(), false, cancellationToken);
    }

    public void SaveUserProfileToBandOnly(IUserProfile profile, DateTimeOffset? updateTimeN = null)
    {
      UserProfile profileImplementation = this.GetUserProfileImplementation(profile);
      this.CheckIfDisposed();
      this.SetUserProfileUpdateTime(profileImplementation, updateTimeN.HasValue ? updateTimeN.Value.ToUniversalTime() : (DateTimeOffset) DateTime.UtcNow);
      try
      {
        this.CheckIfDisconnectedOrUpdateMode();
        this.ProfileSetAppData(profileImplementation);
      }
      catch
      {
        Logger.Log(LogLevel.Info, "SaveUserProfile -- Connection to Device Unavailable.");
      }
    }

    public Task SaveUserProfileToBandOnlyAsync(
      IUserProfile profile,
      DateTimeOffset? updateTimeN = null)
    {
      return Task.Run((Action) (() => this.SaveUserProfileToBandOnly(profile, updateTimeN)));
    }

    public Task SaveUserProfileFirmwareBytesAsync(CancellationToken cancellationToken) => Task.Run((Action) (() => this.SaveUserProfileFirmwareBytes(cancellationToken)));

    public void SaveUserProfileFirmwareBytes(CancellationToken cancellationToken)
    {
      if (this.cloudProvider == null)
      {
        ArgumentNullException e = new ArgumentNullException("cloudProvider");
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      this.CheckIfDisconnectedOrUpdateMode();
      this.cloudProvider.SaveUserProfileFirmware(this.ProfileGetFirmwareBytes(), cancellationToken);
    }

    public Task ImportUserProfileAsync(CancellationToken cancellationToken) => Task.Run((Action) (() => this.ImportUserProfile(cancellationToken)));

    public void ImportUserProfile(CancellationToken cancellationToken) => this.ImportUserProfile(this.GetUserProfile(cancellationToken), cancellationToken);

    public Task ImportUserProfileAsync(
      IUserProfile userProfile,
      CancellationToken cancellationToken)
    {
      return Task.Run((Action) (() => this.ImportUserProfile(userProfile, cancellationToken)));
    }

    public void ImportUserProfile(IUserProfile profile, CancellationToken cancellationToken)
    {
      UserProfile profileImplementation = this.GetUserProfileImplementation(profile);
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (this.cloudProvider == null)
      {
        ArgumentNullException e = new ArgumentNullException("cloudProvider");
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      this.SetUserProfileUpdateTime(profileImplementation, DateTimeOffset.UtcNow);
      this.cloudProvider.SaveUserProfile(profileImplementation.ToCloudProfile(), false, cancellationToken);
      this.GetDeviceMasteredUserProfileProperties(profileImplementation, true);
      this.ProfileSetAppData(profileImplementation);
      if (profileImplementation.DeviceSettings.FirmwareByteArray == null || profileImplementation.DeviceSettings.FirmwareByteArray.Length == 0)
        return;
      this.ProfileSetFirmwareBytes(profileImplementation);
    }

    public DeviceProfileStatus GetDeviceAndProfileLinkStatus(
      IUserProfile userProfile = null)
    {
      return this.GetDeviceAndProfileLinkStatus(CancellationToken.None, userProfile);
    }

    public Task<DeviceProfileStatus> GetDeviceAndProfileLinkStatusAsync(
      IUserProfile userProfile = null)
    {
      return Task.Run<DeviceProfileStatus>((Func<DeviceProfileStatus>) (() => this.GetDeviceAndProfileLinkStatus(CancellationToken.None, userProfile)));
    }

    public Task<DeviceProfileStatus> GetDeviceAndProfileLinkStatusAsync(
      CancellationToken cancellationToken,
      IUserProfile userProfile = null)
    {
      return Task.Run<DeviceProfileStatus>((Func<DeviceProfileStatus>) (() => this.GetDeviceAndProfileLinkStatus(cancellationToken, userProfile)));
    }

    public DeviceProfileStatus GetDeviceAndProfileLinkStatus(
      CancellationToken cancellationToken,
      IUserProfile profile = null)
    {
      UserProfile userProfile = (UserProfile) null;
      if (profile != null)
        userProfile = this.GetUserProfileImplementation(profile);
      else if (this.cloudProvider == null)
      {
        ArgumentNullException e = new ArgumentNullException("cloudProvider");
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (userProfile == null)
        userProfile = (UserProfile) this.GetUserProfile(cancellationToken);
      return this.GetDeviceAndProfileLinkStatus(cancellationToken, userProfile.UserID, userProfile.ApplicationSettings.PairedDeviceId);
    }

    public Task<DeviceProfileStatus> GetDeviceAndProfileLinkStatusAsync(
      CancellationToken cancellationToken,
      Guid cloudUserId,
      Guid cloudDeviceId)
    {
      return Task.Run<DeviceProfileStatus>((Func<DeviceProfileStatus>) (() => this.GetDeviceAndProfileLinkStatus(cancellationToken, cloudUserId, cloudDeviceId)));
    }

    private DeviceProfileStatus GetDeviceAndProfileLinkStatus(
      CancellationToken cancellationToken,
      Guid cloudUserId,
      Guid cloudDeviceId)
    {
      cancellationToken.ThrowIfCancellationRequested();
      DeviceProfileStatus profileLinkStatus = new DeviceProfileStatus();
      UserProfileHeader userProfileHeader = this.ProfileAppHeaderGet();
      profileLinkStatus.UserLinkStatus = !(cloudDeviceId == Guid.Empty) ? (!(cloudDeviceId == this.DeviceUniqueId) ? LinkStatus.NonMatching : LinkStatus.Matching) : LinkStatus.Empty;
      profileLinkStatus.DeviceLinkStatus = !(userProfileHeader.UserID == Guid.Empty) ? (!(userProfileHeader.UserID == cloudUserId) ? LinkStatus.NonMatching : LinkStatus.Matching) : LinkStatus.Empty;
      Logger.Log(LogLevel.Info, "Checking DeviceProfileLink: (UserDeviceID: {2} == DeviceID: {3}) is UserLinkStatus: {0} && (DeviceProfileID: {4} == UserProfileID: {5}) is DeviceLinkStatus: {1}", (object) profileLinkStatus.UserLinkStatus, (object) profileLinkStatus.DeviceLinkStatus, (object) cloudDeviceId, (object) this.DeviceUniqueId, (object) userProfileHeader.UserID, (object) cloudUserId);
      return profileLinkStatus;
    }

    public Task LinkDeviceToProfileAsync(IUserProfile userProfile = null, bool importUserProfile = false) => Task.Run((Action) (() => this.LinkDeviceToProfile(userProfile, importUserProfile)));

    public void LinkDeviceToProfile(IUserProfile userProfile = null, bool importUserProfile = false) => this.LinkDeviceToProfile(CancellationToken.None, userProfile, importUserProfile);

    public Task LinkDeviceToProfileAsync(
      CancellationToken cancellationToken,
      IUserProfile userProfile = null,
      bool importUserProfile = false)
    {
      return Task.Run((Action) (() => this.LinkDeviceToProfile(cancellationToken, userProfile, importUserProfile)));
    }

    public void LinkDeviceToProfile(
      CancellationToken cancellationToken,
      IUserProfile userProfile = null,
      bool importUserProfile = false)
    {
      this.SetDeviceProfileLink(true, cancellationToken, userProfile, importUserProfile);
    }

    public Task UnlinkDeviceFromProfileAsync(IUserProfile userProfile = null) => Task.Run((Action) (() => this.UnlinkDeviceFromProfile(CancellationToken.None, userProfile)));

    public void UnlinkDeviceFromProfile(IUserProfile userProfile = null) => this.SetDeviceProfileLink(false, CancellationToken.None, userProfile);

    public Task UnlinkDeviceFromProfileAsync(
      CancellationToken cancellationToken,
      IUserProfile userProfile = null)
    {
      return Task.Run((Action) (() => this.UnlinkDeviceFromProfile(cancellationToken, userProfile)));
    }

    public void UnlinkDeviceFromProfile(
      CancellationToken cancellationToken,
      IUserProfile userProfile = null)
    {
      this.SetDeviceProfileLink(false, cancellationToken, userProfile);
    }

    internal void SetDeviceProfileLink(
      bool setLink,
      CancellationToken cancellationToken,
      IUserProfile profile = null,
      bool importUserProfile = false)
    {
      UserProfile profile1 = (UserProfile) null;
      if (profile != null)
        profile1 = this.GetUserProfileImplementation(profile);
      this.CheckIfDisposed();
      if (this.cloudProvider == null)
      {
        ArgumentNullException e = new ArgumentNullException("cloudProvider");
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (profile1 == null)
        profile1 = (UserProfile) this.GetUserProfile();
      this.SetUserProfileUpdateTime(profile1, DateTimeOffset.UtcNow);
      if (setLink)
      {
        profile1.ApplicationSettings.PairedDeviceId = this.DeviceUniqueId;
        profile1.DeviceSettings.DeviceId = this.DeviceUniqueId;
        profile1.DeviceSettings.SerialNumber = this.SerialNumber;
      }
      else
      {
        profile1.ApplicationSettings.PairedDeviceId = Guid.Empty;
        profile1.DeviceSettings.DeviceId = Guid.Empty;
        profile1.DeviceSettings.SerialNumber = (string) null;
      }
      this.cloudProvider.SaveDeviceLinkToUserProfile(profile1.ToCloudProfileDeviceLink(), cancellationToken);
      try
      {
        this.CheckIfDisconnectedOrUpdateMode();
        this.GetDeviceMasteredUserProfileProperties(profile1, true);
        if (!setLink)
          profile1.UserID = Guid.Empty;
        this.ProfileSetAppData(profile1);
        if (!(setLink & importUserProfile) || profile1.DeviceSettings.FirmwareByteArray == null || profile1.DeviceSettings.FirmwareByteArray.Length == 0)
          return;
        this.ProfileSetFirmwareBytes(profile1);
      }
      catch
      {
        if (setLink)
          throw;
        else
          Logger.Log(LogLevel.Info, "SetDeviceProfileLink -- Connection to Device Unavailable.  Unlinking Device on Cloud only.");
      }
    }

    public Task SyncUserProfileAsync(CancellationToken cancellationToken) => Task.Run((Action) (() => this.SyncUserProfile(cancellationToken)));

    public void SyncUserProfile(CancellationToken cancellationToken)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (this.cloudProvider == null)
      {
        ArgumentNullException e = new ArgumentNullException("cloudProvider");
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      UserProfile userProfile = (UserProfile) this.GetUserProfile(cancellationToken);
      UserProfileHeader userProfileHeader = this.ProfileAppHeaderGet();
      bool flag = this.GetDeviceMasteredUserProfileProperties(userProfile, false);
      if (userProfile.LastKDKSyncUpdateOn.HasValue)
      {
        DateTimeOffset? lastKdkSyncUpdateOn = userProfileHeader.LastKDKSyncUpdateOn;
        if (lastKdkSyncUpdateOn.HasValue)
        {
          lastKdkSyncUpdateOn = userProfile.LastKDKSyncUpdateOn;
          DateTimeOffset dateTimeOffset1 = lastKdkSyncUpdateOn.Value;
          lastKdkSyncUpdateOn = userProfileHeader.LastKDKSyncUpdateOn;
          DateTimeOffset dateTimeOffset2 = lastKdkSyncUpdateOn.Value;
          if (dateTimeOffset1 > dateTimeOffset2)
          {
            flag = true;
            goto label_8;
          }
          else
          {
            lastKdkSyncUpdateOn = userProfile.LastKDKSyncUpdateOn;
            DateTimeOffset dateTimeOffset3 = lastKdkSyncUpdateOn.Value;
            lastKdkSyncUpdateOn = userProfileHeader.LastKDKSyncUpdateOn;
            DateTimeOffset dateTimeOffset4 = lastKdkSyncUpdateOn.Value;
            int num = dateTimeOffset3 < dateTimeOffset4 ? 1 : 0;
            goto label_8;
          }
        }
      }
      flag = true;
label_8:
      cancellationToken.ThrowIfCancellationRequested();
      if (flag)
        this.ProfileSetAppData(userProfile);
      this.SaveUserProfileFirmwareBytes(cancellationToken);
    }

    private UserProfile GetUserProfileImplementation(IUserProfile profile)
    {
      if (profile == null)
        throw new ArgumentNullException(nameof (profile));
      return profile is UserProfile userProfile ? userProfile : throw new ArgumentException("Unexpected implementation", nameof (profile));
    }

    private void SetUserProfileUpdateTime(UserProfile profile, DateTimeOffset updateTime) => profile.LastKDKSyncUpdateOn = new DateTimeOffset?((DateTimeOffset) new DateTime(updateTime.Year, updateTime.Month, updateTime.Day, updateTime.Hour, updateTime.Minute, updateTime.Second, DateTimeKind.Utc));

    private bool GetDeviceMasteredUserProfileProperties(UserProfile profile, bool forExplicitSave)
    {
      int byteCount = UserProfile.GetAppDataSerializedByteCount(this.ConnectedAdminBandConstants);
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteInt32(byteCount));
      Logger.Log(LogLevel.Info, "Obtaining the device mastered profile settings from the band");
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoProfileGetDataApp, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
      {
        int num = profile.DeserializeAndOverwriteDeviceMasteredAppDataFromBand((ICargoReader) reader, this.ConnectedAdminBandConstants, forExplicitSave) ? 1 : 0;
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return num != 0;
      }
    }

    private UserProfile GetDeviceMasteredUserProfileProperties()
    {
      int byteCount = UserProfile.GetAppDataSerializedByteCount(this.ConnectedAdminBandConstants);
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteInt32(byteCount));
      Logger.Log(LogLevel.Info, "Obtaining the device mastered profile settings from the band");
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoProfileGetDataApp, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
      {
        UserProfile profileProperties = UserProfile.DeserializeDeviceMasteredAppDataFromBand((ICargoReader) reader, this.ConnectedAdminBandConstants);
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return profileProperties;
      }
    }

    public Task<DateTime> GetDeviceUtcTimeAsync() => Task.Run<DateTime>((Func<DateTime>) (() => this.GetDeviceUtcTime()));

    public DateTime GetDeviceUtcTime()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Verbose, "Getting Device UTC time");
      int serializedByteCount = CargoFileTime.GetSerializedByteCount();
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoTimeGetUtcTime, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError))
        return CargoFileTime.DeserializeFromBandAsDateTime((ICargoReader) reader);
    }

    public Task<DateTime> GetDeviceLocalTimeAsync() => Task.Run<DateTime>((Func<DateTime>) (() => this.GetDeviceLocalTime()));

    public DateTime GetDeviceLocalTime()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Verbose, "Getting Device local time");
      int serializedByteCount = CargoSystemTime.GetSerializedByteCount();
      using (ICargoReader reader = (ICargoReader) this.ProtocolBeginRead(DeviceCommands.CargoTimeGetLocalTime, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError))
        return CargoSystemTime.DeserializeFromBandAsDateTime(reader, DateTimeKind.Local);
    }

    public Task SetDeviceUtcTimeAsync() => Task.Run((Action) (() => this.SetDeviceUtcTime()));

    public void SetDeviceUtcTime() => this.SetDeviceUtcTime(DateTime.UtcNow);

    public Task SetDeviceUtcTimeAsync(DateTime utc) => Task.Run((Action) (() => this.SetDeviceUtcTime(utc)));

    public void SetDeviceUtcTime(DateTime utc)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Verbose, "Setting Device UTC time");
      int serializedByteCount = CargoFileTime.GetSerializedByteCount();
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoTimeSetUtcTime, serializedByteCount, CommandStatusHandling.DoNotThrow))
        CargoFileTime.SerializeToBandFromDateTime((ICargoWriter) writer, utc);
    }

    public Task<CargoTimeZoneInfo> GetDeviceTimeZoneAsync() => Task.Run<CargoTimeZoneInfo>((Func<CargoTimeZoneInfo>) (() => this.GetDeviceTimeZone()));

    public CargoTimeZoneInfo GetDeviceTimeZone()
    {
      Logger.Log(LogLevel.Verbose, "Getting Device Time Zone");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      int serializedByteCount = CargoTimeZoneInfo.GetSerializedByteCount();
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoSystemSettingsGetTimeZone, serializedByteCount, CommandStatusHandling.DoNotCheck))
      {
        CargoTimeZoneInfo deviceTimeZone = CargoTimeZoneInfo.DeserializeFromBand((ICargoReader) reader);
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return deviceTimeZone;
      }
    }

    public Task SetDeviceTimeZoneAsync(CargoTimeZoneInfo timeZone) => Task.Run((Action) (() => this.SetDeviceTimeZone(timeZone)));

    public void SetDeviceTimeZone(CargoTimeZoneInfo timeZone)
    {
      Logger.Log(LogLevel.Verbose, "Setting Device Time Zone");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      int serializedByteCount = CargoTimeZoneInfo.GetSerializedByteCount();
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoSystemSettingsSetTimeZone, serializedByteCount, CommandStatusHandling.DoNotCheck))
        timeZone.SerializeToBand((ICargoWriter) writer);
    }

    public Task SetCurrentTimeAndTimeZoneAsync() => Task.Run((Action) (() => this.SetCurrentTimeAndTimeZone(CancellationToken.None)));

    public void SetCurrentTimeAndTimeZone() => this.SetCurrentTimeAndTimeZone(CancellationToken.None);

    public Task SetCurrentTimeAndTimeZoneAsync(CancellationToken cancellationToken) => Task.Run((Action) (() => this.SetCurrentTimeAndTimeZone(cancellationToken)));

    public void SetCurrentTimeAndTimeZone(CancellationToken cancellationToken)
    {
      if (cancellationToken.IsCancellationRequested)
        return;
      this.loggerProvider.Log(ProviderLogLevel.Info, "Syncing device UTC time (if allowed)...");
      bool flag = true;
      if (false)
      {
        DateTime deviceUtcTime = this.GetDeviceUtcTime();
        if (cancellationToken.IsCancellationRequested)
          return;
        flag = Math.Abs(DateTime.UtcNow.Subtract(deviceUtcTime).TotalSeconds) > 0.0;
      }
      if (flag)
        this.SetDeviceUtcTime();
      if (cancellationToken.IsCancellationRequested)
        return;
      this.loggerProvider.Log(ProviderLogLevel.Info, "Syncing device current time zone (if allowed)...");
      this.SetDeviceTimeZone(WindowsDateTime.GetWindowsCurrentTimeZone());
    }

    public Task<bool> GetFirmwareBinariesValidationStatusAsync() => Task.Run<bool>((Func<bool>) (() => this.GetFirmwareBinariesValidationStatus()));

    public bool GetFirmwareBinariesValidationStatus() => this.GetFirmwareBinariesValidationStatusInternal();

    private bool GetFirmwareBinariesValidationStatusInternal()
    {
      Logger.Log(LogLevel.Info, "Getting firmware binaries validation status");
      this.CheckIfDisposed();
      if (this.DeviceTransport == null)
      {
        InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      int count = CargoVersion.GetSerializedByteCount() * 3;
      bool validationStatusInternal = false;
      int bytesToRead = count + 4;
      using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoSRAMFWUpdateValidateAssets, bytesToRead, CommandStatusHandling.ThrowOnlySeverityError))
      {
        this.DeviceTransport.CargoStream.ReadTimeout = 20000;
        cargoCommandReader.ReadExactAndDiscard(count);
        validationStatusInternal = cargoCommandReader.ReadBool32();
      }
      Logger.Log(LogLevel.Info, "Firmware binaries validation status: {0}", validationStatusInternal ? (object) "Valid" : (object) "Invalid");
      return validationStatusInternal;
    }

    public Task<bool> GetDeviceOobeCompletedAsync() => Task.Run<bool>((Func<bool>) (() => this.GetDeviceOobeCompleted()));

    public bool GetDeviceOobeCompleted()
    {
      this.CheckIfDisposed();
      if (this.DeviceTransport == null)
      {
        InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      Logger.Log(LogLevel.Verbose, "Getting OOBECompleted flag from device");
      bool completed = false;
      Action<ICargoReader> readData = (Action<ICargoReader>) (r => completed = r.ReadBool32());
      this.ProtocolRead(DeviceCommands.CargoSystemSettingsOobeCompleteGet, 4, readData, statusHandling: CommandStatusHandling.ThrowAnyNonZero);
      return completed;
    }

    public Task<EphemerisCoverageDates> GetGpsEphemerisCoverageDatesFromDeviceAsync() => Task.Run<EphemerisCoverageDates>((Func<EphemerisCoverageDates>) (() => this.GetGpsEphemerisCoverageDatesFromDevice()));

    public EphemerisCoverageDates GetGpsEphemerisCoverageDatesFromDevice()
    {
      this.CheckIfDisconnectedOrUpdateMode();
      EphemerisCoverageDates coverageDatesFromDevice = new EphemerisCoverageDates();
      try
      {
        int bytesToRead = CargoFileTime.GetSerializedByteCount() * 2;
        using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoGpsEphemerisCoverageDates, bytesToRead, CommandStatusHandling.DoNotCheck))
        {
          coverageDatesFromDevice.StartDate = new DateTime?(CargoFileTime.DeserializeFromBandAsDateTime((ICargoReader) reader));
          coverageDatesFromDevice.EndDate = new DateTime?(CargoFileTime.DeserializeFromBandAsDateTime((ICargoReader) reader));
          if ((int) reader.CommandStatus.Status != (int) DeviceStatusCodes.Success)
          {
            coverageDatesFromDevice.StartDate = new DateTime?();
            coverageDatesFromDevice.EndDate = new DateTime?();
            Logger.Log(LogLevel.Info, "Ephemeris coverage dates were not found.  Proceeding as if no Ephemeris file on device");
          }
        }
      }
      catch (Exception ex)
      {
        Logger.LogException(LogLevel.Info, ex, "Ephemeris coverage dates were not found.  Proceeding as if no Ephemeris file on device");
        coverageDatesFromDevice.StartDate = new DateTime?();
        coverageDatesFromDevice.EndDate = new DateTime?();
      }
      return coverageDatesFromDevice;
    }

    public Task<bool> UpdateGpsEphemerisDataAsync() => Task.Run<bool>((Func<bool>) (() => this.UpdateEphemeris(CancellationToken.None, (ProgressTrackerPrimitiveBase) null, false)));

    public bool UpdateGpsEphemerisData() => this.UpdateEphemeris(CancellationToken.None, (ProgressTrackerPrimitiveBase) null, false);

    public Task<bool> UpdateGpsEphemerisDataAsync(
      CancellationToken cancellationToken,
      bool forceUpdate = false)
    {
      return Task.Run<bool>((Func<bool>) (() => this.UpdateEphemeris(cancellationToken, (ProgressTrackerPrimitiveBase) null, forceUpdate)));
    }

    public bool UpdateGpsEphemerisData(CancellationToken cancellationToken, bool forceUpdate = false) => this.UpdateEphemeris(cancellationToken, (ProgressTrackerPrimitiveBase) null, forceUpdate);

    private bool UpdateEphemeris(
      CancellationToken cancellationToken,
      ProgressTrackerPrimitiveBase progress,
      bool forceUpdate)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.CheckIfStorageAvailable();
      if (this.cloudProvider == null)
        throw new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
      cancellationToken.ThrowIfCancellationRequested();
      if (progress == null)
        progress = new ProgressTrackerPrimitiveBase();
      if (!forceUpdate && this.IsBandEphemerisFileGood())
      {
        progress.Complete();
        return false;
      }
      progress.AddStepsTotal(3);
      cancellationToken.ThrowIfCancellationRequested();
      this.VerifyLocalEphemerisFile(cancellationToken, progress, forceUpdate);
      progress.AddStepsCompleted(1);
      cancellationToken.ThrowIfCancellationRequested();
      string relativePath = Path.Combine(new string[2]
      {
        "Ephemeris",
        "EphemerisUpdate.bin"
      });
      using (Stream ephemerisData = this.storageProvider.OpenFileForRead(relativePath))
        this.UpdateDeviceEphemerisData(ephemerisData, (int) this.storageProvider.GetFileSize(relativePath));
      progress.Complete();
      return true;
    }

    private bool IsBandEphemerisFileGood()
    {
      EphemerisCoverageDates coverageDatesFromDevice = this.GetGpsEphemerisCoverageDatesFromDevice();
      if (coverageDatesFromDevice.StartDate.HasValue && coverageDatesFromDevice.EndDate.HasValue)
      {
        DateTime? endDate = coverageDatesFromDevice.EndDate;
        DateTime? nullable = coverageDatesFromDevice.StartDate;
        if ((endDate.HasValue & nullable.HasValue ? (endDate.GetValueOrDefault() <= nullable.GetValueOrDefault() ? 1 : 0) : 0) == 0)
        {
          nullable = coverageDatesFromDevice.EndDate;
          DateTime dateTime1 = nullable.Value;
          nullable = coverageDatesFromDevice.StartDate;
          DateTime dateTime2 = nullable.Value;
          double num = (dateTime1 - dateTime2).TotalSeconds * 0.5;
          DateTime utcNow = DateTime.UtcNow;
          nullable = coverageDatesFromDevice.StartDate;
          DateTime dateTime3 = nullable.Value;
          return (utcNow - dateTime3).TotalSeconds <= num;
        }
      }
      return false;
    }

    private void VerifyLocalEphemerisFile(
      CancellationToken cancellationToken,
      ProgressTrackerPrimitiveBase progress,
      bool forceUpdate)
    {
      string str1 = string.Format("{0}.temp", new object[1]
      {
        (object) "EphemerisVersion.json"
      });
      string str2 = string.Format("{0}.temp", new object[1]
      {
        (object) "EphemerisUpdate.bin"
      });
      string relativePath1 = Path.Combine(new string[2]
      {
        "Ephemeris",
        "EphemerisVersion.json"
      });
      string relativePath2 = Path.Combine(new string[2]
      {
        "Ephemeris",
        "EphemerisUpdate.bin"
      });
      string str3 = Path.Combine(new string[2]
      {
        "Ephemeris",
        str1
      });
      string str4 = Path.Combine(new string[2]
      {
        "Ephemeris",
        str2
      });
      EphemerisCloudVersion ephemerisCloudVersion = (EphemerisCloudVersion) null;
      bool flag = false;
      this.storageProvider.CreateFolder("Ephemeris");
      DateTime? lastFileUpdatedTime1;
      if (!forceUpdate)
      {
        if (this.storageProvider.FileExists(relativePath1))
        {
          try
          {
            using (Stream inputStream = this.storageProvider.OpenFileForRead(relativePath1, -1))
              ephemerisCloudVersion = CargoClient.DeserializeJson<EphemerisCloudVersion>(inputStream);
          }
          catch
          {
          }
          flag = this.storageProvider.FileExists(Path.Combine(new string[2]
          {
            "Ephemeris",
            "EphemerisUpdate.bin"
          }));
          if (flag && ephemerisCloudVersion != null)
          {
            lastFileUpdatedTime1 = ephemerisCloudVersion.LastFileUpdatedTime;
            if (lastFileUpdatedTime1.HasValue)
            {
              lastFileUpdatedTime1 = ephemerisCloudVersion.LastFileUpdatedTime;
              DateTime dateTime = DateTime.UtcNow.AddHours(-12.0);
              if ((lastFileUpdatedTime1.HasValue ? (lastFileUpdatedTime1.GetValueOrDefault() >= dateTime ? 1 : 0) : 0) != 0)
                return;
            }
          }
        }
      }
      EphemerisCloudVersion ephemerisVersion = this.cloudProvider.GetEphemerisVersion(cancellationToken);
      progress.AddStepsCompleted(1);
      if (!forceUpdate & flag && ephemerisCloudVersion != null && ephemerisCloudVersion.EphemerisFileHeaderDataUrl == ephemerisVersion.EphemerisFileHeaderDataUrl && ephemerisCloudVersion.EphemerisProcessedFileDataUrl == ephemerisVersion.EphemerisProcessedFileDataUrl)
      {
        lastFileUpdatedTime1 = ephemerisCloudVersion.LastFileUpdatedTime;
        DateTime? lastFileUpdatedTime2 = ephemerisVersion.LastFileUpdatedTime;
        if ((lastFileUpdatedTime1.HasValue & lastFileUpdatedTime2.HasValue ? (lastFileUpdatedTime1.GetValueOrDefault() >= lastFileUpdatedTime2.GetValueOrDefault() ? 1 : 0) : 0) != 0)
          return;
      }
      cancellationToken.ThrowIfCancellationRequested();
      try
      {
        using (Stream outputStream = this.storageProvider.OpenFileForWrite(str3, false, 1024))
          CargoClient.SerializeJson(outputStream, (object) ephemerisVersion);
      }
      catch (Exception ex)
      {
        throw new BandException(CommonSR.EphemerisVersionDownloadError, ex);
      }
      cancellationToken.ThrowIfCancellationRequested();
      try
      {
        using (Stream updateStream = this.storageProvider.OpenFileForWrite(str4, false))
        {
          if (!this.cloudProvider.GetEphemeris(ephemerisVersion, updateStream, cancellationToken))
            throw new BandException(CommonSR.EphemerisDownloadError);
        }
      }
      catch (OperationCanceledException ex)
      {
        throw;
      }
      catch (BandException ex)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new BandException(CommonSR.EphemerisDownloadError, ex);
      }
      cancellationToken.ThrowIfCancellationRequested();
      this.storageProvider.DeleteFile(relativePath1);
      this.storageProvider.DeleteFile(relativePath2);
      try
      {
        this.storageProvider.RenameFile(str4, "Ephemeris", "EphemerisUpdate.bin");
        this.storageProvider.RenameFile(str3, "Ephemeris", "EphemerisVersion.json");
      }
      catch (Exception ex)
      {
        throw new BandException(CommonSR.EphemerisDownloadError, ex);
      }
    }

    public Task<uint> GetTimeZonesDataVersionFromDeviceAsync() => Task.Run<uint>((Func<uint>) (() => this.GetTimeZonesDataVersionFromDevice()));

    public uint GetTimeZonesDataVersionFromDevice()
    {
      this.CheckIfDisconnectedOrUpdateMode();
      uint version = 0;
      Action<ICargoReader> readData = (Action<ICargoReader>) (r => version = r.ReadUInt32());
      try
      {
        this.ProtocolRead(DeviceCommands.CargoTimeZoneFileGetVersion, 4, readData);
      }
      catch (Exception ex)
      {
        Logger.LogException(LogLevel.Info, ex, "TimeZoneData Version was not found.  Proceeding as if no TimeZoneData file on device");
        version = 0U;
      }
      return version;
    }

    public Task<bool> UpdateTimeZoneListAsync(IUserProfile profile = null) => Task.Run<bool>((Func<bool>) (() => this.UpdateTimeZoneList(CancellationToken.None, (KdkSyncProgress) null, false, profile)));

    public bool UpdateTimeZoneList(IUserProfile profile = null) => this.UpdateTimeZoneList(CancellationToken.None, (KdkSyncProgress) null, false, profile);

    public Task<bool> UpdateTimeZoneListAsync(
      CancellationToken cancellationToken,
      bool forceUpdate = false,
      IUserProfile profile = null)
    {
      return Task.Run<bool>((Func<bool>) (() => this.UpdateTimeZoneList(cancellationToken, (KdkSyncProgress) null, forceUpdate, profile)));
    }

    public bool UpdateTimeZoneList(
      CancellationToken cancellationToken,
      bool forceUpdate = false,
      IUserProfile profile = null)
    {
      return this.UpdateTimeZoneList(cancellationToken, (KdkSyncProgress) null, forceUpdate, profile);
    }

    private bool UpdateTimeZoneList(
      CancellationToken cancellationToken,
      KdkSyncProgress progress,
      bool forceUpdate,
      IUserProfile profile)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.CheckIfStorageAvailable();
      if (this.cloudProvider == null)
      {
        InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      cancellationToken.ThrowIfCancellationRequested();
      if (progress != null)
      {
        progress.SetState(SyncState.TimeZoneData);
        progress.TimeZoneProgress.AddStepsTotal(3);
      }
      TimeZoneDataCloudVersion dataCloudVersion = (TimeZoneDataCloudVersion) null;
      string relativePath1 = Path.Combine(new string[2]
      {
        "TimeZoneData",
        "TimeZoneData.json"
      });
      if (!forceUpdate && this.GetTimeZonesDataVersionFromDevice() == 0U)
        forceUpdate = true;
      if (profile == null)
        profile = (IUserProfile) this.GetDeviceMasteredUserProfileProperties();
      DateTime? nullable1;
      if (!forceUpdate)
      {
        if (this.storageProvider.FileExists(relativePath1))
        {
          try
          {
            using (Stream inputStream = this.storageProvider.OpenFileForRead(relativePath1, -1))
              dataCloudVersion = CargoClient.DeserializeJson<TimeZoneDataCloudVersion>(inputStream);
          }
          catch (Exception ex)
          {
            Logger.LogException(LogLevel.Warning, ex, "Exception occurred when reading the local timeZone version file");
            dataCloudVersion = (TimeZoneDataCloudVersion) null;
          }
        }
        if (dataCloudVersion != null)
        {
          nullable1 = dataCloudVersion.LastModifiedDateTimeDevice;
          if (nullable1.HasValue)
          {
            nullable1 = dataCloudVersion.LastCloudCheckDateTime;
            if (nullable1.HasValue)
            {
              try
              {
                DateTime utcNow1 = DateTime.UtcNow;
                nullable1 = dataCloudVersion.LastModifiedDateTimeDevice;
                DateTime dateTime1 = nullable1.Value;
                TimeSpan timeSpan1 = utcNow1 - dateTime1;
                DateTime utcNow2 = DateTime.UtcNow;
                nullable1 = dataCloudVersion.LastCloudCheckDateTime;
                DateTime dateTime2 = nullable1.Value;
                TimeSpan timeSpan2 = utcNow2 - dateTime2;
                if (dataCloudVersion.Language != profile.DeviceSettings.LocaleSettings.Language)
                {
                  Logger.Log(LogLevel.Info, "Time Zone Data language does not match band language. Forcing download and transfer to device.");
                  dataCloudVersion = (TimeZoneDataCloudVersion) null;
                }
                else
                {
                  if (Math.Abs(timeSpan2.Days) < 1)
                  {
                    Logger.Log(LogLevel.Info, "Time Zone Data downloaded within {0} day(s). No version or data downloads needed.", (object) 1);
                    progress?.TimeZoneProgress.Complete();
                    return false;
                  }
                  if (Math.Abs(timeSpan1.Days) > 60)
                  {
                    Logger.Log(LogLevel.Info, "Time Zone Data downloaded over {0} day(s) ago. Forcing download and transfer to device.", (object) 60);
                    dataCloudVersion = (TimeZoneDataCloudVersion) null;
                  }
                }
              }
              catch
              {
              }
            }
          }
        }
      }
      TimeZoneDataCloudVersion timeZoneDataVersion = this.cloudProvider.GetTimeZoneDataVersion(profile, cancellationToken);
      if (timeZoneDataVersion == null)
      {
        BandCloudException e = new BandCloudException(CommonSR.TimeZoneDataVersionDownloadError);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      progress?.TimeZoneProgress.AddStepsCompleted(1);
      stopwatch.Stop();
      Logger.Log(LogLevel.Info, "Time to get Time Zone Data Version data: {0}", (object) stopwatch.Elapsed);
      cancellationToken.ThrowIfCancellationRequested();
      DateTime? nullable2;
      if (dataCloudVersion != null)
      {
        nullable1 = dataCloudVersion.LastModifiedDateTime;
        nullable2 = timeZoneDataVersion.LastModifiedDateTime;
        if ((nullable1.HasValue == nullable2.HasValue ? (nullable1.HasValue ? (nullable1.GetValueOrDefault() != nullable2.GetValueOrDefault() ? 1 : 0) : 0) : 1) == 0)
        {
          progress.TimeZoneProgress.AddStepsCompleted(2);
          Logger.Log(LogLevel.Info, "Time Zone Data is recent enough and matches cloud. No data download needed.");
          goto label_56;
        }
      }
      stopwatch.Reset();
      stopwatch.Start();
      string relativePath2 = Path.Combine(new string[2]
      {
        "TimeZoneData",
        "TimeZoneUpdate.bin"
      });
      string str = Path.Combine(new string[2]
      {
        "TimeZoneData",
        "TimeZoneUpdateTemp.bin"
      });
      this.storageProvider.CreateFolder("TimeZoneData");
      if (this.storageProvider.FileExists(str))
        this.storageProvider.DeleteFile(str);
      Stream updateStream;
      try
      {
        updateStream = this.storageProvider.OpenFileForWrite(str, false);
      }
      catch (Exception ex)
      {
        BandException e = new BandException(string.Format(CommonSR.TimeZoneDownloadTempFileOpenError, new object[1]
        {
          (object) str
        }), ex);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      bool flag = false;
      using (updateStream)
        flag = this.cloudProvider.GetTimeZoneData(timeZoneDataVersion, profile, updateStream);
      int fileSize = (int) this.storageProvider.GetFileSize(str);
      if (!flag || fileSize == 0)
      {
        BandCloudException e = new BandCloudException(CommonSR.TimeZoneDataDownloadError);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      this.storageProvider.RenameFile(str, "TimeZoneData", "TimeZoneUpdate.bin");
      stopwatch.Stop();
      Logger.Log(LogLevel.Info, "Time to get Time Zone Data: {0}", (object) stopwatch.Elapsed);
      progress?.TimeZoneProgress.AddStepsCompleted(1);
      stopwatch.Reset();
      stopwatch.Start();
      cancellationToken.ThrowIfCancellationRequested();
      using (Stream timeZonesData = this.storageProvider.OpenFileForRead(relativePath2))
        this.UpdateDeviceTimeZonesData(timeZonesData, fileSize);
      stopwatch.Stop();
      Logger.Log(LogLevel.Info, "Time to transfer Time Zone Data to device: {0}", (object) stopwatch.Elapsed);
      timeZoneDataVersion.LastModifiedDateTimeDevice = new DateTime?(DateTime.UtcNow);
      progress?.TimeZoneProgress.AddStepsCompleted(1);
label_56:
      using (Stream outputStream = this.storageProvider.OpenFileForWrite(relativePath1, false))
      {
        nullable2 = timeZoneDataVersion.LastModifiedDateTimeDevice;
        if (!nullable2.HasValue && dataCloudVersion != null)
        {
          nullable2 = dataCloudVersion.LastModifiedDateTimeDevice;
          if (nullable2.HasValue)
            timeZoneDataVersion.LastModifiedDateTimeDevice = dataCloudVersion.LastModifiedDateTimeDevice;
        }
        timeZoneDataVersion.LastCloudCheckDateTime = new DateTime?(DateTime.UtcNow);
        timeZoneDataVersion.Language = profile.DeviceSettings.LocaleSettings.Language;
        CargoClient.SerializeJson(outputStream, (object) timeZoneDataVersion);
      }
      Logger.Log(LogLevel.Info, "TimeZone data version file saved locally");
      progress?.TimeZoneProgress.Complete();
      return true;
    }

    private bool GetCrashDumpFileFromDeviceAndPushToCloud(
      ProgressTrackerPrimitive progress,
      CancellationToken cancellationToken)
    {
      return this.GetFileFromDeviceAndPushToCloud(FileIndex.CrashDump, progress, cancellationToken, false);
    }

    private bool GetInstrumentationFileFromDeviceAndPushToCloud(
      ProgressTrackerPrimitive progress,
      CancellationToken cancellationToken,
      bool doNeedToDownloadCheckBasedOnVersionFile)
    {
      return this.GetFileFromDeviceAndPushToCloud(FileIndex.Instrumentation, progress, cancellationToken, doNeedToDownloadCheckBasedOnVersionFile);
    }

    private bool GetFileFromDeviceAndPushToCloud(
      FileIndex fileIndex,
      ProgressTrackerPrimitive progress,
      CancellationToken cancellationToken,
      bool checkVersionFileBeforeDownload)
    {
      Logger.Log(LogLevel.Info, "Getting {0} file from device and pushing to cloud", (object) fileIndex.ToString());
      if (fileIndex != FileIndex.CrashDump && fileIndex != FileIndex.Instrumentation)
      {
        ArgumentException e = new ArgumentException(CommonSR.UnsupportedFileTypeToObtainFromDevice);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      progress.AddStepsTotal(2);
      bool deviceAndPushToCloud = true;
      string str = fileIndex.ToString();
      if (this.storageProvider.DirectoryExists(str) && this.storageProvider.GetFiles(str).Length >= 20)
        deviceAndPushToCloud = false;
      if (deviceAndPushToCloud)
        deviceAndPushToCloud = this.GetFileFromDevice(fileIndex, checkVersionFileBeforeDownload);
      progress.AddStepsCompleted(1);
      this.PushLocalFilesToCloud(fileIndex, cancellationToken);
      progress.Complete();
      return deviceAndPushToCloud;
    }

    private DeviceFileSyncTimeInfo GetLocalDeviceFileSyncTimeInfo(
      string deviceFileSyncTimeInfoFileRelativePath)
    {
      DeviceFileSyncTimeInfo fileSyncTimeInfo = (DeviceFileSyncTimeInfo) null;
      if (this.storageProvider.FileExists(deviceFileSyncTimeInfoFileRelativePath))
      {
        try
        {
          using (Stream inputStream = this.storageProvider.OpenFileForRead(deviceFileSyncTimeInfoFileRelativePath, -1))
            fileSyncTimeInfo = CargoClient.DeserializeJson<DeviceFileSyncTimeInfo>(inputStream);
        }
        catch (Exception ex)
        {
          Logger.LogException(LogLevel.Warning, ex, "Exception occurred when reading local deviceFileSync time info");
        }
      }
      return fileSyncTimeInfo;
    }

    private bool NeedToDownloadFileFromDevice(
      FileIndex fileIndex,
      DeviceFileSyncTimeInfo localDeviceFileSyncTimeInfo)
    {
      if (localDeviceFileSyncTimeInfo != null)
      {
        TimeSpan zero = TimeSpan.Zero;
        if (fileIndex == FileIndex.Instrumentation)
        {
          TimeSpan timeSpan1 = TimeSpan.FromHours(168.0);
          DateTime? downloadAttemptTime = localDeviceFileSyncTimeInfo.LastDeviceFileDownloadAttemptTime;
          if (downloadAttemptTime.HasValue)
          {
            TimeSpan timeSpan2 = DateTime.UtcNow - downloadAttemptTime.Value;
            bool downloadFileFromDevice = timeSpan2 >= timeSpan1;
            Logger.Log(LogLevel.Info, "Device file check {0}required; File Index: {1}, Minimum Time Between Checks: {2}, Last Checked: {3:MM/dd/yyyy HH:mm} ({4})", downloadFileFromDevice ? (object) "" : (object) "not ", (object) fileIndex, (object) timeSpan1, (object) downloadAttemptTime.Value.ToLocalTime(), (object) timeSpan2);
            return downloadFileFromDevice;
          }
        }
        else
        {
          ArgumentException e = new ArgumentException(CommonSR.UnsupportedFileTypeToObtainFromDevice);
          Logger.LogException(LogLevel.Warning, (Exception) e);
          throw e;
        }
      }
      Logger.Log(LogLevel.Info, "Device file check required; File Index: {0}, Last Checked: <unknown>", (object) fileIndex);
      return true;
    }

    private void SaveDeviceFileSyncTimeInfoLocally(
      string localDeviceFileSyncTimeInfoFileRelativePath,
      DeviceFileSyncTimeInfo localDeviceFileSyncTimeInfo,
      FileIndex fileIndex)
    {
      if (fileIndex == FileIndex.Instrumentation)
      {
        if (localDeviceFileSyncTimeInfo == null)
          localDeviceFileSyncTimeInfo = new DeviceFileSyncTimeInfo();
        localDeviceFileSyncTimeInfo.LastDeviceFileDownloadAttemptTime = new DateTime?(DateTime.UtcNow);
        this.storageProvider.CreateFolder(fileIndex.ToString());
        using (Stream outputStream = this.storageProvider.OpenFileForWrite(localDeviceFileSyncTimeInfoFileRelativePath, false, 512))
          CargoClient.SerializeJson(outputStream, (object) localDeviceFileSyncTimeInfo);
        Logger.Log(LogLevel.Info, "Saved the deviceFileSyncTimeInfo data into local file");
      }
      else
      {
        ArgumentException e = new ArgumentException(CommonSR.UnsupportedFileTypeToObtainFromDevice);
        Logger.LogException(LogLevel.Warning, (Exception) e);
        throw e;
      }
    }

    private bool GetFileFromDevice(FileIndex fileIndex, bool checkVersionFileBeforeDownload)
    {
      string folderRelativePath = fileIndex.ToString();
      ushort deviceCommand;
      ushort commandId;
      switch (fileIndex)
      {
        case FileIndex.Instrumentation:
          deviceCommand = DeviceCommands.CargoInstrumentationGetFileSize;
          commandId = DeviceCommands.CargoInstrumentationGetFile;
          break;
        case FileIndex.CrashDump:
          deviceCommand = DeviceCommands.CargoCrashDumpGetFileSize;
          commandId = DeviceCommands.CargoCrashDumpGetAndDeleteFile;
          break;
        default:
          ArgumentException e = (ArgumentException) new ArgumentOutOfRangeException(nameof (fileIndex), CommonSR.UnsupportedFileTypeToObtainFromDevice);
          Logger.LogException(LogLevel.Error, (Exception) e);
          throw e;
      }
      string str = Path.Combine(new string[2]
      {
        folderRelativePath,
        "DeviceFileSyncTimeInfo.json"
      });
      DeviceFileSyncTimeInfo localDeviceFileSyncTimeInfo = (DeviceFileSyncTimeInfo) null;
      if (checkVersionFileBeforeDownload)
      {
        localDeviceFileSyncTimeInfo = this.GetLocalDeviceFileSyncTimeInfo(str);
        if (!this.NeedToDownloadFileFromDevice(fileIndex, localDeviceFileSyncTimeInfo))
          return false;
      }
      int size = (int) this.DeviceFileGetSize(deviceCommand);
      if (size == 0)
      {
        Logger.Log(LogLevel.Info, "Device file check: File Index: {0}, Not Present", (object) fileIndex);
      }
      else
      {
        Logger.Log(LogLevel.Info, "Device file download starting: File Index: {0}, Size: {1}", (object) fileIndex, (object) size);
        this.storageProvider.CreateFolder(folderRelativePath);
        string format = string.Format("{{0}}-{{1:{0}}}.bin", new object[1]
        {
          (object) "yyyyMMddHHmmssfff"
        });
        using (Stream stream = this.storageProvider.OpenFileForWrite(Path.Combine(new string[2]
        {
          folderRelativePath,
          string.Format(format, new object[2]
          {
            (object) fileIndex,
            (object) DateTime.UtcNow
          })
        }), false))
        {
          try
          {
            using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(commandId, size, CommandStatusHandling.DoNotCheck))
            {
              while (cargoCommandReader.BytesRemaining > 0)
                cargoCommandReader.CopyTo(stream, Math.Min(cargoCommandReader.BytesRemaining, 8192));
              BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
              stream.Flush();
            }
          }
          catch (Exception ex)
          {
            Logger.LogException(LogLevel.Error, (Exception) new BandException(string.Format(CommonSR.DeviceFileDownloadError, new object[1]
            {
              (object) fileIndex
            }), ex));
            throw;
          }
        }
        Logger.Log(LogLevel.Info, "Device file download complete: File Index: {0}, Size: {1}", (object) fileIndex, (object) size);
      }
      if (fileIndex == FileIndex.Instrumentation)
        this.SaveDeviceFileSyncTimeInfoLocally(str, localDeviceFileSyncTimeInfo, fileIndex);
      return size > 0;
    }

    private void PushLocalFilesToCloud(FileIndex index, CancellationToken cancellationToken)
    {
      string str1 = index.ToString();
      if (!this.storageProvider.DirectoryExists(str1))
        return;
      Logger.Log(LogLevel.Info, "Pushing local {0} files to the cloud", (object) str1);
      foreach (string file in this.storageProvider.GetFiles(str1))
      {
        if (!file.Equals("DeviceFileSyncTimeInfo.json"))
        {
          string str2 = str1 + "\\" + file;
          if (this.UploadFileToCloud(str2, index, cancellationToken))
            Logger.Log(LogLevel.Info, "Successfully uploaded file to cloud: {0}", (object) str2);
          else
            Logger.Log(LogLevel.Info, "File was already uploaded to cloud: {0}", (object) str2);
          this.storageProvider.DeleteFile(str2);
        }
      }
    }

    public Task<IFirmwareUpdateInfo> GetLatestAvailableFirmwareVersionAsync(
      List<KeyValuePair<string, string>> queryParams = null)
    {
      return Task.Run<IFirmwareUpdateInfo>((Func<IFirmwareUpdateInfo>) (() => this.GetLatestAvailableFirmwareVersion(queryParams)));
    }

    public IFirmwareUpdateInfo GetLatestAvailableFirmwareVersion(
      List<KeyValuePair<string, string>> queryParams = null)
    {
      return this.GetLatestAvailableFirmwareVersion(CancellationToken.None, queryParams);
    }

    public Task<IFirmwareUpdateInfo> GetLatestAvailableFirmwareVersionAsync(
      CancellationToken cancellationToken,
      List<KeyValuePair<string, string>> queryParams = null)
    {
      return Task.Run<IFirmwareUpdateInfo>((Func<IFirmwareUpdateInfo>) (() => this.GetLatestAvailableFirmwareVersion(cancellationToken, queryParams)));
    }

    public IFirmwareUpdateInfo GetLatestAvailableFirmwareVersion(
      CancellationToken cancellationToken,
      List<KeyValuePair<string, string>> queryParams = null)
    {
      Logger.Log(LogLevel.Info, "Getting latest available firmware version");
      this.CheckIfDisposed();
      if (this.cloudProvider == null)
      {
        InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (this.DeviceTransport == null)
      {
        InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      bool firmwareOnDeviceValid = false;
      if (this.DeviceTransportApp == RunningAppType.App)
        firmwareOnDeviceValid = this.GetFirmwareBinariesValidationStatus();
      return this.GetLatestAvailableFirmwareVersion(cancellationToken, this.FirmwareVersions, firmwareOnDeviceValid, queryParams);
    }

    internal IFirmwareUpdateInfo GetLatestAvailableFirmwareVersion(
      CancellationToken cancellationToken,
      FirmwareVersions deviceVersions,
      bool firmwareOnDeviceValid,
      List<KeyValuePair<string, string>> queryParams = null)
    {
      FirmwareUpdateInfo availableFirmwareVersion = this.cloudProvider.GetLatestAvailableFirmwareVersion(deviceVersions, firmwareOnDeviceValid, queryParams, cancellationToken);
      if (availableFirmwareVersion == null)
      {
        BandCloudException e = new BandCloudException(CommonSR.FirmwareUpdateInfoError);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (availableFirmwareVersion.IsFirmwareUpdateAvailable)
      {
        Logger.Log(LogLevel.Info, "Firmware update is available: Version: {0}", (object) availableFirmwareVersion.FirmwareVersion);
        try
        {
          int.Parse(availableFirmwareVersion.SizeInBytes);
        }
        catch
        {
          BandException e = new BandException(CommonSR.InvalidUpdateDataSize);
          Logger.LogException(LogLevel.Error, (Exception) e);
          throw e;
        }
      }
      return (IFirmwareUpdateInfo) availableFirmwareVersion;
    }

    public Task<bool> UpdateFirmwareAsync(
      IFirmwareUpdateInfo updateInfo,
      IProgress<FirmwareUpdateProgress> progress = null)
    {
      return Task.Run<bool>((Func<bool>) (() => this.UpdateFirmware(updateInfo, CancellationToken.None, progress)));
    }

    public Task<bool> UpdateFirmwareAsync(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null)
    {
      return Task.Run<bool>((Func<bool>) (() => this.UpdateFirmware(updateInfo, cancellationToken, progress)));
    }

    public bool UpdateFirmware(
      IFirmwareUpdateInfo updateInfo,
      IProgress<FirmwareUpdateProgress> progress = null)
    {
      return this.UpdateFirmware(updateInfo, CancellationToken.None, progress);
    }

    public bool UpdateFirmware(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null)
    {
      if (updateInfo == null)
        throw new ArgumentNullException(nameof (updateInfo));
      if (!(updateInfo is FirmwareUpdateInfo updateInfo1))
        throw new ArgumentException("Unexpected implementation", nameof (updateInfo));
      if (!updateInfo.IsFirmwareUpdateAvailable)
        return false;
      FirmwareUpdateOverallProgress progressTracker = new FirmwareUpdateOverallProgress(progress, FirmwareUpdateOperation.DownloadAndUpdate);
      this.DownloadFirmwareUpdateInternal(updateInfo1, cancellationToken, progressTracker);
      cancellationToken.ThrowIfCancellationRequested();
      int num = this.PushFirmwareUpdateToDeviceInternal(updateInfo1, cancellationToken, progressTracker) ? 1 : 0;
      progressTracker.SetState(FirmwareUpdateState.Done);
      return num != 0;
    }

    public Task DownloadFirmwareUpdateAsync(IFirmwareUpdateInfo updateInfo) => Task.Run((Action) (() => this.DownloadFirmwareUpdate(updateInfo, CancellationToken.None, (IProgress<FirmwareUpdateProgress>) null)));

    public void DownloadFirmwareUpdate(IFirmwareUpdateInfo updateInfo) => this.DownloadFirmwareUpdate(updateInfo, CancellationToken.None, (IProgress<FirmwareUpdateProgress>) null);

    public Task DownloadFirmwareUpdateAsync(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null)
    {
      return Task.Run((Action) (() => this.DownloadFirmwareUpdate(updateInfo, cancellationToken, progress)));
    }

    public void DownloadFirmwareUpdate(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null)
    {
      if (updateInfo == null)
        throw new ArgumentNullException(nameof (updateInfo));
      if (!(updateInfo is FirmwareUpdateInfo updateInfo1))
        throw new ArgumentException("Unexpected implementation", nameof (updateInfo));
      this.CheckIfDisposed();
      this.CheckIfStorageAvailable();
      FirmwareUpdateOverallProgress progressTracker = new FirmwareUpdateOverallProgress(progress, FirmwareUpdateOperation.DownloadOnly);
      this.DownloadFirmwareUpdateInternal(updateInfo1, cancellationToken, progressTracker);
      progressTracker.SetState(FirmwareUpdateState.Done);
    }

    public Task<bool> PushFirmwareUpdateToDeviceAsync(IFirmwareUpdateInfo updateInfo) => Task.Run<bool>((Func<bool>) (() => this.PushFirmwareUpdateToDevice(updateInfo, CancellationToken.None, (IProgress<FirmwareUpdateProgress>) null)));

    public Task<bool> PushFirmwareUpdateToDeviceAsync(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null)
    {
      return Task.Run<bool>((Func<bool>) (() => this.PushFirmwareUpdateToDevice(updateInfo, cancellationToken, progress)));
    }

    public bool PushFirmwareUpdateToDevice(IFirmwareUpdateInfo updateInfo) => this.PushFirmwareUpdateToDevice(updateInfo, CancellationToken.None, (IProgress<FirmwareUpdateProgress>) null);

    public bool PushFirmwareUpdateToDevice(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null)
    {
      if (updateInfo == null)
        throw new ArgumentNullException(nameof (updateInfo));
      if (!(updateInfo is FirmwareUpdateInfo updateInfo1))
        throw new ArgumentException("Unexpected implementation", nameof (updateInfo));
      this.CheckIfDisposed();
      this.CheckIfStorageAvailable();
      FirmwareUpdateOverallProgress progressTracker = new FirmwareUpdateOverallProgress(progress, FirmwareUpdateOperation.UpdateOnly);
      int num = this.PushFirmwareUpdateToDeviceInternal(updateInfo1, cancellationToken, progressTracker) ? 1 : 0;
      progressTracker.SetState(FirmwareUpdateState.Done);
      return num != 0;
    }

    internal static JsonSerializerSettings GetJsonSerializerSettings() => new JsonSerializerSettings()
    {
      NullValueHandling = NullValueHandling.Ignore
    };

    internal static string SerializeJson(object value) => JsonConvert.SerializeObject(value, CargoClient.GetJsonSerializerSettings());

    internal static void SerializeJson(Stream outputStream, object value)
    {
      using (StreamWriter streamWriter = new StreamWriter(outputStream, Encoding.UTF8, 128, true))
      {
        using (JsonWriter jsonWriter = (JsonWriter) new JsonTextWriter((TextWriter) streamWriter))
          JsonSerializer.Create(CargoClient.GetJsonSerializerSettings()).Serialize(jsonWriter, value);
      }
    }

    internal static T DeserializeJson<T>(Stream inputStream)
    {
      using (StreamReader inputStream1 = new StreamReader(inputStream, Encoding.UTF8, false, 128, true))
        return CargoClient.DeserializeJson<T>((TextReader) inputStream1);
    }

    internal static T DeserializeJson<T>(string input)
    {
      using (StringReader inputStream = new StringReader(input))
        return CargoClient.DeserializeJson<T>((TextReader) inputStream);
    }

    internal static T DeserializeJson<T>(TextReader inputStream)
    {
      using (JsonReader reader = (JsonReader) new JsonTextReader(inputStream))
        return JsonSerializer.Create(CargoClient.GetJsonSerializerSettings()).Deserialize<T>(reader);
    }

    public Task UpdateLogProcessingAsync(
      List<LogProcessingStatus> fileInfoList,
      EventHandler<LogProcessingUpdatedEventArgs> notificationHandler,
      bool singleCallback,
      CancellationToken cancellationToken)
    {
      return Task.Run((Action) (() => this.UpdateLogProcessing(fileInfoList, notificationHandler, singleCallback, cancellationToken)));
    }

    public void UpdateLogProcessing(
      List<LogProcessingStatus> fileInfoList,
      EventHandler<LogProcessingUpdatedEventArgs> notificationHandler,
      bool singleCallback,
      CancellationToken cancellationToken)
    {
      if (fileInfoList == null)
        throw new ArgumentNullException(nameof (fileInfoList));
      Logger.Log(LogLevel.Info, "UpdateLogProcessing Called; Files: {0}", (object) fileInfoList.Count);
      if (fileInfoList.Count == 0)
        return;
      TimeSpan timeSpan1 = TimeSpan.FromSeconds(2.0);
      TimeSpan timeout = TimeSpan.FromSeconds(4.0);
      LinkedList<LogProcessingStatus> source = new LinkedList<LogProcessingStatus>();
      Dictionary<string, LogProcessingStatus> dictionary = new Dictionary<string, LogProcessingStatus>();
      List<LogProcessingStatus> completedFiles = new List<LogProcessingStatus>();
      List<LogProcessingStatus> notRecognizedFiles = new List<LogProcessingStatus>();
      int num1 = 0;
      bool flag = true;
      foreach (LogProcessingStatus processingStatus in (IEnumerable<LogProcessingStatus>) fileInfoList.OrderBy<LogProcessingStatus, DateTime>((Func<LogProcessingStatus, DateTime>) (lps => lps.KnownStatus)))
      {
        source.AddLast(processingStatus);
        dictionary.Add(processingStatus.UploadId, processingStatus);
      }
      while (source.Count > 0)
      {
        int num2 = 0;
        int num3 = 0;
        int num4 = 0;
        int num5 = Math.Min(source.Count, 25);
        TimeSpan timeSpan2 = DateTime.UtcNow - source.Take<LogProcessingStatus>(25).Last<LogProcessingStatus>().KnownStatus;
        if (timeSpan2 < timeSpan1)
          cancellationToken.WaitAndThrowIfCancellationRequested(timeSpan1 - timeSpan2);
        Logger.Log(LogLevel.Info, "Executing Upload Status Query; File IDs: {0}", (object) num5);
        Dictionary<string, LogUploadStatusInfo> processingUpdate;
        try
        {
          processingUpdate = this.cloudProvider.GetLogProcessingUpdate(source.Take<LogProcessingStatus>(25).Select<LogProcessingStatus, string>((Func<LogProcessingStatus, string>) (lps => lps.UploadId)), cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
          throw;
        }
        catch (Exception ex)
        {
          Logger.LogException(LogLevel.Error, ex);
          if (++num1 >= 3)
          {
            BandException bandException = new BandException(CommonSR.LogProcessingStatusDownloadError);
            Logger.Log(LogLevel.Error, "UpdateLogProcessing stopped due to repeated failures to downloaded logs.");
            foreach (LogProcessingStatus processingStatus in source)
              Logger.Log(LogLevel.Error, "Log File: Upload Id: {0}, Status: Unknown (multiple attempts failed)", (object) processingStatus.UploadId);
            throw bandException;
          }
          cancellationToken.WaitAndThrowIfCancellationRequested(timeout);
          continue;
        }
        if (num1 > 0)
          --num1;
        DateTime utcNow = DateTime.UtcNow;
        for (int index = 0; index < num5; ++index)
        {
          LogProcessingStatus processingStatus = source.First.Value;
          source.RemoveFirst();
          LogUploadStatusInfo uploadStatusInfo;
          if (!processingUpdate.TryGetValue(processingStatus.UploadId, out uploadStatusInfo))
          {
            processingStatus.KnownStatus = utcNow;
            source.AddLast(processingStatus);
          }
          else
          {
            switch (uploadStatusInfo.UploadStatus)
            {
              case LogUploadStatus.UploadPathSent:
              case LogUploadStatus.QueuedForETL:
              case LogUploadStatus.ActivitiesProcessingDone:
              case LogUploadStatus.EventsProcessingBlocked:
                processingStatus.KnownStatus = utcNow;
                source.AddLast(processingStatus);
                if (!dictionary.ContainsKey(processingStatus.UploadId))
                {
                  dictionary.Add(processingStatus.UploadId, processingStatus);
                  flag = true;
                }
                ++num3;
                continue;
              case LogUploadStatus.UploadDone:
              case LogUploadStatus.EventsProcessingDone:
                completedFiles.Add(processingStatus);
                flag = true;
                dictionary.Remove(processingStatus.UploadId);
                ++num2;
                continue;
              default:
                notRecognizedFiles.Add(processingStatus);
                flag = true;
                ++num4;
                continue;
            }
          }
        }
        Logger.Log(LogLevel.Info, "Upload Status Query Result: Complete: {0}, Still Processing: {1}, Not Recognized: {2}", (object) num2, (object) num3, (object) num4);
        if (notificationHandler != null & flag && !singleCallback)
        {
          this.DoLogCallback(notificationHandler, (IEnumerable<LogProcessingStatus>) completedFiles, (IEnumerable<LogProcessingStatus>) dictionary.Values, (IEnumerable<LogProcessingStatus>) notRecognizedFiles);
          flag = false;
        }
      }
      if (!(notificationHandler != null & flag))
        return;
      this.DoLogCallback(notificationHandler, (IEnumerable<LogProcessingStatus>) completedFiles, (IEnumerable<LogProcessingStatus>) dictionary.Values, (IEnumerable<LogProcessingStatus>) notRecognizedFiles);
    }

    private void DoLogCallback(
      EventHandler<LogProcessingUpdatedEventArgs> notificationHandler,
      IEnumerable<LogProcessingStatus> completedFiles,
      IEnumerable<LogProcessingStatus> processingFiles,
      IEnumerable<LogProcessingStatus> notRecognizedFiles)
    {
      try
      {
        Logger.Log(LogLevel.Info, "Callback for Log Updates; Complete: {0}, Still Processing: {1}, Not Recognized: {2}", (object) completedFiles.Count<LogProcessingStatus>(), (object) processingFiles.Count<LogProcessingStatus>(), (object) notRecognizedFiles.Count<LogProcessingStatus>());
        notificationHandler((object) this, new LogProcessingUpdatedEventArgs(completedFiles, processingFiles, notRecognizedFiles));
      }
      catch
      {
      }
    }

    public Task SetGoalsAsync(Goals goals) => Task.Run((Action) (() => this.SetGoals(goals)));

    public void SetGoals(Goals goals)
    {
      if (goals == null)
        throw new ArgumentNullException(nameof (goals));
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Setting goals on the device:");
      Logger.Log(LogLevel.Info, "CaloriesEnabled = {0}, CaloriesGoal = {1}", (object) goals.CaloriesEnabled, (object) goals.CaloriesGoal);
      Logger.Log(LogLevel.Info, "DistanceEnabled = {0}, DistanceGoal = {1}", (object) goals.DistanceEnabled, (object) goals.DistanceGoal);
      Logger.Log(LogLevel.Info, "StepsEnabled = {0}, StepsGoal = {1}", (object) goals.StepsEnabled, (object) goals.StepsGoal);
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoGoalTrackerSet, Goals.GetSerializedByteCount(this.ConnectedAdminBandConstants), CommandStatusHandling.DoNotCheck))
      {
        goals.SerializeToBand((ICargoWriter) writer, this.ConnectedAdminBandConstants);
        BandClient.CheckStatus(writer.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
    }

    public Task SetWorkoutPlanAsync(Stream workoutPlansStream) => Task.Run((Action) (() => this.SetWorkoutPlan(workoutPlansStream)));

    public void SetWorkoutPlan(Stream workoutPlansStream)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (workoutPlansStream == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (workoutPlansStream));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      this.UploadFitnessPlan(workoutPlansStream, (int) workoutPlansStream.Length);
    }

    public Task SetWorkoutPlanAsync(byte[] workoutPlansData) => Task.Run((Action) (() => this.SetWorkoutPlan(workoutPlansData)));

    public void SetWorkoutPlan(byte[] workoutPlanData)
    {
      Logger.Log(LogLevel.Info, "Setting workout plan data on the device");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (workoutPlanData == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (workoutPlanData));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      using (MemoryStream fitnessPlan = new MemoryStream(workoutPlanData, false))
        this.UploadFitnessPlan((Stream) fitnessPlan, workoutPlanData.Length);
    }

    public Task<IList<WorkoutActivity>> GetWorkoutActivitiesAsync() => Task.Run<IList<WorkoutActivity>>((Func<IList<WorkoutActivity>>) (() => this.GetWorkoutActivities()));

    internal IList<WorkoutActivity> GetWorkoutActivities()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.CheckIfNotEnvoy();
      int bytesToRead = WorkoutActivity.GetSerializedByteCount() * 15 + 4 + 10;
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoPersistedAppDataGetWorkoutActivities, bytesToRead, CommandStatusHandling.ThrowOnlySeverityError))
      {
        List<WorkoutActivity> source = new List<WorkoutActivity>(15);
        for (int index = 0; index < 15; ++index)
          source.Add(WorkoutActivity.DeserializeFromBand((ICargoReader) reader));
        uint val1 = reader.ReadUInt32();
        reader.ReadExactAndDiscard(10);
        return (IList<WorkoutActivity>) source.Take<WorkoutActivity>((int) Math.Min(val1, 15U)).ToList<WorkoutActivity>();
      }
    }

    public Task SetWorkoutActivitiesAsync(IList<WorkoutActivity> activities) => Task.Run((Action) (() => this.SetWorkoutActivities(activities)));

    internal void SetWorkoutActivities(IList<WorkoutActivity> activities)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.CheckIfNotEnvoy();
      if (activities == null)
        throw new ArgumentNullException(nameof (activities));
      if (activities.Count == 0 || activities.Count > 15)
        throw new ArgumentOutOfRangeException(nameof (activities));
      if (activities.Contains((WorkoutActivity) null))
        throw new InvalidDataException();
      int serializedByteCount = WorkoutActivity.GetSerializedByteCount();
      int dataSize = serializedByteCount * 15 + 4 + 10;
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoPersistedAppDataSetWorkoutActivities, dataSize, CommandStatusHandling.ThrowOnlySeverityError))
      {
        int index;
        for (index = 0; index < activities.Count; ++index)
          activities[index].SerializeToBand((ICargoWriter) writer);
        for (; index < 15; ++index)
          writer.WriteByte((byte) 0, serializedByteCount);
        writer.WriteUInt32((uint) activities.Count);
        writer.WriteByte((byte) 0, 10);
      }
    }

    public Task<int> GetGolfCourseMaxSizeAsync() => Task.Run<int>((Func<int>) (() => this.GetGolfCourseMaxSize()));

    public int GetGolfCourseMaxSize()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoGolfCourseFileGetMaxSize, 4, CommandStatusHandling.ThrowAnyNonZero))
        return cargoCommandReader.ReadInt32();
    }

    public Task SetGolfCourseAsync(Stream golfCourseStream, int length = -1) => Task.Run((Action) (() => this.SetGolfCourse(golfCourseStream, length)));

    public void SetGolfCourse(Stream golfCourseStream, int length = -1)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (golfCourseStream == null)
        throw new ArgumentNullException(nameof (golfCourseStream));
      if (length < 0)
      {
        try
        {
          length = (int) (golfCourseStream.Length - golfCourseStream.Position);
        }
        catch (Exception ex)
        {
          throw new Exception("Unable to discover stream length", ex);
        }
      }
      Logger.Log(LogLevel.Info, "Setting golf course data on the device");
      this.UploadGolfCourse(golfCourseStream, length);
    }

    public Task SetGolfCourseAsync(byte[] golfCourseData) => Task.Run((Action) (() => this.SetGolfCourse(golfCourseData)));

    public void SetGolfCourse(byte[] golfCourseData)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (golfCourseData == null)
        throw new ArgumentNullException(nameof (golfCourseData));
      Logger.Log(LogLevel.Info, "Setting golf course data on the device");
      using (MemoryStream golfCourse = new MemoryStream(golfCourseData, false))
        this.UploadGolfCourse((Stream) golfCourse, golfCourseData.Length);
    }

    public Task NavigateToScreenAsync(CargoScreen screen) => Task.Run((Action) (() => this.NavigateToScreen(screen)));

    public void NavigateToScreen(CargoScreen screen)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Navigating to screen: {0}", (object) screen.ToString());
      Action<ICargoWriter> writeData = (Action<ICargoWriter>) (w => w.WriteUInt16((ushort) screen));
      this.ProtocolWriteWithData(DeviceCommands.CargoFireballUINavigateToScreen, 2, writeData);
    }

    public Task<OobeStage> GetOobeStageAsync() => Task.Run<OobeStage>((Func<OobeStage>) (() => this.GetOobeStage()));

    public OobeStage GetOobeStage()
    {
      OobeStage stage = OobeStage.AskPhoneType;
      Action<ICargoReader> readData = (Action<ICargoReader>) (r => stage = (OobeStage) r.ReadUInt16());
      this.ProtocolRead(DeviceCommands.CargoOobeGetStage, 2, readData);
      return stage;
    }

    public Task SetOobeStageAsync(OobeStage stage) => Task.Run((Action) (() => this.SetOobeStage(stage)));

    public void SetOobeStage(OobeStage stage)
    {
      if (stage >= OobeStage.PreStateCharging)
        throw new ArgumentOutOfRangeException(nameof (stage));
      this.loggerProvider.Log(ProviderLogLevel.Info, "Setting OOBE Stage: {0}", (object) stage);
      Action<ICargoWriter> writeData = (Action<ICargoWriter>) (w => w.WriteUInt16((ushort) stage));
      this.ProtocolWriteWithData(DeviceCommands.CargoOobeSetStage, 2, writeData);
    }

    public Task FinalizeOobeAsync() => Task.Run((Action) (() => this.FinalizeOobe()));

    public void FinalizeOobe() => this.ProtocolWrite(DeviceCommands.CargoOobeFinalize);

    public Task<string[]> GetPhoneCallResponsesAsync() => Task.Run<string[]>((Func<string[]>) (() => this.GetPhoneCallResponses()));

    public string[] GetPhoneCallResponses()
    {
      Logger.Log(LogLevel.Info, "Getting phone call responses");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      string[] allResponses = this.GetAllResponses();
      string[] phoneCallResponses = new string[4];
      string[] destinationArray = phoneCallResponses;
      Array.Copy((Array) allResponses, 0, (Array) destinationArray, 0, 4);
      return phoneCallResponses;
    }

    public Task SetPhoneCallResponsesAsync(
      string response1,
      string response2,
      string response3,
      string response4)
    {
      return Task.Run((Action) (() => this.SetPhoneCallResponses(response1, response2, response3, response4)));
    }

    public void SetPhoneCallResponses(
      string response1,
      string response2,
      string response3,
      string response4)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (response1 == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (response1));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response2 == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (response2));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response3 == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (response3));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response4 == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (response4));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response1.Length > 160)
      {
        ArgumentException e = new ArgumentException(string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2]
        {
          (object) nameof (response1),
          (object) 160
        }));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response2.Length > 160)
      {
        ArgumentException e = new ArgumentException(string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2]
        {
          (object) nameof (response2),
          (object) 160
        }));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response3.Length > 160)
      {
        ArgumentException e = new ArgumentException(string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2]
        {
          (object) nameof (response3),
          (object) 160
        }));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response4.Length > 160)
      {
        ArgumentException e = new ArgumentException(string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2]
        {
          (object) nameof (response4),
          (object) 160
        }));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      Logger.Log(LogLevel.Info, "Setting phone call responses - ");
      this.SetResponse((byte) 0, response1);
      this.SetResponse((byte) 1, response2);
      this.SetResponse((byte) 2, response3);
      this.SetResponse((byte) 3, response4);
    }

    public Task<string[]> GetSmsResponsesAsync() => Task.Run<string[]>((Func<string[]>) (() => this.GetSmsResponses()));

    public string[] GetSmsResponses()
    {
      Logger.Log(LogLevel.Info, "Getting SMS responses");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      string[] allResponses = this.GetAllResponses();
      string[] smsResponses = new string[4];
      string[] destinationArray = smsResponses;
      Array.Copy((Array) allResponses, 4, (Array) destinationArray, 0, 4);
      return smsResponses;
    }

    public Task SetSmsResponsesAsync(
      string response1,
      string response2,
      string response3,
      string response4)
    {
      return Task.Run((Action) (() => this.SetSmsResponses(response1, response2, response3, response4)));
    }

    public void SetSmsResponses(
      string response1,
      string response2,
      string response3,
      string response4)
    {
      if (response1 == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (response1));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response2 == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (response2));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response3 == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (response3));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response4 == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (response4));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (response1.Length > 160)
      {
        Logger.Log(LogLevel.Warning, string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2]
        {
          (object) nameof (response1),
          (object) 160
        }));
        response1 = response1.Substring(0, 160);
      }
      if (response2.Length > 160)
      {
        Logger.Log(LogLevel.Warning, string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2]
        {
          (object) nameof (response2),
          (object) 160
        }));
        response2 = response2.Substring(0, 160);
      }
      if (response3.Length > 160)
      {
        Logger.Log(LogLevel.Warning, string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2]
        {
          (object) nameof (response3),
          (object) 160
        }));
        response3 = response3.Substring(0, 160);
      }
      if (response4.Length > 160)
      {
        Logger.Log(LogLevel.Warning, string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2]
        {
          (object) nameof (response4),
          (object) 160
        }));
        response4 = response4.Substring(0, 160);
      }
      Logger.Log(LogLevel.Info, "Setting SMS responses - ");
      this.SetResponse((byte) 4, response1);
      this.SetResponse((byte) 5, response2);
      this.SetResponse((byte) 6, response3);
      this.SetResponse((byte) 7, response4);
    }

    public Task<CargoRunDisplayMetrics> GetRunDisplayMetricsAsync() => Task.Run<CargoRunDisplayMetrics>((Func<CargoRunDisplayMetrics>) (() => this.GetRunDisplayMetrics()));

    public CargoRunDisplayMetrics GetRunDisplayMetrics()
    {
      Logger.Log(LogLevel.Info, "Getting run display metrics");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      int serializedByteCount = CargoRunDisplayMetrics.GetSerializedByteCount(this.ConnectedAdminBandConstants);
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoPersistedAppDataGetRunMetrics, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError))
        return CargoRunDisplayMetrics.DeserializeFromBand((ICargoReader) reader, this.ConnectedAdminBandConstants);
    }

    public Task SetRunDisplayMetricsAsync(CargoRunDisplayMetrics cargoRunDisplayMetrics) => Task.Run((Action) (() => this.SetRunDisplayMetrics(cargoRunDisplayMetrics)));

    public void SetRunDisplayMetrics(CargoRunDisplayMetrics cargoRunDisplayMetrics)
    {
      Logger.Log(LogLevel.Info, "Setting run display metrics");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (cargoRunDisplayMetrics == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (cargoRunDisplayMetrics));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (!cargoRunDisplayMetrics.IsValid(this.ConnectedAdminBandConstants))
      {
        ArgumentException e = new ArgumentException(CommonSR.InvalidCargoRunDisplayMetrics);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      int serializedByteCount = CargoRunDisplayMetrics.GetSerializedByteCount(this.ConnectedAdminBandConstants);
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoPersistedAppDataSetRunMetrics, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError))
        cargoRunDisplayMetrics.SerializeToBand((ICargoWriter) writer, this.ConnectedAdminBandConstants);
    }

    public Task<CargoBikeDisplayMetrics> GetBikeDisplayMetricsAsync() => Task.Run<CargoBikeDisplayMetrics>((Func<CargoBikeDisplayMetrics>) (() => this.GetBikeDisplayMetrics()));

    public CargoBikeDisplayMetrics GetBikeDisplayMetrics()
    {
      Logger.Log(LogLevel.Info, "Getting bike display metrics");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      int serializedByteCount = CargoBikeDisplayMetrics.GetSerializedByteCount(this.ConnectedAdminBandConstants);
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoPersistedAppDataGetBikeMetrics, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError))
        return CargoBikeDisplayMetrics.DeserializeFromBand((ICargoReader) reader, this.ConnectedAdminBandConstants);
    }

    public Task SetBikeDisplayMetricsAsync(CargoBikeDisplayMetrics cargoBikeDisplayMetrics) => Task.Run((Action) (() => this.SetBikeDisplayMetrics(cargoBikeDisplayMetrics)));

    public void SetBikeDisplayMetrics(CargoBikeDisplayMetrics cargoBikeDisplayMetrics)
    {
      Logger.Log(LogLevel.Info, "Setting bike display metrics");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (cargoBikeDisplayMetrics == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (cargoBikeDisplayMetrics));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (!cargoBikeDisplayMetrics.IsValid(this.ConnectedAdminBandConstants))
      {
        ArgumentException e = new ArgumentException(CommonSR.InvalidCargoBikeDisplayMetrics);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      int serializedByteCount = CargoBikeDisplayMetrics.GetSerializedByteCount(this.ConnectedAdminBandConstants);
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoPersistedAppDataSetBikeMetrics, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError))
        cargoBikeDisplayMetrics.SerializeToBand((ICargoWriter) writer, this.ConnectedAdminBandConstants);
    }

    public Task SetBikeSplitMultiplierAsync(int multiplier) => Task.Run((Action) (() => this.SetBikeSplitMultiplier(multiplier)));

    public void SetBikeSplitMultiplier(int multiplier)
    {
      Logger.Log(LogLevel.Info, "Setting bike split multiplier");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Action<ICargoWriter> writeData = multiplier >= 1 && multiplier <= (int) byte.MaxValue ? (Action<ICargoWriter>) (w => w.WriteInt32(multiplier)) : throw new ArgumentOutOfRangeException(nameof (multiplier));
      this.ProtocolWriteWithData(DeviceCommands.CargoPersistedAppDataSetBikeSplitMult, 4, writeData);
    }

    public Task<int> GetBikeSplitMultiplierAsync() => Task.Run<int>((Func<int>) (() => this.GetBikeSplitMultiplier()));

    public int GetBikeSplitMultiplier()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Getting bike split multiplier");
      int bikeSplitMultiplier = 0;
      Action<ICargoReader> readData = (Action<ICargoReader>) (r => bikeSplitMultiplier = r.ReadInt32());
      this.ProtocolRead(DeviceCommands.CargoPersistedAppDataGetBikeSplitMult, 4, readData);
      return bikeSplitMultiplier;
    }

    public Task<SleepNotification> GetSleepNotificationAsync() => Task.Run<SleepNotification>((Func<SleepNotification>) (() => this.GetSleepNotification()));

    internal SleepNotification GetSleepNotification()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.CheckIfNotEnvoy();
      int serializedByteCount = SleepNotification.GetSerializedByteCount();
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoPersistedAppDataGetSleepNotification, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError))
        return SleepNotification.DeserializeFromBand((ICargoReader) reader);
    }

    public Task SetSleepNotificationAsync(SleepNotification notification) => Task.Run((Action) (() => this.SetSleepNotification(notification)));

    internal void SetSleepNotification(SleepNotification notification)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.CheckIfNotEnvoy();
      if (notification == null)
        throw new ArgumentNullException(nameof (notification));
      int serializedByteCount = SleepNotification.GetSerializedByteCount();
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoPersistedAppDataSetSleepNotification, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError))
        notification.SerializeToBand((ICargoWriter) writer);
    }

    public Task DisableSleepNotificationAsync() => Task.Run((Action) (() => this.DisableSleepNotification()));

    internal void DisableSleepNotification()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.CheckIfNotEnvoy();
      this.ProtocolWrite(DeviceCommands.CargoPersistedAppDataDisableSleepNotification);
    }

    public Task<LightExposureNotification> GetLightExposureNotificationAsync() => Task.Run<LightExposureNotification>((Func<LightExposureNotification>) (() => this.GetLightExposureNotification()));

    internal LightExposureNotification GetLightExposureNotification()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.CheckIfNotEnvoy();
      int serializedByteCount = LightExposureNotification.GetSerializedByteCount();
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoPersistedAppDataGetLightExposureNotification, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError))
        return LightExposureNotification.DeserializeFromBand((ICargoReader) reader);
    }

    public Task SetLightExposureNotificationAsync(LightExposureNotification notification) => Task.Run((Action) (() => this.SetLightExposureNotification(notification)));

    internal void SetLightExposureNotification(LightExposureNotification notification)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.CheckIfNotEnvoy();
      if (notification == null)
        throw new ArgumentNullException(nameof (notification));
      int serializedByteCount = LightExposureNotification.GetSerializedByteCount();
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoPersistedAppDataSetLightExposureNotification, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError))
        notification.SerializeToBand((ICargoWriter) writer);
    }

    public Task DisableLightExposureNotificationAsync() => Task.Run((Action) (() => this.DisableLightExposureNotification()));

    internal void DisableLightExposureNotification()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.CheckIfNotEnvoy();
      this.ProtocolWrite(DeviceCommands.CargoPersistedAppDataDisableLightExposureNotification);
    }

    public Task<CargoRunStatistics> GetLastRunStatisticsAsync() => Task.Run<CargoRunStatistics>((Func<CargoRunStatistics>) (() => this.GetLastRunStatistics()));

    public CargoRunStatistics GetLastRunStatistics()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Getting last run statistics");
      int serializedByteCount = CargoRunStatistics.GetSerializedByteCount();
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoPersistedStatisticsRunGet, serializedByteCount, CommandStatusHandling.DoNotCheck))
      {
        CargoRunStatistics lastRunStatistics = CargoRunStatistics.DeserializeFromBand((ICargoReader) reader);
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return lastRunStatistics;
      }
    }

    public Task<CargoWorkoutStatistics> GetLastWorkoutStatisticsAsync() => Task.Run<CargoWorkoutStatistics>((Func<CargoWorkoutStatistics>) (() => this.GetLastWorkoutStatistics()));

    public CargoWorkoutStatistics GetLastWorkoutStatistics()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Getting last workout statistics");
      int serializedByteCount = CargoWorkoutStatistics.GetSerializedByteCount();
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoPersistedStatisticsWorkoutGet, serializedByteCount, CommandStatusHandling.DoNotCheck))
      {
        CargoWorkoutStatistics workoutStatistics = CargoWorkoutStatistics.DeserializeFromBand((ICargoReader) reader);
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return workoutStatistics;
      }
    }

    public Task<CargoSleepStatistics> GetLastSleepStatisticsAsync() => Task.Run<CargoSleepStatistics>((Func<CargoSleepStatistics>) (() => this.GetLastSleepStatistics()));

    public CargoSleepStatistics GetLastSleepStatistics()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Getting last workout statistics");
      int serializedByteCount = CargoSleepStatistics.GetSerializedByteCount();
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoPersistedStatisticsSleepGet, serializedByteCount, CommandStatusHandling.DoNotCheck))
      {
        CargoSleepStatistics lastSleepStatistics = CargoSleepStatistics.DeserializeFromBand((ICargoReader) reader);
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return lastSleepStatistics;
      }
    }

    public Task<CargoGuidedWorkoutStatistics> GetLastGuidedWorkoutStatisticsAsync() => Task.Run<CargoGuidedWorkoutStatistics>((Func<CargoGuidedWorkoutStatistics>) (() => this.GetLastGuidedWorkoutStatistics()));

    public CargoGuidedWorkoutStatistics GetLastGuidedWorkoutStatistics()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Getting last guided workout statistics");
      int serializedByteCount = CargoGuidedWorkoutStatistics.GetSerializedByteCount();
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoPersistedStatisticsGuidedWorkoutGet, serializedByteCount, CommandStatusHandling.DoNotCheck))
      {
        CargoGuidedWorkoutStatistics workoutStatistics = CargoGuidedWorkoutStatistics.DeserializeFromBand((ICargoReader) reader);
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return workoutStatistics;
      }
    }

    public Task SensorSubscribeAsync(SensorType subscriptionType) => Task.Run((Action) (() => this.SensorSubscribe(subscriptionType)));

    public void SensorSubscribe(SensorType subscriptionType)
    {
      this.CheckIfDisposed();
      if (subscriptionType == SensorType.LogEntry)
        throw new ArgumentOutOfRangeException(nameof (subscriptionType));
      lock (this.StreamingLock)
      {
        if (this.IsSensorSubscribed(subscriptionType))
          return;
        Logger.Log(LogLevel.Info, "Subscribing to the sensor module: {0}", (object) subscriptionType);
        bool flag = false;
        Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w =>
        {
          w.WriteByte((byte) subscriptionType);
          w.WriteBool32(false);
        });
        this.ProtocolWriteWithArgs(DeviceCommands.CargoRemoteSubscriptionSubscribe, 5, writeArgBuf);
        lock (this.SubscribedSensorTypes)
        {
          flag = this.SubscribedSensorTypes.Count == 0;
          this.SubscribedSensorTypes.Add((byte) subscriptionType);
        }
        if (!flag)
          return;
        this.StartOrAwakeStreamingSubscriptionTasks();
      }
    }

    public Task SensorUnsubscribeAsync(SensorType subscriptionType) => Task.Run((Action) (() => this.SensorUnsubscribe(subscriptionType)));

    public void SensorUnsubscribe(SensorType subscriptionType)
    {
      this.CheckIfDisposed();
      lock (this.StreamingLock)
      {
        if (!this.IsSensorSubscribed(subscriptionType))
          return;
        Logger.Log(LogLevel.Info, "Unsubscribing from the sensor module: {0}", (object) subscriptionType.ToString());
        Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteByte((byte) subscriptionType));
        bool flag = false;
        this.ProtocolWriteWithArgs(DeviceCommands.CargoRemoteSubscriptionUnsubscribe, 1, writeArgBuf);
        lock (this.SubscribedSensorTypes)
        {
          this.SubscribedSensorTypes.Remove((byte) subscriptionType);
          flag = this.SubscribedSensorTypes.Count == 0;
        }
        if (!flag)
          return;
        this.StopStreamingSubscriptionTasks();
      }
    }

    private bool IsSensorSubscribed(SensorType subscriptionType) => this.SubscribedSensorTypes.Contains((byte) subscriptionType);

    protected override void StartOrAwakeStreamingSubscriptionTasks()
    {
      if (this.StreamingTask != null)
        return;
      this.StreamingTaskCancel = new CancellationTokenSource();
      Logger.Log(LogLevel.Info, "Starting the streaming task...");
      this.StreamingTask = Task.Run((Action) (() => this.StreamBandData((ManualResetEvent) null, this.StreamingTaskCancel.Token)));
    }

    protected override void StopStreamingSubscriptionTasks()
    {
      if (this.StreamingTask == null)
        return;
      Logger.Log(LogLevel.Info, "Signaling the streaming task to stop...");
      this.StreamingTaskCancel.Cancel();
      this.StreamingTask.Wait();
      this.StreamingTaskCancel.Dispose();
      this.StreamingTaskCancel = (CancellationTokenSource) null;
      this.StreamingTask = (Task) null;
      Logger.Log(LogLevel.Info, "Streaming task has stopped");
    }

    protected override void OnDisconnected(TransportDisconnectedEventArgs args)
    {
      if (args.Reason != TransportDisconnectedReason.TransportIssue)
        return;
      EventHandler disconnected = this.Disconnected;
      if (disconnected == null)
        return;
      disconnected((object) this, EventArgs.Empty);
    }

    public void CloseSession() => this.Dispose();

    public event EventHandler<BatteryGaugeUpdatedEventArgs> BatteryGaugeUpdated;

    public event EventHandler<LogEntryUpdatedEventArgs> LogEntryUpdated;

    public event EventHandler Disconnected;

    public Task SyncWebTilesAsync(bool forceSync, CancellationToken cancellationToken) => Task.Run((Action) (() => this.SyncWebTiles(forceSync, cancellationToken)));

    private void SyncWebTiles(bool forceSync, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      StartStrip startStripNoImages = this.GetStartStripNoImages();
      IList<Guid> installedWebTileIds = WebTileManagerFactory.Instance.GetInstalledWebTileIds();
      for (int index = 0; index < installedWebTileIds.Count; ++index)
      {
        cancellationToken.ThrowIfCancellationRequested();
        if (!startStripNoImages.Contains(installedWebTileIds[index]))
        {
          WebTileManagerFactory.Instance.UninstallWebTileAsync(installedWebTileIds[index]).Wait();
          installedWebTileIds.RemoveAt(index);
          --index;
        }
      }
      for (int index = 0; index < startStripNoImages.Count; ++index)
      {
        cancellationToken.ThrowIfCancellationRequested();
        AdminBandTile webTile = startStripNoImages[index];
        if (webTile.IsWebTile)
        {
          try
          {
            if (!this.SyncWebTile(startStripNoImages, webTile, installedWebTileIds, forceSync))
              --index;
          }
          catch (Exception ex)
          {
            this.loggerProvider.LogException(ProviderLogLevel.Error, ex, "Error syncing Webtile Name: {0}, Id: {1}.", (object) webTile.Name, (object) webTile.TileId);
          }
        }
      }
    }

    public Task SyncWebTileAsync(Guid tileId, CancellationToken cancellationToken) => Task.Run((Action) (() => this.SyncWebTile(tileId, cancellationToken)));

    private void SyncWebTile(Guid tileId, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      StartStrip startStripNoImages = this.GetStartStripNoImages();
      IList<Guid> installedWebTileIds = WebTileManagerFactory.Instance.GetInstalledWebTileIds();
      cancellationToken.ThrowIfCancellationRequested();
      int index = startStripNoImages.IndexOf(tileId);
      if (index < 0)
        return;
      AdminBandTile webTile = startStripNoImages[index];
      if (!webTile.IsWebTile)
        return;
      this.SyncWebTile(startStripNoImages, webTile, installedWebTileIds, true);
    }

    private bool SyncWebTile(
      StartStrip startStrip,
      AdminBandTile webTile,
      IList<Guid> installedWebTiles,
      bool forceSync)
    {
      IWebTile webTile1 = (IWebTile) null;
      if (installedWebTiles.Contains(webTile.Id))
        webTile1 = WebTileManagerFactory.Instance.GetWebTile(webTile.Id);
      if (webTile1 != null)
      {
        DateTimeOffset utcNow = DateTimeOffset.UtcNow;
        if (forceSync || webTile1.HasRefreshIntervalElapsed(utcNow))
        {
          NotificationDialog notificationDialog = (NotificationDialog) null;
          bool clearPages;
          bool sendAsMessage;
          List<PageData> pages = webTile1.Refresh(out clearPages, out sendAsMessage, out notificationDialog);
          this.SendPagesToBand(webTile1.TileId, pages, clearPages, sendAsMessage);
          webTile1.SaveLastSync(utcNow);
          if (notificationDialog != null)
          {
            Guid tileId = webTile1.TileId;
            BandNotificationFlags flagbits = BandNotificationFlags.ForceNotificationDialog;
            this.ShowDialogHelper(tileId, notificationDialog.Title ?? "", notificationDialog.Body ?? "", CancellationToken.None, flagbits);
          }
        }
        return true;
      }
      startStrip.Remove(webTile);
      this.SetStartStrip(startStrip);
      return false;
    }

    private void SendPagesToBand(
      Guid tileId,
      List<PageData> pages,
      bool clearPages,
      bool sendAsMessage)
    {
      if (clearPages)
        this.RemovePages(tileId, CancellationToken.None);
      if (pages == null)
        return;
      if (sendAsMessage)
      {
        foreach (PageData page in pages)
        {
          string elementTextData1 = this.GetElementTextData(page, 1);
          string elementTextData2 = this.GetElementTextData(page, 2);
          DateTimeOffset utcNow = DateTimeOffset.UtcNow;
          if (!string.IsNullOrWhiteSpace(elementTextData1) && !string.IsNullOrWhiteSpace(elementTextData2))
            this.SendMessage(tileId, elementTextData1, elementTextData2, utcNow, MessageFlags.None, CancellationToken.None);
        }
      }
      else
        this.SetPages(tileId, CancellationToken.None, (IEnumerable<PageData>) pages);
    }

    private string GetElementTextData(PageData pageData, int elementId) => pageData.Values.FirstOrDefault<PageElementData>((Func<PageElementData, bool>) (d => (int) d.ElementId == elementId)) is TextBlockData textBlockData && textBlockData.Text != null ? textBlockData.Text.Trim() : string.Empty;

    public void EnableRetailDemoMode()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Activating retail demo mode.");
      this.ProtocolWrite(DeviceCommands.CargoSystemSettingsEnableDemoMode);
    }

    public Task EnableRetailDemoModeAsync() => Task.Run((Action) (() => this.EnableRetailDemoMode()));

    public void DisableRetailDemoMode()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Shutting down retail demo mode.");
      this.ProtocolWrite(DeviceCommands.CargoSystemSettingsDisableDemoMode);
    }

    public Task DisableRetailDemoModeAsync() => Task.Run((Action) (() => this.DisableRetailDemoMode()));

    public void CargoSystemSettingsFactoryReset()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Factory Resetting Band.");
      int num = 128;
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteByte((byte) 0));
      using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoSystemSettingsFactoryReset, 1, num, writeArgBuf, CommandStatusHandling.ThrowOnlySeverityError))
        cargoCommandReader.ReadExactAndDiscard(num);
    }

    public Task CargoSystemSettingsFactoryResetAsync() => Task.Run((Action) (() => this.CargoSystemSettingsFactoryReset()));

    public Task GenerateSensorLogAsync(TimeSpan duration) => Task.Run((Action) (() => this.GenerateSensorLog(duration)));

    public void GenerateSensorLog(TimeSpan duration)
    {
      this.LoggerEnable();
      this.LoggerSubscribe(SensorType.AccelGyro_2_4_MS_16G);
      this.platformProvider.Sleep((int) duration.TotalMilliseconds);
      this.LoggerUnsubscribe(SensorType.AccelGyro_2_4_MS_16G);
    }

    internal void LoggerSubscribe(SensorType subscriptionType)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Subscribing to subscription logger");
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w =>
      {
        w.WriteByte((byte) subscriptionType);
        w.WriteBool32(false);
      });
      this.ProtocolWriteWithArgs(DeviceCommands.CargoSubscriptionLoggerSubscribe, 5, writeArgBuf);
    }

    internal void LoggerUnsubscribe(SensorType subscriptionType)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Unsubscribing from subscription logger");
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteByte((byte) subscriptionType));
      this.ProtocolWriteWithArgs(DeviceCommands.CargoSubscriptionLoggerUnsubscribe, 1, writeArgBuf);
    }

    public Task LoggerEnableAsync() => Task.Run((Action) (() => this.LoggerEnable()));

    public void LoggerEnable()
    {
      Logger.Log(LogLevel.Info, "Enabling cargo logger");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.ProtocolWrite(DeviceCommands.CargoLoggerEnableLogging);
    }

    public Task LoggerDisableAsync() => Task.Run((Action) (() => this.LoggerDisable()));

    public void LoggerDisable()
    {
      Logger.Log(LogLevel.Info, "Disabling cargo logger");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.ProtocolWrite(DeviceCommands.CargoLoggerDisableLogging);
    }

    internal void ClearCache()
    {
      this.CheckIfDisposed();
      this.storageProvider.DeleteFolder("Ephemeris");
      this.storageProvider.DeleteFolder("TimeZoneData");
      this.storageProvider.DeleteFolder("FirmwareUpdate");
    }

    public void StartCortana()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.ProtocolWrite(DeviceCommands.CargoCortanaStart);
    }

    public void StopCortana()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.ProtocolWrite(DeviceCommands.CargoCortanaStop);
    }

    public void CancelCortana()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.ProtocolWrite(DeviceCommands.CargoCortanaCancel);
    }

    public void SendCortanaNotification(CortanaStatus status, string message)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (!Enum.IsDefined(typeof (CortanaStatus), (object) status))
        throw new ArgumentException("Invalid status parameter", nameof (status));
      if (message == null)
        throw new ArgumentNullException(nameof (message));
      if (message.Length > 160)
        throw new ArgumentOutOfRangeException(nameof (message), "message length exceeded.");
      using (CargoCommandWriter cargoCommandWriter = this.ProtocolBeginWrite(DeviceCommands.CargoCortanaNotification, 326, CommandStatusHandling.DoNotCheck))
      {
        cargoCommandWriter.WriteUInt16((ushort) status);
        cargoCommandWriter.WriteUInt16((ushort) 320);
        cargoCommandWriter.WriteByte((byte) 0);
        cargoCommandWriter.WriteByte((byte) 0);
        cargoCommandWriter.WriteStringWithPadding(message, 160);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
    }

    public Task<byte[]> EFlashReadAsync(uint address, uint numBytesToRead)
    {
      Logger.Log(LogLevel.Verbose, "[CargoClient.EFlashReadAsync()] Invoked");
      return Task.Run<byte[]>((Func<byte[]>) (() => this.EFlashRead(address, numBytesToRead)));
    }

    public byte[] EFlashRead(uint address, uint numBytesToRead)
    {
      Logger.Log(LogLevel.Verbose, "[CargoClient.EFlashRead()] Invoked");
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w =>
      {
        w.WriteUInt32(address);
        w.WriteUInt32(numBytesToRead);
      });
      using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoEFlashRead, 8, (int) numBytesToRead, writeArgBuf, CommandStatusHandling.DoNotCheck))
      {
        byte[] numArray = cargoCommandReader.ReadExact((int) numBytesToRead);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return numArray;
      }
    }

    public Task<byte[]> LogChunkReadAsync(uint address, uint numBytesToRead)
    {
      Logger.Log(LogLevel.Verbose, "[CargoClient.LogChunkReadAsync()] Invoked");
      return Task.Run<byte[]>((Func<byte[]>) (() => this.LogChunkRead(address, numBytesToRead)));
    }

    public byte[] LogChunkRead(uint address, uint numBytesToRead)
    {
      Logger.Log(LogLevel.Verbose, "[CargoClient.LogChunkRead()] Invoked");
      using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoLoggerGetChunkData, (int) numBytesToRead, CommandStatusHandling.DoNotCheck))
      {
        byte[] numArray = cargoCommandReader.ReadExact((int) numBytesToRead);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return numArray;
      }
    }

    public Task CallBadDeviceCommandAsync() => Task.Run((Action) (() => this.CallBadDeviceCommand()));

    public void CallBadDeviceCommand() => this.ProtocolWrite((ushort) 1);

    internal Task OobeCompleteClearAsync() => Task.Run((Action) (() => this.OobeCompleteClear()));

    internal void OobeCompleteClear() => this.ProtocolWrite(DeviceCommands.CargoSystemSettingsOobeCompleteClear);

    internal Task OobeCompleteSetAsync() => Task.Run((Action) (() => this.OobeCompleteSet()));

    internal void OobeCompleteSet() => this.ProtocolWrite(DeviceCommands.CargoSystemSettingsOobeCompleteSet);

    internal CargoClient(
      IDeviceTransport transport,
      CloudProvider cloudProvider,
      ILoggerProvider loggerProvider,
      IPlatformProvider platformProvider,
      IApplicationPlatformProvider applicationPlatformProvider)
      : base(transport, loggerProvider, applicationPlatformProvider)
    {
      this.cloudProvider = cloudProvider;
      this.platformProvider = platformProvider;
      this.loggerLock = new object();
      this.runningFirmwareApp = FirmwareApp.Invalid;
      Logger.Log(LogLevel.Verbose, "[CargoClient.CargoClient()] Object constructed");
    }

    internal void InitializeStorageProvider(IStorageProvider storageProvider)
    {
      this.storageProvider = storageProvider != null ? storageProvider : throw new ArgumentNullException(nameof (storageProvider));
      this.storageProvider.CreateFolder("PendingData");
    }

    public string UserAgent
    {
      get => this.cloudProvider != null ? this.cloudProvider.UserAgent : throw new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
      set
      {
        if (this.cloudProvider == null)
          throw new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
        this.cloudProvider.SetUserAgent(value, true);
      }
    }

    public Task<ushort> GetLogVersionAsync() => Task.Run<ushort>((Func<ushort>) (() => this.GetLogVersion()));

    public ushort GetLogVersion()
    {
      this.CheckIfDisconnectedOrUpdateMode();
      Logger.Log(LogLevel.Info, "Invoking ProtocolReadStruct on KDevice for command: CargoCoreModuleGetLogVersion");
      ushort result = 0;
      Action<ICargoReader> readData = (Action<ICargoReader>) (r => result = r.ReadUInt16());
      this.ProtocolRead(DeviceCommands.CargoCoreModuleGetLogVersion, 2, readData);
      return result;
    }

    private void PopulateUploadMetadata(UploadMetaData metadata)
    {
      metadata.UTCTimeZoneOffsetInMinutes = new int?((int) DateTimeOffset.Now.Offset.TotalMinutes);
      metadata.HostOSVersion = this.platformProvider.GetHostOSVersion().ToString();
      metadata.HostAppVersion = this.platformProvider.GetAssemblyVersion();
      metadata.HostOS = this.platformProvider.GetHostOS();
    }

    private void LoggerFlush(CancellationToken cancel, uint maxBusyRetries = 4)
    {
      uint num = 0;
      while (true)
      {
        CargoStatus status = this.ProtocolWrite(DeviceCommands.CargoLoggerFlush, statusHandling: CommandStatusHandling.DoNotCheck);
        if ((int) status.Status != (int) DeviceStatusCodes.Success)
        {
          if ((int) status.Status == (int) DeviceStatusCodes.DataLoggerBusy)
          {
            bool flag = num < maxBusyRetries;
            Logger.Log(LogLevel.Warning, "LoggerFlush(): Device error DeviceStatusCodes.DataLoggerBusy (0x{0:X}); attempt {1}, {2}", (object) status.Status, (object) (uint) ((int) num + 1), flag ? (object) "retrying..." : (object) "giving up");
            if (flag)
            {
              cancel.WaitAndThrowIfCancellationRequested(DeviceConstants.DefaultLoggerFlushBusyRetryDelay);
              goto label_7;
            }
          }
          BandClient.CheckStatus(status, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
label_7:
          ++num;
        }
        else
          break;
      }
      Logger.Log(LogLevel.Info, "LoggerFlush(): Successful on attempt {0}", (object) (uint) ((int) num + 1));
    }

    private LogMetadataRange GetChunkRangeMetadata(int chunkCount)
    {
      int serializedByteCount = LogMetadataRange.GetSerializedByteCount();
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteInt32(chunkCount));
      LogMetadataRange chunkRangeMetadata;
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoLoggerGetChunkRangeMetadata, 4, serializedByteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
      {
        chunkRangeMetadata = LogMetadataRange.DeserializeFromBand((ICargoReader) reader);
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
      if (chunkRangeMetadata.EndingSeqNumber < chunkRangeMetadata.StartingSeqNumber || (long) chunkRangeMetadata.ByteCount > (long) (chunkCount * 4096))
      {
        Logger.Log(LogLevel.Warning, "The device returned an invalid metadata structure. RequestedChunkRangeSize = {0}. Returned ChunkRangeMetadata = (BytesCount = {1}, Start = {2}, End = {3}).", (object) chunkCount, (object) chunkRangeMetadata.ByteCount, (object) chunkRangeMetadata.StartingSeqNumber, (object) chunkRangeMetadata.EndingSeqNumber);
        throw new InvalidOperationException("Invalid sensor log metadata.");
      }
      return chunkRangeMetadata;
    }

    private void DeleteChunkRange(LogMetadataRange range)
    {
      int serializedByteCount = LogMetadataRange.GetSerializedByteCount();
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoLoggerDeleteChunkRange, serializedByteCount, CommandStatusHandling.DoNotCheck))
      {
        range.SerializeToBand((ICargoWriter) writer);
        BandClient.CheckStatus(writer.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
    }

    private int RemainingDeviceLogDataChunks()
    {
      int bytesToRead = 8;
      uint num = 0;
      using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoLoggerGetChunkCounts, bytesToRead, CommandStatusHandling.DoNotCheck))
      {
        num = cargoCommandReader.ReadUInt32();
        cargoCommandReader.ReadExactAndDiscard(4);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
      return (int) num;
    }

    private double CalculateTransferKbitsPerSecond(long ellapsedMilliseconds, long bytes) => (double) (bytes * 8L) / ((double) ellapsedMilliseconds / 1000.0) / 1000.0;

    private double CalculateTransferKbytesPerSecond(long ellapsedMilliseconds, long bytes) => Math.Round((double) bytes / (double) ellapsedMilliseconds * 1000.0 / 1024.0, 2);

    internal LogSyncResult SyncSensorLog(
      CancellationToken cancellationToken,
      ProgressTrackerPrimitive progress)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (this.cloudProvider == null)
        throw new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
      int num1 = this.platformProvider.MaxChunkRange * 10;
      LogSyncResult logSyncResult = new LogSyncResult();
      logSyncResult.LogFilesProcessing = new List<LogProcessingStatus>();
      int num2 = 0;
      int num3 = 0;
      UploadMetaData uploadMetadata = this.GetUploadMetadata();
      uploadMetadata.DeviceMetadataHint = "band";
      int num4 = num1 + 1;
      Stopwatch stopwatch1 = new Stopwatch();
      Stopwatch uploadWatch = new Stopwatch();
      Stopwatch stopwatch2 = Stopwatch.StartNew();
      int num5 = 0;
      this.LoggerFlush(cancellationToken);
      while (num2 > 0 || num4 > num1)
      {
        cancellationToken.ThrowIfCancellationRequested();
        stopwatch1.Start();
        if (num4 > num1)
        {
          int num6 = this.RemainingDeviceLogDataChunks();
          int num7 = num6 * 4096;
          progress.AddStepsTotal((num7 - num3) * 2);
          if (logSyncResult.DownloadedSensorLogBytes == 0L)
            Logger.Log(LogLevel.Info, "Sensor Log Sync beginning: Total Chunks: {0}, Total Estimated Bytes: {1}", (object) num6, (object) num7);
          else
            Logger.Log(LogLevel.Info, "Sensor Log Sync data re-evaluated: Additional Chunks: {0}, Additional Bytes: {1}, Total Chunks: {2}, Total Estimated Bytes: {3}", (object) (num6 - num2), (object) (num7 - num3), (object) num6, (object) num7);
          num2 = num6;
          num3 = num7;
          num4 = 0;
        }
        rangeMetadata = this.GetChunkRangeMetadata(this.platformProvider.MaxChunkRange);
        int num8 = rangeMetadata.ByteCount > 0U ? (int) rangeMetadata.EndingSeqNumber - (int) rangeMetadata.StartingSeqNumber + 1 : 0;
        cancellationToken.ThrowIfCancellationRequested();
        if (num8 != 0)
        {
          Logger.Log(LogLevel.Info, "Downloading log chunk range: ID's {0} - {1}, Chunks: {2}, Bytes: {3}", (object) rangeMetadata.StartingSeqNumber, (object) rangeMetadata.EndingSeqNumber, (object) num8, (object) rangeMetadata.ByteCount);
          string uploadId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
          uploadMetadata.StartSequenceId = new int?((int) rangeMetadata.StartingSeqNumber);
          uploadMetadata.EndSequenceId = new int?((int) rangeMetadata.EndingSeqNumber);
          using (MemoryPipeStream transferPipe = new MemoryPipeStream((int) rangeMetadata.ByteCount, progress, this.loggerProvider))
          {
            Task<FileUploadStatus> task = Task.Run<FileUploadStatus>((Func<FileUploadStatus>) (() =>
            {
              uploadWatch.Start();
              int cloud = (int) this.cloudProvider.UploadFileToCloud((Stream) transferPipe, LogFileTypes.Sensor, uploadId, uploadMetadata, cancellationToken);
              uploadWatch.Stop();
              return (FileUploadStatus) cloud;
            }));
            try
            {
              LogMetadataRange rangeMetadata;
              Action<ICargoWriter> writeArgBuf = closure_0 ?? (closure_0 = (Action<ICargoWriter>) (w => rangeMetadata.SerializeToBand(w)));
              using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoLoggerGetChunkRangeData, LogMetadataRange.GetSerializedByteCount(), (int) rangeMetadata.ByteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
              {
                bool flag = false;
                while (cargoCommandReader.BytesRemaining > 1)
                {
                  int count = Math.Min(cargoCommandReader.BytesRemaining - 1, Math.Min(8192, cargoCommandReader.BytesRemaining));
                  try
                  {
                    cargoCommandReader.CopyTo((Stream) transferPipe, count);
                  }
                  catch (Exception ex)
                  {
                    try
                    {
                      cargoCommandReader.ReadToEndAndDiscard();
                    }
                    catch
                    {
                    }
                    if (ex is ObjectDisposedException)
                    {
                      flag = true;
                      break;
                    }
                    throw;
                  }
                  logSyncResult.DownloadedSensorLogBytes += (long) count;
                }
                if (!flag)
                {
                  byte num9 = cargoCommandReader.ReadByte();
                  BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowAnyNonZero, this.loggerProvider);
                  transferPipe.WriteByte(num9);
                  ++logSyncResult.DownloadedSensorLogBytes;
                }
              }
            }
            catch (Exception ex1)
            {
              try
              {
                transferPipe.SetEndOfStream();
              }
              catch (ObjectDisposedException ex2)
              {
              }
              if (!(ex1 is BandIOException))
                throw new BandIOException(ex1.Message, ex1);
              throw;
            }
            stopwatch1.Stop();
            try
            {
              task.Wait();
            }
            catch (AggregateException ex)
            {
              if (ex.InnerExceptions.Count == 1)
                throw ex.InnerException;
              throw;
            }
            if (task.Result != FileUploadStatus.UploadDone)
              logSyncResult.LogFilesProcessing.Add(new LogProcessingStatus(uploadId, DateTime.UtcNow));
          }
          stopwatch1.Start();
          this.DeleteChunkRange(rangeMetadata);
          stopwatch1.Stop();
          logSyncResult.UploadedSensorLogBytes += (long) rangeMetadata.ByteCount;
          num2 -= num8;
          num3 -= (int) rangeMetadata.ByteCount;
          num4 += num8;
          ++num5;
        }
        else
          break;
      }
      stopwatch2.Stop();
      progress.Complete();
      logSyncResult.DownloadKbitsPerSecond = this.CalculateTransferKbitsPerSecond(stopwatch1.ElapsedMilliseconds, logSyncResult.DownloadedSensorLogBytes);
      logSyncResult.DownloadKbytesPerSecond = this.CalculateTransferKbytesPerSecond(stopwatch1.ElapsedMilliseconds, logSyncResult.DownloadedSensorLogBytes);
      logSyncResult.UploadKbitsPerSecond = this.CalculateTransferKbitsPerSecond(uploadWatch.ElapsedMilliseconds, logSyncResult.UploadedSensorLogBytes);
      logSyncResult.UploadKbytesPerSecond = this.CalculateTransferKbytesPerSecond(uploadWatch.ElapsedMilliseconds, logSyncResult.UploadedSensorLogBytes);
      logSyncResult.DownloadTime = stopwatch1.ElapsedMilliseconds;
      logSyncResult.UploadTime = uploadWatch.ElapsedMilliseconds;
      logSyncResult.RanToCompletion = true;
      Logger.Log(LogLevel.Info, "Log download: {0} bytes, {1}, {2} KB/s", (object) logSyncResult.DownloadedSensorLogBytes, (object) stopwatch1.Elapsed, (object) logSyncResult.DownloadKbytesPerSecond);
      Logger.Log(LogLevel.Info, "Log upload: {0} bytes, {1}, {2} KB/s", (object) logSyncResult.UploadedSensorLogBytes, (object) uploadWatch.Elapsed, (object) logSyncResult.UploadKbytesPerSecond);
      Logger.Log(LogLevel.Info, "Log sync: {0} bytes, {1}, {2} KB/s", (object) logSyncResult.UploadedSensorLogBytes, (object) stopwatch2.Elapsed, (object) this.CalculateTransferKbytesPerSecond(stopwatch2.ElapsedMilliseconds, logSyncResult.UploadedSensorLogBytes));
      Logger.Log(LogLevel.Info, "Log sync: Chunk Ranges: {0}, Still Processing: {1}", (object) num5, (object) logSyncResult.LogFilesProcessing.Count);
      return logSyncResult;
    }

    public void DownloadSensorLog(Stream stream, int chunkRangeSize)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (chunkRangeSize > 256)
        throw new ArgumentOutOfRangeException(nameof (chunkRangeSize));
      this.LoggerFlush(CancellationToken.None);
      int num1 = this.RemainingDeviceLogDataChunks();
      Logger.Log(LogLevel.Verbose, "Starting to download the sensor log. TotalChunksToDownload = {0}", (object) num1);
      int chunkCount;
      for (int val2 = num1; val2 > 0; val2 -= chunkCount)
      {
        chunkCount = Math.Min(chunkRangeSize, val2);
        LogMetadataRange rangeMetadata = this.GetChunkRangeMetadata(chunkCount);
        if (rangeMetadata.ByteCount == 0U)
        {
          Logger.Log(LogLevel.Warning, "Someone is downloading/deleting the sensor log concurrently. We were expecting RemainingChunksToDownload = {0}, but the ChunkRangeMetadata came as (BytesCount = {1}, Start = {2}, End = {3}).", (object) val2, (object) rangeMetadata.ByteCount, (object) rangeMetadata.StartingSeqNumber, (object) rangeMetadata.EndingSeqNumber);
          break;
        }
        try
        {
          Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => rangeMetadata.SerializeToBand(w));
          using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoLoggerGetChunkRangeData, LogMetadataRange.GetSerializedByteCount(), (int) rangeMetadata.ByteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
          {
            while (cargoCommandReader.BytesRemaining > 1)
            {
              int count = Math.Min(cargoCommandReader.BytesRemaining - 1, Math.Min(8192, cargoCommandReader.BytesRemaining));
              cargoCommandReader.CopyTo(stream, count);
            }
            byte num2 = cargoCommandReader.ReadByte();
            BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowAnyNonZero, this.loggerProvider);
            stream.WriteByte(num2);
          }
        }
        catch (BandIOException ex)
        {
          throw;
        }
        catch (Exception ex)
        {
          throw new BandIOException(ex.Message, ex);
        }
        this.DeleteChunkRange(rangeMetadata);
      }
    }

    private UploadMetaData GetUploadMetadata()
    {
      UploadMetaData metadata = new UploadMetaData()
      {
        DeviceId = this.DeviceUniqueId.ToString(),
        DeviceSerialNumber = this.SerialNumber,
        DeviceVersion = this.FirmwareVersions.ApplicationVersion.ToString(4),
        LogVersion = new int?((int) this.GetLogVersion()),
        PcbId = this.FirmwareVersions.PcbId.ToString()
      };
      this.PopulateUploadMetadata(metadata);
      return metadata;
    }

    private void UpdateDeviceEphemerisData(Stream ephemerisData, int length)
    {
      try
      {
        using (CargoCommandWriter cargoCommandWriter = this.ProtocolBeginWrite(DeviceCommands.CargoSystemSettingsSetEphemerisFile, length, CommandStatusHandling.DoNotCheck))
        {
          ICargoStream cargoStream = this.DeviceTransport.CargoStream;
          cargoStream.WriteTimeout = 30000;
          cargoStream.ReadTimeout = 30000;
          cargoCommandWriter.CopyFromStream(ephemerisData);
          BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        }
        Logger.Log(LogLevel.Info, "Sent ephemeris data to the device");
      }
      catch (BandIOException ex)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new BandIOException(ex.Message, ex);
      }
    }

    private void UpdateDeviceTimeZonesData(Stream timeZonesData, int length)
    {
      try
      {
        using (CargoCommandWriter cargoCommandWriter = this.ProtocolBeginWrite(DeviceCommands.CargoTimeSetTimeZoneFile, length, CommandStatusHandling.DoNotCheck))
        {
          ICargoStream cargoStream = this.DeviceTransport.CargoStream;
          cargoStream.WriteTimeout = 30000;
          cargoStream.ReadTimeout = 30000;
          cargoCommandWriter.CopyFromStream(timeZonesData);
          BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        }
        Logger.Log(LogLevel.Info, "Sent time zone data to the device");
      }
      catch (BandIOException ex)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new BandIOException(ex.Message, ex);
      }
    }

    private void UploadFitnessPlan(Stream fitnessPlan, int length)
    {
      using (CargoCommandWriter cargoCommandWriter = this.ProtocolBeginWrite(DeviceCommands.CargoFitnessPlansWriteFile, length, CommandStatusHandling.DoNotCheck))
      {
        ICargoStream cargoStream = this.DeviceTransport.CargoStream;
        cargoStream.WriteTimeout = 30000;
        cargoStream.ReadTimeout = 30000;
        cargoCommandWriter.CopyFromStream(fitnessPlan);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
      Logger.Log(LogLevel.Info, "Sent fitness plan data to the device");
    }

    private void UploadGolfCourse(Stream golfCourse, int length)
    {
      using (CargoCommandWriter cargoCommandWriter = this.ProtocolBeginWrite(DeviceCommands.CargoGolfCourseFileWrite, length, CommandStatusHandling.DoNotCheck))
      {
        ICargoStream cargoStream = this.DeviceTransport.CargoStream;
        cargoStream.WriteTimeout = 30000;
        cargoStream.ReadTimeout = 30000;
        cargoCommandWriter.CopyFromStream(golfCourse);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
      Logger.Log(LogLevel.Info, "Sent golf course data to the device");
    }

    private uint DeviceFileGetSize(ushort deviceCommand)
    {
      uint fileSize = 0;
      Action<ICargoReader> readData = (Action<ICargoReader>) (r => fileSize = r.ReadUInt32());
      return (int) this.ProtocolRead(deviceCommand, 4, readData, statusHandling: CommandStatusHandling.DoNotThrow).Status != (int) DeviceStatusCodes.Success ? 0U : fileSize;
    }

    public Task<string> GetProductSerialNumberAsync() => Task.Run<string>((Func<string>) (() => this.GetProductSerialNumber()));

    public string GetProductSerialNumber()
    {
      int num1 = 12;
      using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoGetProductSerialNumber, num1, CommandStatusHandling.ThrowOnlySeverityError))
      {
        StringBuilder stringBuilder = new StringBuilder(num1);
        while (cargoCommandReader.BytesRemaining > 0)
        {
          byte num2 = cargoCommandReader.ReadByte();
          switch (num2)
          {
            case 48:
            case 49:
            case 50:
            case 51:
            case 52:
            case 53:
            case 54:
            case 55:
            case 56:
            case 57:
              stringBuilder.Append((char) num2);
              continue;
            default:
              stringBuilder.Append('0');
              continue;
          }
        }
        return stringBuilder.ToString();
      }
    }

    private Guid GetDeviceUniqueId()
    {
      this.CheckIfDisposed();
      Logger.Log(LogLevel.Verbose, "Retrieving the device unique ID");
      using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoCoreModuleGetUniqueID, 66, CommandStatusHandling.DoNotThrow))
      {
        cargoCommandReader.ReadExactAndDiscard(2);
        string input = cargoCommandReader.ReadString(32);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        try
        {
          return Guid.Parse(input);
        }
        catch (Exception ex)
        {
          BandException e = new BandException(CommonSR.InvalidGuidFromDevice, ex);
          Logger.LogException(LogLevel.Error, (Exception) e);
          throw e;
        }
      }
    }

    public Task<bool> GpsIsEnabledAsync() => Task.Run<bool>((Func<bool>) (() => this.GpsIsEnabled()));

    public bool GpsIsEnabled()
    {
      bool enabled = false;
      Action<ICargoReader> readData = (Action<ICargoReader>) (r => enabled = r.ReadBool32());
      this.ProtocolRead(DeviceCommands.CargoGpsIsEnabled, 4, readData);
      return enabled;
    }

    private UserProfileHeader ProfileAppHeaderGet()
    {
      int byteCount = UserProfileHeader.GetSerializedByteCount();
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteInt32(byteCount));
      Logger.Log(LogLevel.Info, "Obtaining the header portion of the application profile from the KDevice");
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoProfileGetDataApp, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
      {
        UserProfileHeader userProfileHeader = UserProfileHeader.DeserializeFromBand((ICargoReader) reader);
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return userProfileHeader;
      }
    }

    private UserProfileHeader ProfileFirmwareHeaderGet()
    {
      int byteCount = UserProfileHeader.GetSerializedByteCount();
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteInt32(byteCount));
      Logger.Log(LogLevel.Info, "Obtaining the header portion of the application profile from the KDevice");
      using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoProfileGetDataFW, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
      {
        UserProfileHeader userProfileHeader = UserProfileHeader.DeserializeFromBand((ICargoReader) reader);
        BandClient.CheckStatus(reader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return userProfileHeader;
      }
    }

    private byte[] ProfileGetFirmwareBytes()
    {
      int serializedByteCount = UserProfileHeader.GetSerializedByteCount();
      int byteCount = serializedByteCount + 256;
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteInt32(byteCount));
      Logger.Log(LogLevel.Info, "Obtaining the profile firmware bytes from the Band");
      using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoProfileGetDataFW, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
      {
        cargoCommandReader.ReadExactAndDiscard(serializedByteCount);
        byte[] firmwareBytes = cargoCommandReader.ReadExact(256);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        return firmwareBytes;
      }
    }

    private void ProfileSetFirmwareBytes(UserProfile profile)
    {
      Logger.Log(LogLevel.Info, "Saving the firmware profile on the KDevice");
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoProfileSetDataFW, UserProfile.GetFirmwareBytesSerializedByteCount(), CommandStatusHandling.DoNotCheck))
      {
        profile.SerializeFirmwareBytesToBand((ICargoWriter) writer, this.ConnectedAdminBandConstants);
        BandClient.CheckStatus(writer.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
    }

    private void ProfileSetAppData(UserProfile profile)
    {
      Logger.Log(LogLevel.Info, "Saving the application profile on the band");
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoProfileSetDataApp, UserProfile.GetAppDataSerializedByteCount(this.ConnectedAdminBandConstants), CommandStatusHandling.DoNotCheck))
      {
        profile.SerializeAppDataToBand((ICargoWriter) writer, this.ConnectedAdminBandConstants);
        BandClient.CheckStatus(writer.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
    }

    private void CheckIfUpdateValidForDevice(string firmwareVersion)
    {
      uint num = uint.Parse(firmwareVersion.Split('.')[2]);
      int build = this.FirmwareVersions.ApplicationVersion.Build;
      if (build < 5100 && num >= 5100U)
      {
        BandException e = new BandException(string.Format(CommonSR.ObsoleteFirmwareVersionOnDevice, new object[2]
        {
          (object) build,
          (object) num
        }));
        Logger.LogException(LogLevel.Warning, (Exception) e);
        throw e;
      }
    }

    private void DownloadFirmwareUpdateInternal(
      FirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      FirmwareUpdateOverallProgress progressTracker)
    {
      string firmwareUpdateVersionFileRelativePath = Path.Combine(new string[2]
      {
        "FirmwareUpdate",
        "FirmwareUpdate.json"
      });
      string relativePath = Path.Combine(new string[2]
      {
        "FirmwareUpdate",
        "FirmwareUpdate.bin"
      });
      string str = Path.Combine(new string[2]
      {
        "FirmwareUpdate",
        "FirmwareUpdateTemp.bin"
      });
      FirmwareUpdateInfo versionFromLocalFile = this.GetFirmwareUpdateVersionFromLocalFile(firmwareUpdateVersionFileRelativePath);
      if (versionFromLocalFile != null && versionFromLocalFile.FirmwareVersion.Equals(updateInfo.FirmwareVersion) && this.storageProvider.FileExists(relativePath))
      {
        progressTracker.DownloadFirmwareProgress.Complete();
      }
      else
      {
        if (this.cloudProvider == null)
        {
          InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
          Logger.LogException(LogLevel.Error, (Exception) e);
          throw e;
        }
        cancellationToken.ThrowIfCancellationRequested();
        progressTracker.SetState(FirmwareUpdateState.DownloadingUpdate);
        if (this.storageProvider.FileExists(str))
          this.storageProvider.DeleteFile(str);
        this.storageProvider.CreateFolder("FirmwareUpdate");
        Stream stream;
        try
        {
          stream = this.storageProvider.OpenFileForWrite(str, false);
        }
        catch (Exception ex)
        {
          BandException e = new BandException(string.Format(CommonSR.FirmwareUpdateDownloadTempFileOpenError, new object[1]
          {
            (object) str
          }), ex);
          Logger.LogException(LogLevel.Error, (Exception) e);
          throw e;
        }
        Exception e1 = (Exception) null;
        using (stream)
        {
          try
          {
            this.cloudProvider.GetFirmwareUpdate(updateInfo, stream, cancellationToken);
          }
          catch (OperationCanceledException ex)
          {
            e1 = (Exception) ex;
          }
          catch (Exception ex)
          {
            e1 = (Exception) new BandCloudException(CommonSR.FirmwareUpdateDownloadError, ex);
          }
          if (e1 == null)
          {
            long num = long.Parse(updateInfo.SizeInBytes);
            if (stream.Length != num)
              e1 = (Exception) new BandException(string.Format(CommonSR.FirmwareUpdateDownloadTempFileSizeMismatchError, new object[2]
              {
                (object) stream.Length,
                (object) num
              }));
          }
          if (e1 == null)
          {
            stream.Seek(0L, SeekOrigin.Begin);
            if (!this.AreEqual(this.platformProvider.ComputeHashMd5(stream), Convert.FromBase64String(updateInfo.HashMd5)))
              e1 = (Exception) new BandException(CommonSR.FirmwareUpdateIntegrityError);
          }
        }
        if (e1 != null)
        {
          try
          {
            this.storageProvider.DeleteFile(str);
          }
          catch (Exception ex)
          {
          }
          Logger.LogException(LogLevel.Error, e1);
          throw e1;
        }
        this.storageProvider.RenameFile(str, "FirmwareUpdate", "FirmwareUpdate.bin");
        this.SaveFirmwareUpdateVersionFileLocally(firmwareUpdateVersionFileRelativePath, updateInfo);
        Logger.Log(LogLevel.Info, "Firmware update file downloaded successfully");
        progressTracker.DownloadFirmwareProgress.Complete();
      }
    }

    private bool AreEqual(byte[] left, byte[] right)
    {
      if (right.Length != left.Length)
        return false;
      for (int index = 0; index < left.Length; ++index)
      {
        if ((int) right[index] != (int) left[index])
          return false;
      }
      return true;
    }

    private void SaveFirmwareUpdateVersionFileLocally(
      string firmwareUpdateVersionFileRelativePath,
      FirmwareUpdateInfo updateInfo)
    {
      using (Stream outputStream = this.storageProvider.OpenFileForWrite(firmwareUpdateVersionFileRelativePath, false, 4096))
        CargoClient.SerializeJson(outputStream, (object) updateInfo);
    }

    private FirmwareUpdateInfo GetFirmwareUpdateVersionFromLocalFile(
      string firmwareUpdateVersionFileRelativePath)
    {
      FirmwareUpdateInfo versionFromLocalFile = (FirmwareUpdateInfo) null;
      if (this.storageProvider.FileExists(firmwareUpdateVersionFileRelativePath))
      {
        try
        {
          using (Stream inputStream = this.storageProvider.OpenFileForRead(firmwareUpdateVersionFileRelativePath, -1))
            versionFromLocalFile = CargoClient.DeserializeJson<FirmwareUpdateInfo>(inputStream);
        }
        catch (Exception ex)
        {
        }
      }
      return versionFromLocalFile;
    }

    private bool PushFirmwareUpdateToDeviceInternal(
      FirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      FirmwareUpdateOverallProgress progressTracker)
    {
      bool deviceInternal = false;
      Logger.Log(LogLevel.Info, "Verified that the firmware update is valid for the device");
      cancellationToken.ThrowIfCancellationRequested();
      string str1 = Path.Combine(new string[2]
      {
        "FirmwareUpdate",
        "FirmwareUpdate.bin"
      });
      if (this.storageProvider.FileExists(str1))
      {
        int.Parse(updateInfo.SizeInBytes);
        this.UploadDeviceFirmware(str1, progressTracker);
      }
      string str2 = this.FirmwareVersions.ApplicationVersion.ToString();
      if (updateInfo.FirmwareVersion.Equals(str2) && this.GetFirmwareBinariesValidationStatusInternal())
      {
        deviceInternal = true;
        Logger.Log(LogLevel.Info, "Verified that the firmware update is successfully installed on the device");
      }
      return deviceInternal;
    }

    private void BootIntoUpdateMode()
    {
      lock (this.protocolLock)
      {
        this.ProtocolWrite(DeviceCommands.CargoSRAMFWUpdateBootIntoUpdateMode, swallowStatusReadException: true);
        this.DeviceTransport.Disconnect();
      }
    }

    private void WriteFirmwareUpdate(
      Stream updateFileStream,
      int updateFileSize,
      ProgressTrackerPrimitive progressTracker)
    {
      try
      {
        this.WriteFirmwareUpdateHelper(updateFileStream, updateFileSize, progressTracker);
      }
      catch (BandIOException ex)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new BandIOException(ex.Message, ex);
      }
    }

    private void WriteFirmwareUpdateHelper(
      Stream updateFileStream,
      int updateFileSize,
      ProgressTrackerPrimitive progressTracker)
    {
      using (CargoCommandWriter cargoCommandWriter = this.ProtocolBeginWrite(DeviceCommands.CargoSRAMFWUpdateLoadData, updateFileSize, CommandStatusHandling.DoNotCheck))
      {
        Stopwatch stopwatch = Stopwatch.StartNew();
        ICargoStream cargoStream = this.DeviceTransport.CargoStream;
        cargoStream.WriteTimeout = 30000;
        cargoStream.ReadTimeout = 30000;
        while (cargoCommandWriter.BytesRemaining > 0)
        {
          int count = Math.Min(cargoCommandWriter.BytesRemaining, 8192);
          int steps = cargoCommandWriter.CopyFromStream(updateFileStream, count);
          if (steps == 0)
            throw new EndOfStreamException();
          cargoCommandWriter.Flush();
          progressTracker.AddStepsCompleted(steps);
        }
        Logger.Log(LogLevel.Info, "Firmware upload complete: {0} bytes, {1}Kbytes/second", (object) cargoCommandWriter.Length, (object) Math.Round((double) cargoCommandWriter.Length / (double) stopwatch.ElapsedMilliseconds * 1000.0 / 1024.0, 2));
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowAnyNonZero, this.loggerProvider);
      }
    }

    private void UploadDeviceFirmware(
      string updateFilePath,
      FirmwareUpdateOverallProgress progressTracker)
    {
      this.CheckIfDisposed();
      if (this.DeviceTransport == null)
      {
        InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      Logger.Log(LogLevel.Info, "Updating the device firmware with the firmware update binary...");
      bool post2UpMode = this.DeviceTransportApp == RunningAppType.TwoUp;
      using (Stream updateFileStream = this.storageProvider.OpenFileForRead(updateFilePath))
      {
        progressTracker.Send2UpUpdateToDeviceProgress.AddStepsTotal((int) updateFileStream.Length);
        ProgressTrackerPrimitive upUpdateProgress = progressTracker.WaitToConnectAfter2UpUpdateProgress;
        TimeSpan expectedWaitTime = DeviceConstants.Firmware2UpUpdateConnectExpectedWaitTime;
        int totalMilliseconds1 = (int) expectedWaitTime.TotalMilliseconds;
        upUpdateProgress.AddStepsTotal(totalMilliseconds1);
        progressTracker.SendUpdateToDeviceProgress.AddStepsTotal((int) updateFileStream.Length);
        ProgressTrackerPrimitive afterUpdateProgress = progressTracker.WaitToConnectAfterUpdateProgress;
        expectedWaitTime = DeviceConstants.FirmwareUpAppUpdateConnectExpectedWaitTime;
        int totalMilliseconds2 = (int) expectedWaitTime.TotalMilliseconds;
        afterUpdateProgress.AddStepsTotal(totalMilliseconds2);
        if (post2UpMode)
        {
          progressTracker.SetTo2UpUpdate();
          this.UploadDeviceFirmware2UpMode(updateFileStream, (int) updateFileStream.Length, progressTracker);
          Logger.Log(LogLevel.Info, "2up mode update complete. Attempting subsequent UpApp mode update...");
          updateFileStream.Seek(0L, SeekOrigin.Begin);
        }
        this.UploadDeviceFirmwareAppMode(updateFileStream, (int) updateFileStream.Length, post2UpMode, progressTracker);
      }
    }

    private void UploadDeviceFirmware2UpMode(
      Stream updateFileStream,
      int updateFileSize,
      FirmwareUpdateOverallProgress progressTracker)
    {
      Logger.Log(LogLevel.Warning, "Device is in 2up mode. Attempting rescue mode update...");
      Logger.Log(LogLevel.Info, "Writing firmware update to the device...");
      progressTracker.SetState(FirmwareUpdateState.SendingUpdateToDevice);
      lock (this.protocolLock)
      {
        this.WriteFirmwareUpdate(updateFileStream, updateFileSize, progressTracker.Send2UpUpdateToDeviceProgress);
        this.DeviceTransport.Disconnect();
      }
      progressTracker.SetState(FirmwareUpdateState.WaitingtoConnectAfterUpdate);
      this.WaitForDeviceRebootAfterFirmwareWrite(DeviceConstants.Firmware2UpUpdateConnectExpectedWaitTime, false, progressTracker.WaitToConnectAfter2UpUpdateProgress);
    }

    private void UploadDeviceFirmwareAppMode(
      Stream updateFileStream,
      int updateFileSize,
      bool post2UpMode,
      FirmwareUpdateOverallProgress progressTracker)
    {
      progressTracker.SetState(FirmwareUpdateState.SyncingLog);
      if (post2UpMode)
      {
        Logger.Log(LogLevel.Info, "Post recovery update.  Not downloading sensor logs.");
      }
      else
      {
        bool flag = true;
        try
        {
          flag = this.GetDeviceOobeCompleted();
        }
        catch
        {
        }
        if (flag)
        {
          Logger.Log(LogLevel.Info, "Downloading sensor logs prior to firmware update...");
          this.SyncSensorLog(CancellationToken.None, progressTracker.LogSyncProgress);
        }
        else
          Logger.Log(LogLevel.Info, "Device in OOBE mode.  Not downloading sensor logs.");
      }
      Logger.Log(LogLevel.Info, "Booting the device into update mode...");
      progressTracker.LogSyncProgress.Complete();
      progressTracker.SetState(FirmwareUpdateState.BootingToUpdateMode);
      this.BootIntoUpdateMode();
      byte num = 0;
      this.platformProvider.Sleep(5000);
      while (!this.DeviceTransport.IsConnected)
      {
        if (num > (byte) 0)
          this.platformProvider.Sleep(500);
        try
        {
          this.DeviceTransport.Connect();
          if (!this.IsSameDeviceAfterReboot(true, false))
          {
            this.DeviceTransport.Disconnect();
            BandException e = new BandException("Wrong device");
            Logger.LogException(LogLevel.Error, (Exception) e);
            throw e;
          }
        }
        catch
        {
          ++num;
          if (num > (byte) 40)
          {
            BandIOException e = new BandIOException(CommonSR.DeviceReconnectMaxAttemptsExceeded);
            Logger.LogException(LogLevel.Error, (Exception) e, "Exception occurred prior to firmware upload, but after BootIntoUpdateMode command");
            throw e;
          }
        }
      }
      this.InitializeCachedProperties();
      if (this.DeviceTransportApp != RunningAppType.UpApp)
      {
        BandException e = new BandException(CommonSR.DeviceNotInUpdateMode);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      progressTracker.BootToUpdateModeProgress.Complete();
      progressTracker.SetState(FirmwareUpdateState.SendingUpdateToDevice);
      Logger.Log(LogLevel.Info, "Writing firmware update to the device...");
      lock (this.protocolLock)
      {
        this.WriteFirmwareUpdate(updateFileStream, updateFileSize, progressTracker.SendUpdateToDeviceProgress);
        this.DeviceTransport.Disconnect();
      }
      progressTracker.SetState(FirmwareUpdateState.WaitingtoConnectAfterUpdate);
      this.WaitForDeviceRebootAfterFirmwareWrite(DeviceConstants.FirmwareUpAppUpdateConnectExpectedWaitTime, true, progressTracker.WaitToConnectAfterUpdateProgress);
    }

    private bool IsSameDeviceAfterReboot(bool verifyVersions, bool verifyId)
    {
      FirmwareVersions firmwareVersions = this.GetFirmwareVersionsFromBand().ToFirmwareVersions();
      if ((int) firmwareVersions.PcbId != (int) this.FirmwareVersions.PcbId || firmwareVersions.BootloaderVersion != this.FirmwareVersions.BootloaderVersion || verifyVersions && (firmwareVersions.UpdaterVersion != this.FirmwareVersions.UpdaterVersion || firmwareVersions.ApplicationVersion != this.FirmwareVersions.ApplicationVersion))
        return false;
      if (!verifyId)
        return true;
      Guid uniqueId;
      this.GetDeviceSerialAndUniqueId(out string _, out uniqueId);
      return uniqueId == this.DeviceUniqueId;
    }

    private void WaitForDeviceRebootAfterFirmwareWrite(
      TimeSpan expectedWait,
      bool verifyId,
      ProgressTrackerPrimitive progressTracker)
    {
      Stopwatch stopwatch1 = Stopwatch.StartNew();
      Stopwatch stopwatch2 = new Stopwatch();
      DateTime dateTime = DateTime.MinValue;
      while (stopwatch1.Elapsed < DeviceConstants.FirmwareUpdateConnectMaxWaitTime)
      {
        stopwatch2.Restart();
        if (stopwatch1.Elapsed >= DeviceConstants.FirmwareUpdateInitialConnectWait && DateTime.UtcNow - dateTime >= DeviceConstants.FirmwareUpdateConnectRetryInterval)
        {
          if (this.AttemptReconnect(verifyId))
          {
            progressTracker.Complete();
            return;
          }
          dateTime = DateTime.UtcNow;
          progressTracker.AddStepsCompleted((int) stopwatch2.ElapsedMilliseconds);
          stopwatch2.Restart();
        }
        this.platformProvider.Sleep(1000);
        progressTracker.AddStepsCompleted((int) stopwatch2.ElapsedMilliseconds);
      }
      BandIOException e = new BandIOException(CommonSR.DeviceReconnectMaxAttemptsExceeded);
      Logger.LogException(LogLevel.Error, (Exception) e, "Exception occurred after firmware upload, when trying to check if device exited update mode");
      throw e;
    }

    private bool AttemptReconnect(bool verifyId)
    {
      this.CheckIfDisposed();
      try
      {
        this.DeviceTransport.Connect();
      }
      catch (Exception ex)
      {
        Logger.LogException(LogLevel.Warning, ex, "Post FW-Update connection attempt failed.");
        return false;
      }
      try
      {
        if (!this.IsSameDeviceAfterReboot(false, verifyId))
        {
          Logger.Log(LogLevel.Warning, "Post FW-Update connected to wrong device.");
          lock (this.protocolLock)
            this.DeviceTransport.Disconnect();
          return false;
        }
        this.InitializeCachedProperties();
        if (this.runningFirmwareApp == FirmwareApp.TwoUp || this.runningFirmwareApp == FirmwareApp.UpApp)
        {
          Logger.Log(LogLevel.Warning, "Post FW-Update connection attempt still in Update Mode.");
          lock (this.protocolLock)
            this.DeviceTransport.Disconnect();
          return false;
        }
        Logger.Log(LogLevel.Info, "Device out of update mode after firmware upload");
        if (this.cloudProvider != null)
          this.cloudProvider.SetUserAgent(this.platformProvider.GetDefaultUserAgent(this.FirmwareVersions), false);
      }
      catch (Exception ex)
      {
        Logger.LogException(LogLevel.Warning, ex, "Post FW-Update exception after successful connection.");
        lock (this.protocolLock)
          this.DeviceTransport.Disconnect();
        return false;
      }
      return true;
    }

    protected override void StreamBandData(ManualResetEvent started, CancellationToken stop)
    {
      Logger.Log(LogLevel.Info, "Polling streaming task starting...");
      started?.Set();
      while (!stop.IsCancellationRequested)
      {
        bool flag = false;
        try
        {
          flag = this.PollDataSubscription();
        }
        catch
        {
        }
        if (!flag)
          stop.WaitHandle.WaitOne(5000);
      }
      Logger.Log(LogLevel.Info, "Polling streaming task exiting...");
    }

    private bool PollDataSubscription()
    {
      int bytesToRead;
      lock (this.protocolLock)
      {
        using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoRemoteSubscriptionGetDataLength, 4, CommandStatusHandling.ThrowOnlySeverityError))
          bytesToRead = cargoCommandReader.ReadInt32();
        if (bytesToRead > 0)
        {
          using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoRemoteSubscriptionGetData, bytesToRead, CommandStatusHandling.ThrowOnlySeverityError))
            this.ParseRemoteSubscriptionSample(reader);
        }
      }
      return bytesToRead > 0;
    }

    private void ParseRemoteSubscriptionSample(CargoCommandReader reader)
    {
      int num = 1;
      while (reader.BytesRemaining > 0)
      {
        RemoteSubscriptionSampleHeader sampleHeader;
        try
        {
          sampleHeader = RemoteSubscriptionSampleHeader.DeserializeFromBand((ICargoReader) reader);
        }
        catch (Exception ex)
        {
          Exception e = (Exception) new BandIOException(string.Format("An exception occurred reading subscription sample header #{0}", new object[1]
          {
            (object) num
          }), ex);
          Logger.LogException(LogLevel.Warning, e);
          throw e;
        }
        try
        {
          this.ParseSensorPayload(reader, sampleHeader);
        }
        catch (Exception ex)
        {
          Exception e = (Exception) new BandIOException(string.Format("An exception occurred parsing subscription data payload #{0}; Type: {1}, Size: {2}", new object[3]
          {
            (object) num,
            (object) sampleHeader.SubscriptionType,
            (object) sampleHeader.SampleSize
          }), ex);
          Logger.LogException(LogLevel.Warning, e);
          throw e;
        }
        ++num;
      }
    }

    private void ParseSensorPayload(
      CargoCommandReader reader,
      RemoteSubscriptionSampleHeader sampleHeader)
    {
      switch (sampleHeader.SubscriptionType)
      {
        case SubscriptionType.BatteryGauge:
          EventHandler<BatteryGaugeUpdatedEventArgs> batteryGaugeUpdated;
          if ((batteryGaugeUpdated = this.BatteryGaugeUpdated) != null)
          {
            BatteryGaugeUpdatedEventArgs e = BatteryGaugeUpdatedEventArgs.DeserializeFromBand((ICargoReader) reader);
            try
            {
              batteryGaugeUpdated((object) this, e);
              break;
            }
            catch
            {
              break;
            }
          }
          else
          {
            reader.ReadExactAndDiscard(BatteryGaugeUpdatedEventArgs.GetSerializedByteCount());
            break;
          }
        case SubscriptionType.LogEntry:
          if (this.LogEntryUpdated != null)
          {
            LogEntryUpdatedEventArgs.DeserializeFromBand((ICargoReader) reader);
            break;
          }
          LogEntryUpdatedEventArgs.DeserializeFromBand((ICargoReader) reader);
          break;
        default:
          reader.ReadExactAndDiscard((int) sampleHeader.SampleSize);
          break;
      }
    }

    private bool UploadFileToCloud(
      string relativeFilePath,
      FileIndex fileIndex,
      CancellationToken cancellationToken)
    {
      using (Stream fileStream = this.storageProvider.OpenFileForRead(relativeFilePath, -1))
      {
        string uploadId = string.Format("{0}-{1}", new object[2]
        {
          (object) this.storageProvider.GetFileCreationTimeUtc(relativeFilePath).ToString("yyyyMMddHHmmssfff"),
          (object) (int) fileIndex
        });
        return this.UploadFileToCloud(fileStream, fileIndex, uploadId, cancellationToken);
      }
    }

    public bool UploadFileToCloud(
      Stream fileStream,
      FileIndex fileIndex,
      string uploadId,
      CancellationToken cancellationToken)
    {
      LogCompressionAlgorithm compressionAlgorithm = LogCompressionAlgorithm.uncompressed;
      int logVersion = 0;
      LogFileTypes fileType;
      switch (fileIndex)
      {
        case FileIndex.Instrumentation:
          fileType = LogFileTypes.Telemetry;
          break;
        case FileIndex.CrashDump:
          fileType = LogFileTypes.CrashDump;
          break;
        default:
          BandCloudException e = new BandCloudException(CommonSR.UnsupportedFileTypeForCloudUpload);
          Logger.LogException(LogLevel.Error, (Exception) e);
          throw e;
      }
      return this.UploadFileToCloud(fileStream, fileType, uploadId, logVersion, compressionAlgorithm, (string) null, cancellationToken);
    }

    public Task<bool> UploadFileToCloudAsync(
      Stream fileStream,
      LogFileTypes fileType,
      string uploadId,
      int logVersion,
      LogCompressionAlgorithm compressionAlgorithm,
      string compressedFileCRC,
      CancellationToken cancellationToken)
    {
      return Task.Run<bool>((Func<bool>) (() => this.UploadFileToCloud(fileStream, fileType, uploadId, logVersion, compressionAlgorithm, compressedFileCRC, cancellationToken)));
    }

    public bool UploadFileToCloud(
      Stream fileStream,
      LogFileTypes fileType,
      string uploadId,
      int logVersion,
      LogCompressionAlgorithm compressionAlgorithm,
      string compressedFileCRC,
      CancellationToken cancellationToken)
    {
      UploadMetaData metadata = new UploadMetaData();
      if (this.DeviceTransport != null)
      {
        metadata.DeviceId = this.DeviceUniqueId.ToString();
        metadata.DeviceSerialNumber = this.SerialNumber;
        metadata.DeviceVersion = this.FirmwareVersions.ApplicationVersion.ToString(4);
        metadata.LogVersion = new int?(logVersion);
        metadata.PcbId = this.FirmwareVersions.PcbId.ToString();
      }
      if (fileType == LogFileTypes.Sensor)
        metadata.DeviceMetadataHint = "band";
      metadata.CompressionAlgorithm = new LogCompressionAlgorithm?(compressionAlgorithm);
      this.PopulateUploadMetadata(metadata);
      return this.cloudProvider.UploadFileToCloud(fileStream, fileType, uploadId, metadata, cancellationToken) == FileUploadStatus.UploadDone;
    }

    public bool UploadCrashDumpToCloud(
      Stream fileStream,
      FirmwareVersions deviceVersions,
      string uploadId,
      int logVersion,
      CancellationToken cancellationToken)
    {
      UploadMetaData metadata = new UploadMetaData();
      metadata.DeviceId = this.DeviceUniqueId.ToString();
      metadata.DeviceSerialNumber = this.SerialNumber;
      metadata.CompressionAlgorithm = new LogCompressionAlgorithm?(LogCompressionAlgorithm.uncompressed);
      metadata.DeviceVersion = deviceVersions.ApplicationVersion.ToString(4);
      metadata.LogVersion = new int?(logVersion);
      metadata.PcbId = deviceVersions.PcbId.ToString();
      this.PopulateUploadMetadata(metadata);
      metadata.DeviceSerialNumber = "000000000000";
      return this.cloudProvider.UploadFileToCloud(fileStream, LogFileTypes.CrashDump, uploadId, metadata, cancellationToken) == FileUploadStatus.UploadDone;
    }

    private void SetResponse(byte index, string response)
    {
      int dataSize = 323;
      using (CargoCommandWriter cargoCommandWriter = this.ProtocolBeginWrite(DeviceCommands.CargoFireballUISetSmsResponse, dataSize, CommandStatusHandling.ThrowOnlySeverityError))
      {
        cargoCommandWriter.WriteByte(index);
        cargoCommandWriter.WriteStringWithPadding(response, 161);
      }
    }

    private string[] GetAllResponses()
    {
      string[] allResponses = new string[8];
      int bytesToRead = 322 * allResponses.Length;
      try
      {
        using (CargoCommandReader cargoCommandReader = this.ProtocolBeginRead(DeviceCommands.CargoFireballUIGetAllSmsResponse, bytesToRead, CommandStatusHandling.DoNotCheck))
        {
          for (int index = 0; index < allResponses.Length; ++index)
            allResponses[index] = cargoCommandReader.ReadString(161);
          BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
        }
      }
      catch (BandIOException ex)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new BandIOException(ex.Message, ex);
      }
      Logger.Log(LogLevel.Info, "Retrieved all responses from the device, phoneCallResponses will be in the first half, SMS responses will be in the second half of the response array");
      return allResponses;
    }

    private void CheckIfStorageAvailable()
    {
      if (this.storageProvider == null)
      {
        Logger.Log(LogLevel.Error, CommonSR.OperationRequiredStorageProvider);
        throw new InvalidOperationException(CommonSR.OperationRequiredStorageProvider);
      }
    }

    public Task SetMeTileImageAsync(BandImage image, uint imageId = 4294967295) => Task.Run((Action) (() => this.SetMeTileImage(image, imageId)));

    public void SetMeTileImage(BandImage image, uint imageId = 4294967295)
    {
      Logger.Log(LogLevel.Info, "Setting the Me tile on the device");
      this.SetMeTileImageInternal(image, imageId, CancellationToken.None);
    }

    public BandImage GetMeTileImage()
    {
      Logger.Log(LogLevel.Info, "Getting the Me Tile image");
      return this.GetMeTileImageInternal(CancellationToken.None);
    }

    public Task<uint> GetMeTileIdAsync() => Task.Run<uint>((Func<uint>) (() => this.GetMeTileId()));

    public uint GetMeTileId()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      uint result = 0;
      Action<ICargoReader> readData = (Action<ICargoReader>) (r => result = r.ReadUInt32());
      this.ProtocolRead(DeviceCommands.CargoSystemSettingsGetMeTileImageID, 4, readData, 60000);
      return result;
    }

    public Task SendSmsNotificationAsync(
      uint callId,
      string name,
      string body,
      DateTime timestamp)
    {
      return Task.Run((Action) (() => this.SendSmsNotification(callId, name, body, timestamp, NotificationFlags.UnmodifiedNotificationSettings)));
    }

    public void SendSmsNotification(
      uint callID,
      string name,
      string body,
      DateTime timestamp,
      NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (name == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (name));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (body == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (body));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      this.SendNotification(NotificationID.Sms, NotificationPBMessageType.Messaging, (NotificationBase) new CargoSms(callID, name, body, timestamp, flagbits));
    }

    public Task SendSmsNotificationAsync(CargoSms sms) => Task.Run((Action) (() => this.SendSmsNotification(sms)));

    public void SendSmsNotification(CargoSms sms)
    {
      Logger.Log(LogLevel.Info, "Sending SMS Notification");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.SendNotification(NotificationID.Sms, NotificationPBMessageType.Messaging, (NotificationBase) sms);
    }

    public Task SendIncomingCallNotificationAsync(CargoCall call) => Task.Run((Action) (() => this.SendIncomingCallNotification(call)));

    public void SendIncomingCallNotification(CargoCall call)
    {
      Logger.Log(LogLevel.Info, "Sending incoming call notification");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      call.CallType = CargoCall.PhoneCallType.Incoming;
      this.SendNotification(NotificationID.IncomingCall, NotificationPBMessageType.Messaging, (NotificationBase) call);
    }

    public Task SendAnsweredCallNotificationAsync(CargoCall call) => Task.Run((Action) (() => this.SendAnsweredCallNotification(call)));

    public void SendAnsweredCallNotification(CargoCall call)
    {
      Logger.Log(LogLevel.Info, "Sending answered call notification");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      call.CallType = CargoCall.PhoneCallType.Answered;
      this.SendNotification(NotificationID.AnsweredCall, NotificationPBMessageType.Messaging, (NotificationBase) call);
    }

    public Task SendHangupCallNotificationAsync(CargoCall call) => Task.Run((Action) (() => this.SendHangupCallNotification(call)));

    public void SendHangupCallNotification(CargoCall call)
    {
      Logger.Log(LogLevel.Info, "Sending hangup call notification");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      call.CallType = CargoCall.PhoneCallType.Hangup;
      this.SendNotification(NotificationID.HangupCall, NotificationPBMessageType.Messaging, (NotificationBase) call);
    }

    public Task SendMissedCallNotificationAsync(CargoCall call) => Task.Run((Action) (() => this.SendMissedCallNotification(call)));

    public void SendMissedCallNotification(CargoCall call)
    {
      Logger.Log(LogLevel.Info, "Sending missed call notification");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      call.CallType = CargoCall.PhoneCallType.Missed;
      this.SendNotification(NotificationID.MissedCall, NotificationPBMessageType.Messaging, (NotificationBase) call);
    }

    public Task SendVoiceMailCallNotificationAsync(CargoCall call) => Task.Run((Action) (() => this.SendVoiceMailCallNotification(call)));

    public void SendVoiceMailCallNotification(CargoCall call)
    {
      Logger.Log(LogLevel.Info, "Sending voice mail call notification");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      call.CallType = CargoCall.PhoneCallType.VoiceMail;
      this.SendNotification(NotificationID.Voicemail, NotificationPBMessageType.Messaging, (NotificationBase) call);
    }

    public Task SendEmailNotificationAsync(string name, string subject, DateTime timestamp) => Task.Run((Action) (() => this.SendEmailNotification(name, subject, timestamp)));

    public void SendEmailNotification(string name, string subject, DateTime timestamp)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (name == null)
        throw new ArgumentNullException(nameof (name));
      if (subject == null)
        throw new ArgumentNullException(nameof (subject));
      Logger.Log(LogLevel.Info, "Sending email to NotificationANCSEmailAppGuid");
      this.SendNotification(NotificationID.Messaging, NotificationPBMessageType.Messaging, (NotificationBase) new NotificationEmail()
      {
        Name = name,
        Subject = subject,
        TimeStamp = timestamp
      });
    }

    public Task SendTileDialogAsync(
      Guid tileId,
      string lineOne,
      string lineTwo,
      NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings,
      bool forceDialog = false,
      bool throwErrorStatus = false)
    {
      return Task.Run((Action) (() => this.SendTileDialog(tileId, lineOne, lineTwo, flagbits, forceDialog, throwErrorStatus)));
    }

    public void SendTileDialog(
      Guid tileId,
      string lineOne,
      string lineTwo,
      NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings,
      bool forceDialog = false,
      bool throwErrorStatus = false)
    {
      this.ShowDialogHelper(tileId, lineOne, lineTwo, CancellationToken.None, flagbits.ToBandNotificationFlags());
    }

    public Task SendTileMessageAsync(Guid tileId, TileMessage message, bool throwErrorStatus = false) => Task.Run((Action) (() => this.SendTileMessage(tileId, message, throwErrorStatus)));

    public void SendTileMessage(Guid tileId, TileMessage message, bool throwErrorStatus = false)
    {
      Logger.Log(LogLevel.Info, "Sending tile message to tileId:{0}. Flags:{1}", (object) tileId, (object) message.Flags);
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.SendNotification(Microsoft.Band.Notifications.NotificationID.Messaging, NotificationPBMessageType.Messaging, (Microsoft.Band.Notifications.NotificationBase) new NotificationMessaging(tileId)
      {
        Timestamp = (DateTimeOffset) (message.timestampHasValue ? message.Timestamp : DateTime.FromFileTime(0L)),
        Title = message.Title,
        Body = message.Body,
        Flags = (byte) message.Flags
      });
    }

    public Task SendPageUpdateAsync(
      Guid tileId,
      Guid pageId,
      ushort pageLayoutIndex,
      IList<ITilePageElement> textFields)
    {
      return Task.Run((Action) (() => this.SendPageUpdate(tileId, pageId, pageLayoutIndex, textFields)));
    }

    public void SendPageUpdate(
      Guid tileId,
      Guid pageId,
      ushort pageLayoutIndex,
      IList<ITilePageElement> textFields)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (textFields == null)
        throw new ArgumentNullException(nameof (textFields));
      if (textFields.Count == 0)
        throw new ArgumentException(string.Format(CommonSR.GenericCountZero, new object[1]
        {
          (object) nameof (textFields)
        }), nameof (textFields));
      this.SetPages(tileId, CancellationToken.None, new PageData(pageId, (int) pageLayoutIndex, this.PageElementsAdminToPublic((IEnumerable<ITilePageElement>) textFields)).AsEnumerable<PageData>());
    }

    private IEnumerable<PageElementData> PageElementsAdminToPublic(
      IEnumerable<ITilePageElement> elements)
    {
      foreach (ITilePageElement iElement in elements)
      {
        switch (iElement)
        {
          case TileTextbox _:
            TileTextbox tileTextbox = iElement as TileTextbox;
            yield return (PageElementData) new TextBlockData((short) tileTextbox.ElementId, tileTextbox.TextboxValue);
            break;
          case TileWrappableTextbox _:
            TileWrappableTextbox wrappableTextbox = iElement as TileWrappableTextbox;
            yield return (PageElementData) new WrappedTextBlockData((short) wrappableTextbox.ElementId, wrappableTextbox.TextboxValue);
            break;
          case TileIconbox _:
            TileIconbox tileIconbox = iElement as TileIconbox;
            yield return (PageElementData) new IconData((short) tileIconbox.ElementId, tileIconbox.IconIndex);
            break;
          case TileBarcode _:
            TileBarcode tileBarcode = iElement as TileBarcode;
            Microsoft.Band.Tiles.Pages.BarcodeType codeType;
            switch (tileBarcode.CodeType)
            {
              case BarcodeType.Code39:
                codeType = Microsoft.Band.Tiles.Pages.BarcodeType.Code39;
                break;
              case BarcodeType.Pdf417:
                codeType = Microsoft.Band.Tiles.Pages.BarcodeType.Pdf417;
                break;
              default:
                throw new InvalidDataException("Unrecognized bar code type encountered");
            }
            yield return (PageElementData) new BarcodeData(codeType, (short) tileBarcode.ElementId, tileBarcode.BarcodeValue);
            break;
          default:
            throw new InvalidDataException("Unrecognized tile page element type encountered");
        }
      }
    }

    public Task ClearTileAsync(Guid tileId) => Task.Run((Action) (() => this.ClearTile(tileId)));

    public void ClearTile(Guid tileId)
    {
      Logger.Log(LogLevel.Info, "Clearing tile");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.RemovePages(tileId, CancellationToken.None);
    }

    public Task ClearPageAsync(Guid tileId, Guid pageId) => Task.Run((Action) (() => this.ClearPage(tileId, pageId)));

    public void ClearPage(Guid tileId, Guid pageId)
    {
      Logger.Log(LogLevel.Info, "Clearing page");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.SendNotification(NotificationID.GenericClearPage, NotificationPBMessageType.TileManagement, (NotificationBase) new NotificationGenericClearPage(tileId, pageId));
    }

    public Task SendCalendarEventsAsync(CalendarEvent[] events) => Task.Run((Action) (() => this.SendCalendarEvents(events)));

    public void SendCalendarEvents(CalendarEvent[] events)
    {
      Logger.Log(LogLevel.Info, "Sending Calendar events");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (events == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (events));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if (events.Length > 8)
      {
        ArgumentException e = new ArgumentException(string.Format(CommonSR.AppointmentsExceedLimit, new object[1]
        {
          (object) (ushort) 8
        }));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      this.ClearTile(new Guid("ec149021-ce45-40e9-aeee-08f86e4746a7"));
      foreach (NotificationBase notification in events)
        this.SendNotification(NotificationID.CalendarEventAdd, NotificationPBMessageType.CalendarUpdate, notification);
    }

    public Task VibrateAsync(AdminVibrationType vibrationType) => Task.Run((Action) (() => this.Vibrate(vibrationType)));

    public void Vibrate(AdminVibrationType vibrationType) => this.VibrateHelper(vibrationType.ToBandVibrationType(), CancellationToken.None);

    internal Task SendKeyboardMessageAsync(KeyboardCmdSample sample) => Task.Run((Action) (() => this.SendKeyboardMessage(sample)));

    internal void SendKeyboardMessage(KeyboardCmdSample sample)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      using (CargoCommandWriter cargoCommandWriter = this.ProtocolBeginWrite(DeviceCommands.CargoKeyboardCmd, 407, CommandStatusHandling.DoNotCheck))
      {
        cargoCommandWriter.WriteByte((byte) sample.KeyboardMsgType);
        cargoCommandWriter.WriteByte(sample.NumOfCandidates);
        cargoCommandWriter.WriteByte(sample.WordIndex);
        cargoCommandWriter.WriteUInt32(sample.DataLength);
        cargoCommandWriter.Write(sample.Datafield, 0, 400);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
    }

    private void SendNotification(
      NotificationID notificationId,
      NotificationPBMessageType notificationPbType,
      NotificationBase notification)
    {
      int argBufSize = 0;
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) null;
      ushort commandId;
      int byteCount;
      switch (this.BandTypeConstants.BandType)
      {
        case BandType.Cargo:
          commandId = DeviceCommands.CargoNotification;
          byteCount = 2 + notification.GetSerializedByteCount();
          break;
        case BandType.Envoy:
          commandId = DeviceCommands.CargoNotificationProtoBuf;
          byteCount = notification.GetSerializedProtobufByteCount();
          argBufSize = 4;
          writeArgBuf = (Action<ICargoWriter>) (w =>
          {
            w.WriteUInt16((ushort) byteCount);
            w.WriteUInt16((ushort) notificationPbType);
          });
          break;
        default:
          throw new InvalidOperationException();
      }
      using (CargoCommandWriter cargoCommandWriter = this.ProtocolBeginWrite(commandId, argBufSize, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
      {
        switch (this.BandTypeConstants.BandType)
        {
          case BandType.Cargo:
            cargoCommandWriter.WriteUInt16((ushort) notificationId);
            notification.SerializeToBand((ICargoWriter) cargoCommandWriter);
            break;
          case BandType.Envoy:
            CodedOutputStream output = new CodedOutputStream((Stream) cargoCommandWriter, byteCount);
            notification.SerializeProtobufToBand(output);
            output.Flush();
            break;
        }
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
    }

    public async Task PersonalizeDeviceAsync(
      StartStrip startStrip = null,
      BandImage image = null,
      BandTheme color = null,
      uint imageId = 4294967295,
      IDictionary<Guid, BandTheme> customColors = null)
    {
      await Task.Run((Action) (() => this.PersonalizeDevice(startStrip, image, color, imageId, customColors)));
    }

    public void PersonalizeDevice(
      StartStrip startStrip = null,
      BandImage image = null,
      BandTheme theme = null,
      uint imageId = 4294967295,
      IDictionary<Guid, BandTheme> customThemes = null)
    {
      Logger.Log(LogLevel.Verbose, "Personalizing device");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      if (image != null)
        this.ValidateMeTileImage(image, imageId);
      if (startStrip != null)
        this.SetStartStripValidator(startStrip);
      if (customThemes != null)
        this.SetTileThemesValidator(customThemes);
      this.RunUsingSynchronizedFirmwareUI((Action) (() =>
      {
        if (image != null)
          this.SetMeTileImageInternal(image, imageId);
        if (theme != null)
          this.SetThemeInternal(theme);
        if (startStrip != null)
          this.SetStartStripHelperInsideSync(startStrip);
        if (customThemes == null)
          return;
        this.SetTileThemesHelper(customThemes);
      }), (Action) (() =>
      {
        if (startStrip == null)
          return;
        this.SetStartStripHelperOutsideSync(startStrip);
      }));
    }

    internal new void InitializeCachedProperties()
    {
      base.InitializeCachedProperties();
      this.FirmwareVersions = base.FirmwareVersions.ToFirmwareVersions();
      this.loggerProvider.Log(ProviderLogLevel.Verbose, "Firmware versions:");
      this.loggerProvider.Log(ProviderLogLevel.Verbose, "Bootloader version: {0}", (object) this.FirmwareVersions.BootloaderVersion);
      this.loggerProvider.Log(ProviderLogLevel.Verbose, "Updater version: {0}", (object) this.FirmwareVersions.UpdaterVersion);
      this.loggerProvider.Log(ProviderLogLevel.Verbose, "Application version: {0}", (object) this.FirmwareVersions.ApplicationVersion);
      this.loggerProvider.Log(ProviderLogLevel.Verbose, "Running App: {0}", (object) this.FirmwareApp);
      if (this.FirmwareApp != FirmwareApp.App || this.SerialNumber != null && this.DeviceUniqueId != Guid.Empty)
        return;
      string serial;
      Guid uniqueId;
      this.GetDeviceSerialAndUniqueId(out serial, out uniqueId);
      this.SerialNumber = serial;
      this.DeviceUniqueId = uniqueId;
    }

    private void GetDeviceSerialAndUniqueId(out string serial, out Guid uniqueId)
    {
      serial = this.GetProductSerialNumber();
      switch (this.ConnectedBandConstants.BandClass)
      {
        case BandClass.Cargo:
          uniqueId = this.GetDeviceUniqueId();
          break;
        case BandClass.Envoy:
          uniqueId = CargoClient.ConstructDeviceIdFromSerialNumber(serial);
          break;
        default:
          this.loggerProvider.Log(ProviderLogLevel.Warning, string.Format("Unrecognized band class; PcbId = {0}", new object[1]
          {
            (object) this.FirmwareVersions.PcbId
          }));
          uniqueId = Guid.Empty;
          break;
      }
    }

    private static Guid ConstructDeviceIdFromSerialNumber(string serialNumber) => serialNumber.Length == 12 ? new Guid(string.Format("{0}{1}", new object[2]
    {
      (object) "FFFFFFFF-FFFF-FFFF-FFFF-",
      (object) serialNumber
    })) : (serialNumber.Length < 12 ? new Guid(string.Format("{0}{1}{2}", new object[3]
    {
      (object) "FFFFFFFF-FFFF-FFFF-FFFF-",
      (object) new string('0', 12 - serialNumber.Length),
      (object) serialNumber
    })) : new Guid(string.Format("{0}{1}", new object[2]
    {
      (object) "FFFFFFFF-FFFF-FFFF-FFFF-",
      (object) serialNumber.Substring(0, 12)
    })));

    public RunningAppType DeviceTransportApp => this.runningFirmwareApp.ToRunningAppType();

    public Guid DeviceUniqueId { get; internal set; }

    public string SerialNumber { get; internal set; }

    public FirmwareVersions FirmwareVersions { get; private set; }

    public Task<RunningAppType> GetRunningAppAsync() => Task.Run<RunningAppType>((Func<RunningAppType>) (() => this.GetRunningApp()));

    public RunningAppType GetRunningApp() => this.runningFirmwareApp.ToRunningAppType();

    public async Task SetDeviceThemeAsync(BandTheme color) => await Task.Run((Action) (() => this.SetDeviceTheme(color)));

    public void SetDeviceTheme(BandTheme theme)
    {
      Logger.Log(LogLevel.Info, "Setting the first party theme");
      this.SetThemeInternal(theme, CancellationToken.None);
    }

    public async Task SetTileThemesAsync(Dictionary<Guid, BandTheme> customColors) => await Task.Run((Action) (() => this.SetTileThemes(customColors)));

    public void SetTileThemes(Dictionary<Guid, BandTheme> customThemes)
    {
      Logger.Log(LogLevel.Info, "Setting the list of tile themes");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.SetTileThemesValidator((IDictionary<Guid, BandTheme>) customThemes);
      this.RunUsingSynchronizedFirmwareUI((Action) (() => this.SetTileThemesHelper((IDictionary<Guid, BandTheme>) customThemes)));
    }

    private void SetTileThemesValidator(IDictionary<Guid, BandTheme> customThemes)
    {
      if (customThemes == null)
      {
        ArgumentNullException e = new ArgumentNullException("customColors");
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      foreach (KeyValuePair<Guid, BandTheme> customTheme in (IEnumerable<KeyValuePair<Guid, BandTheme>>) customThemes)
        this.ValidateTileTheme(customTheme.Value, customTheme.Key);
    }

    private void SetTileThemesHelper(IDictionary<Guid, BandTheme> customThemes)
    {
      foreach (KeyValuePair<Guid, BandTheme> customTheme in (IEnumerable<KeyValuePair<Guid, BandTheme>>) customThemes)
        this.SetTileThemeInternal(customTheme.Value, customTheme.Key);
    }

    public async Task SetTileThemeAsync(BandTheme color, Guid id) => await Task.Run((Action) (() => this.SetTileTheme(color, id)));

    public void SetTileTheme(BandTheme theme, Guid id)
    {
      Logger.Log(LogLevel.Info, "Setting the tile theme");
      this.SetTileThemeInternal(theme, id, CancellationToken.None);
    }

    public async Task<BandTheme> GetDeviceThemeAsync() => await Task.Run<BandTheme>((Func<BandTheme>) (() => this.GetDeviceTheme()));

    public BandTheme GetDeviceTheme()
    {
      Logger.Log(LogLevel.Info, "Getting first party theme");
      return this.GetThemeInternal(CancellationToken.None);
    }

    public async Task ResetThemeColorsAsync() => await Task.Run((Action) (() => this.ResetThemeColors()));

    public void ResetThemeColors()
    {
      Logger.Log(LogLevel.Info, "Resetting theme colors");
      this.ResetThemeInternal(CancellationToken.None);
    }

    public Task<StartStrip> GetStartStripAsync() => Task.Run<StartStrip>((Func<StartStrip>) (() => this.GetStartStrip()));

    public StartStrip GetStartStrip()
    {
      Logger.Log(LogLevel.Verbose, "Getting start strip");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      return new StartStrip(this.GetInstalledTiles().Select<TileData, AdminBandTile>((Func<TileData, AdminBandTile>) (t => t.ToAdminBandTile())));
    }

    public Task<IList<AdminBandTile>> GetDefaultTilesAsync() => Task.Run<IList<AdminBandTile>>((Func<IList<AdminBandTile>>) (() => this.GetDefaultTiles()));

    public IList<AdminBandTile> GetDefaultTiles()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      return (IList<AdminBandTile>) this.GetDefaultTilesInternal().Select<TileData, AdminBandTile>((Func<TileData, AdminBandTile>) (tileData => tileData.ToAdminBandTile())).ToList<AdminBandTile>();
    }

    public Task<AdminBandTile> GetTileAsync(Guid id) => Task.Run<AdminBandTile>((Func<AdminBandTile>) (() => this.GetTile(id)));

    public AdminBandTile GetTile(Guid id)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      return this.GetInstalledTiles().Where<TileData>((Func<TileData, bool>) (t => t.AppID == id)).Select<TileData, AdminBandTile>((Func<TileData, AdminBandTile>) (t => t.ToAdminBandTile())).FirstOrDefault<AdminBandTile>();
    }

    public Task<StartStrip> GetStartStripNoImagesAsync() => Task.Run<StartStrip>((Func<StartStrip>) (() => this.GetStartStripNoImages()));

    public StartStrip GetStartStripNoImages()
    {
      Logger.Log(LogLevel.Verbose, "Getting start strip without images");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      return new StartStrip(this.GetInstalledTilesNoIcons().Select<TileData, AdminBandTile>((Func<TileData, AdminBandTile>) (t => t.ToAdminBandTile())));
    }

    public Task<IList<AdminBandTile>> GetDefaultTilesNoImagesAsync() => Task.Run<IList<AdminBandTile>>((Func<IList<AdminBandTile>>) (() => this.GetDefaultTilesNoImages()));

    public IList<AdminBandTile> GetDefaultTilesNoImages()
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      return (IList<AdminBandTile>) this.GetDefaultTilesNoIconsInternal().Select<TileData, AdminBandTile>((Func<TileData, AdminBandTile>) (t => t.ToAdminBandTile())).ToList<AdminBandTile>();
    }

    public Task<AdminBandTile> GetTileNoImageAsync(Guid id) => Task.Run<AdminBandTile>((Func<AdminBandTile>) (() => this.GetTileNoImage(id)));

    public AdminBandTile GetTileNoImage(Guid id)
    {
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      return this.GetInstalledTilesNoIcons().Where<TileData>((Func<TileData, bool>) (t => t.AppID == id)).Select<TileData, AdminBandTile>((Func<TileData, AdminBandTile>) (t => t.ToAdminBandTile())).FirstOrDefault<AdminBandTile>();
    }

    public Task SetStartStripAsync(StartStrip tiles) => Task.Run((Action) (() => this.SetStartStrip(tiles)));

    public void SetStartStrip(StartStrip tiles)
    {
      Logger.Log(LogLevel.Verbose, "Setting start strip");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.SetStartStripValidator(tiles);
      this.RunUsingSynchronizedFirmwareUI((Action) (() => this.SetStartStripHelperInsideSync(tiles)), (Action) (() => this.SetStartStripHelperOutsideSync(tiles)));
    }

    private void SetStartStripValidator(StartStrip tiles)
    {
      if (tiles == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (tiles));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if ((long) tiles.Count > (long) this.GetTileCapacity() || (long) tiles.Count > (long) this.GetTileMaxAllocatedCapacity())
      {
        ArgumentException e = new ArgumentException(string.Format(CommonSR.GenericCountMax, new object[1]
        {
          (object) nameof (tiles)
        }));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
    }

    private void SetStartStripHelperInsideSync(StartStrip tiles)
    {
      IList<AdminBandTile> list1 = (IList<AdminBandTile>) this.GetDefaultTilesNoIconsInternal().Select<TileData, AdminBandTile>((Func<TileData, AdminBandTile>) (t => t.ToAdminBandTile())).ToList<AdminBandTile>();
      IList<AdminBandTile> list2 = (IList<AdminBandTile>) this.GetInstalledTilesNoIcons().Select<TileData, AdminBandTile>((Func<TileData, AdminBandTile>) (t => t.ToAdminBandTile())).ToList<AdminBandTile>();
      for (int index = 0; index < list2.Count; ++index)
      {
        if (!tiles.Contains(list2[index].Id))
        {
          this.UnregisterTileIcons(list2[index].Id);
          list2.RemoveAt(index);
          --index;
        }
      }
      Logger.Log(LogLevel.Info, "Removed all tiles that are currently on device, but shouldn't be");
      for (int index = 0; index < tiles.Count; ++index)
      {
        bool flag1 = false;
        foreach (AdminBandTile adminBandTile in (IEnumerable<AdminBandTile>) list1)
        {
          if (adminBandTile.Id == tiles[index].Id)
          {
            flag1 = true;
            break;
          }
        }
        bool flag2 = false;
        foreach (AdminBandTile adminBandTile in (IEnumerable<AdminBandTile>) list2)
        {
          if (adminBandTile.Id == tiles[index].Id)
          {
            flag2 = true;
            break;
          }
        }
        if (!(flag1 & flag2))
        {
          if (flag1 && !flag2)
            this.DynamicAppRegisterDefaultTile(tiles[index]);
          else if (!flag1 & flag2)
          {
            if (tiles[index].Images != null)
            {
              this.DynamicAppRegisterTileOrIcons(tiles[index], true);
              this.SetTileIconIndexes(tiles[index].Id, tiles[index].TileImageIndex, tiles[index].BadgeImageIndex, tiles[index].NotificationImageIndex);
            }
          }
          else
          {
            if (tiles[index].Images == null)
              throw new ArgumentException(CommonSR.NewTileRequiresImages);
            this.DynamicAppRegisterTileOrIcons(tiles[index], false);
            this.SetTileIconIndexes(tiles[index].Id, tiles[index].TileImageIndex, tiles[index].BadgeImageIndex, tiles[index].NotificationImageIndex);
          }
        }
      }
      this.InstalledAppListSet((IList<AdminBandTile>) tiles);
      for (int index = 0; index < tiles.Count; ++index)
      {
        if (tiles[index].Theme != null)
          this.SetTileThemeInternal(tiles[index].Theme, tiles[index].Id);
      }
    }

    private void SetStartStripHelperOutsideSync(StartStrip tiles)
    {
      foreach (AdminBandTile tile in tiles)
      {
        Logger.Log(LogLevel.Verbose, "Apply all queued-up Layout removal actions for tile: {0}", (object) tile.Name);
        foreach (uint layoutIndex in (IEnumerable<uint>) tile.LayoutsToRemove)
          this.DynamicPageLayoutRemoveLayout(tile.Id, layoutIndex);
        Logger.Log(LogLevel.Verbose, "Apply all queued-up Layout add/overwrite actions for tile: {0}", (object) tile.Name);
        foreach (KeyValuePair<uint, TileLayout> layout in tile.Layouts)
          this.DynamicPageLayoutSetSerializedLayout(tile.Id, layout.Key, layout.Value.layoutBlob);
      }
      Logger.Log(LogLevel.Info, "Finished layout operations on the tiles");
    }

    public Task UpdateTileAsync(AdminBandTile tile) => Task.Run((Action) (() => this.UpdateTile(tile)));

    public void UpdateTile(AdminBandTile tile)
    {
      Logger.Log(LogLevel.Verbose, "Updating tile");
      this.CheckIfDisposed();
      this.CheckIfDisconnectedOrUpdateMode();
      this.UpdateTileValidator(tile);
      List<AdminBandTile> list1 = this.GetDefaultTilesNoIconsInternal().Select<TileData, AdminBandTile>((Func<TileData, AdminBandTile>) (t => t.ToAdminBandTile())).ToList<AdminBandTile>();
      IList<AdminBandTile> list2 = (IList<AdminBandTile>) this.GetInstalledTilesNoIcons().Select<TileData, AdminBandTile>((Func<TileData, AdminBandTile>) (t => t.ToAdminBandTile())).ToList<AdminBandTile>();
      bool isDefault = false;
      foreach (AdminBandTile adminBandTile in (IEnumerable<AdminBandTile>) list1)
      {
        if (adminBandTile.Id == tile.Id)
        {
          isDefault = true;
          break;
        }
      }
      bool alreadyOnDevice = false;
      foreach (AdminBandTile adminBandTile in (IEnumerable<AdminBandTile>) list2)
      {
        if (adminBandTile.Id == tile.Id)
        {
          alreadyOnDevice = true;
          break;
        }
      }
      this.RunUsingSynchronizedFirmwareUI((Action) (() => this.UpdateTileHelperInsideSync(tile, isDefault, alreadyOnDevice)), (Action) (() => this.UpdateTileHelperOutsideSync(tile, alreadyOnDevice)));
    }

    private void UpdateTileValidator(AdminBandTile tile)
    {
      if (tile == null)
      {
        ArgumentNullException e = new ArgumentNullException(nameof (tile));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
    }

    private void UpdateTileHelperInsideSync(
      AdminBandTile tile,
      bool isDefault,
      bool alreadyOnDevice)
    {
      if (!alreadyOnDevice)
        return;
      if (!(isDefault & alreadyOnDevice))
      {
        if (isDefault && !alreadyOnDevice)
          this.DynamicAppRegisterDefaultTile(tile);
        else if (!isDefault & alreadyOnDevice)
        {
          if (tile.Images != null)
          {
            this.DynamicAppRegisterTileOrIcons(tile, true);
            this.SetTileIconIndexes(tile.Id, tile.TileImageIndex, tile.BadgeImageIndex, tile.NotificationImageIndex);
          }
        }
        else
        {
          if (tile.Images == null)
          {
            ArgumentException e = new ArgumentException(CommonSR.NewTileRequiresImages);
            Logger.LogException(LogLevel.Error, (Exception) e);
            throw e;
          }
          this.DynamicAppRegisterTileOrIcons(tile, false);
          this.SetTileIconIndexes(tile.Id, tile.TileImageIndex, tile.BadgeImageIndex, tile.NotificationImageIndex);
        }
      }
      this.InstalledAppListSetTile(tile);
    }

    private void UpdateTileHelperOutsideSync(AdminBandTile tile, bool alreadyOnDevice)
    {
      if (!alreadyOnDevice)
        return;
      Logger.Log(LogLevel.Info, "Apply all queued-up Layout removal actions");
      foreach (uint layoutIndex in (IEnumerable<uint>) tile.LayoutsToRemove)
        this.DynamicPageLayoutRemoveLayout(tile.Id, layoutIndex);
      Logger.Log(LogLevel.Info, "Apply all queued-up Layout add/overwrite actions");
      foreach (KeyValuePair<uint, TileLayout> layout in tile.Layouts)
        this.DynamicPageLayoutSetSerializedLayout(tile.Id, layout.Key, layout.Value.layoutBlob);
      Logger.Log(LogLevel.Info, "Finished layout operations on the tile");
    }

    public Task<uint> GetMaxTileCountAsync() => Task.Run<uint>((Func<uint>) (() => this.GetMaxTileCount()));

    public uint GetMaxTileCount() => this.GetTileCapacity();

    private void SetIconIndexValidator(uint iconIndex)
    {
      this.CheckIfDisposed();
      if (this.DeviceTransport == null)
      {
        InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      if ((long) iconIndex >= (long) this.ConnectedBandConstants.MaxIconsPerTile)
      {
        ArgumentOutOfRangeException e = new ArgumentOutOfRangeException(nameof (iconIndex));
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
    }

    public Task SetTileIconIndexAsync(Guid tileId, uint iconIndex) => Task.Run((Action) (() => this.SetTileIconIndex(tileId, iconIndex)));

    public void SetTileIconIndex(Guid tileId, uint iconIndex)
    {
      this.SetIconIndexValidator(iconIndex);
      this.SetMainIconIndex(tileId, iconIndex);
    }

    public Task SetTileBadgeIconIndexAsync(Guid tileId, uint iconIndex) => Task.Run((Action) (() => this.SetTileBadgeIconIndex(tileId, iconIndex)));

    public void SetTileBadgeIconIndex(Guid tileId, uint iconIndex)
    {
      this.SetIconIndexValidator(iconIndex);
      this.SetBadgeIconIndex(tileId, iconIndex);
    }

    public Task SetTileNotificationIconIndexAsync(Guid id, uint iconIndex) => Task.Run((Action) (() => this.SetTileNotificationIconIndex(id, iconIndex)));

    public void SetTileNotificationIconIndex(Guid tileId, uint iconIndex)
    {
      this.SetIconIndexValidator(iconIndex);
      this.SetNotificationIconIndex(tileId, iconIndex);
    }

    public Task<AdminTileSettings> GetTileSettingsAsync(Guid id) => Task.Run<AdminTileSettings>((Func<AdminTileSettings>) (() => this.GetTileSettings(id)));

    public AdminTileSettings GetTileSettings(Guid id)
    {
      this.CheckIfDisposed();
      if (this.DeviceTransport == null)
      {
        InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      AdminTileSettings settings = AdminTileSettings.None;
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteGuid(id));
      Action<ICargoReader> readData = (Action<ICargoReader>) (r => settings = (AdminTileSettings) r.ReadUInt16());
      this.ProtocolRead(DeviceCommands.CargoInstalledAppListGetSettingsMask, 16, 2, writeArgBuf, readData);
      return settings;
    }

    public Task SetTileSettingsAsync(Guid tileId, AdminTileSettings settings) => Task.Run((Action) (() => this.SetTileSettings(tileId, settings)));

    public void SetTileSettings(Guid tileId, AdminTileSettings settings)
    {
      this.CheckIfDisposed();
      if (this.DeviceTransport == null)
      {
        InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      Action<ICargoWriter> writeData = (Action<ICargoWriter>) (w =>
      {
        w.WriteGuid(tileId);
        w.WriteUInt16((ushort) settings);
      });
      this.ProtocolWriteWithData(DeviceCommands.CargoInstalledAppListSetSettingsMask, 18, writeData);
    }

    public Task EnableTileSettingsAsync(Guid tileId, AdminTileSettings settings) => Task.Run((Action) (() => this.EnableTileSettings(tileId, settings)));

    public void EnableTileSettings(Guid tileId, AdminTileSettings settings) => this.ChangeTileSettings(tileId, settings, DeviceCommands.CargoInstalledAppListEnableSetting);

    public Task DisableTileSettingsAsync(Guid tileId, AdminTileSettings settings) => Task.Run((Action) (() => this.DisableTileSettings(tileId, settings)));

    public void DisableTileSettings(Guid tileId, AdminTileSettings settings) => this.ChangeTileSettings(tileId, settings, DeviceCommands.CargoInstalledAppListDisableSetting);

    private void ChangeTileSettings(Guid tileId, AdminTileSettings settings, ushort commandId)
    {
      this.CheckIfDisposed();
      if (this.DeviceTransport == null)
      {
        InvalidOperationException e = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
        Logger.LogException(LogLevel.Error, (Exception) e);
        throw e;
      }
      ushort num = (ushort) settings;
      ushort bitIndex = 0;
      while (num > (ushort) 0)
      {
        if ((int) num % 2 != 0)
        {
          Action<ICargoWriter> writeData = (Action<ICargoWriter>) (w =>
          {
            w.WriteGuid(tileId);
            w.WriteUInt16(bitIndex);
          });
          this.ProtocolWriteWithData(commandId, 18, writeData);
        }
        num >>= 1;
        bitIndex++;
      }
    }

    private void InstalledAppListSet(IList<AdminBandTile> orderedList)
    {
      List<TileData> orderedList1 = new List<TileData>();
      for (int index = 0; index < orderedList.Count; ++index)
      {
        TileData tileData = orderedList[index].ToTileData((uint) index);
        orderedList1.Add(tileData);
      }
      this.SetStartStripData((IEnumerable<TileData>) orderedList1, orderedList1.Count);
    }

    private AdminBandTile InstalledAppListGetTile(Guid guid)
    {
      Action<ICargoWriter> writeArgBuf = (Action<ICargoWriter>) (w => w.WriteGuid(guid));
      int bytesToRead = 1024 + TileData.GetSerializedByteCount();
      using (PooledBuffer buffer = BufferServer.GetBuffer(1024))
      {
        TileData data;
        using (CargoCommandReader reader = this.ProtocolBeginRead(DeviceCommands.CargoInstalledAppListGetTile, 16, bytesToRead, writeArgBuf, CommandStatusHandling.ThrowOnlySeverityError))
        {
          this.DeviceTransport.CargoStream.ReadTimeout = 60000;
          reader.ReadExact(buffer.Buffer, 0, buffer.Length);
          data = TileData.DeserializeFromBand((ICargoReader) reader);
          data.Icon = BandIconRleCodec.DecodeTileIconRle(buffer);
        }
        return data.ToAdminBandTile();
      }
    }

    private void InstalledAppListSetTile(AdminBandTile tile)
    {
      TileData tileData = tile.ToTileData();
      Logger.Log(LogLevel.Info, "Invoking CargoInstalledAppListSetTile");
      using (CargoCommandWriter writer = this.ProtocolBeginWrite(DeviceCommands.CargoInstalledAppListSetTile, TileData.GetSerializedByteCount(), CommandStatusHandling.DoNotCheck))
      {
        this.DeviceTransport.CargoStream.ReadTimeout = 60000;
        tileData.SerializeToBand((ICargoWriter) writer);
        BandClient.CheckStatus(writer.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, this.loggerProvider);
      }
    }

    private void DynamicAppRegisterDefaultTile(AdminBandTile tile)
    {
      int dataSize = 20;
      Logger.Log(LogLevel.Verbose, "Invoking CargoDynamicAppRegisterApp for tile: {0}", (object) tile.Name);
      try
      {
        using (CargoCommandWriter cargoCommandWriter = this.ProtocolBeginWrite(DeviceCommands.CargoDynamicAppRegisterApp, dataSize, CommandStatusHandling.ThrowOnlySeverityError))
        {
          cargoCommandWriter.WriteGuid(tile.Id);
          cargoCommandWriter.WriteInt32(0);
        }
      }
      catch (BandIOException ex)
      {
        throw;
      }
      catch (Exception ex)
      {
        throw new BandIOException(ex.Message, ex);
      }
    }

    private void DynamicAppRegisterTileOrIcons(AdminBandTile tile, bool iconsAlreadyRegistered) => this.RegisterTileIcons(tile.Id, tile.Name, (IEnumerable<BandIcon>) tile.Images, iconsAlreadyRegistered);
  }
}
