using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

namespace Microsoft.Band.Admin;

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

    public DynamicAdminBandConstants ConnectedAdminBandConstants
    {
        get
        {
            if (base.DeviceTransport != null)
            {
                BandType bandType = base.BandTypeConstants.BandType;
                if (bandType != BandType.Cargo && bandType == BandType.Envoy)
                {
                    return DynamicAdminBandConstants.Envoy;
                }
                return DynamicAdminBandConstants.Cargo;
            }
            return null;
        }
    }

    public IDynamicBandConstants ConnectedBandConstants
    {
        get
        {
            if (base.DeviceTransport != null)
            {
                BandType bandType = base.BandTypeConstants.BandType;
                if (bandType != BandType.Cargo && bandType == BandType.Envoy)
                {
                    return DynamicBandConstants.EnvoyConstants;
                }
                return DynamicBandConstants.CargoConstants;
            }
            return null;
        }
    }

    public string UserAgent
    {
        get
        {
            if (cloudProvider == null)
            {
                throw new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
            }
            return cloudProvider.UserAgent;
        }
        set
        {
            if (cloudProvider == null)
            {
                throw new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
            }
            cloudProvider.SetUserAgent(value, appOverride: true);
        }
    }

    public RunningAppType DeviceTransportApp => runningFirmwareApp.ToRunningAppType();

    public Guid DeviceUniqueId { get; internal set; }

    public string SerialNumber { get; internal set; }

    public new FirmwareVersions FirmwareVersions { get; private set; }

    public event EventHandler<BatteryGaugeUpdatedEventArgs> BatteryGaugeUpdated;

    public event EventHandler<LogEntryUpdatedEventArgs> LogEntryUpdated;

    public event EventHandler Disconnected;

    private CargoClient(IDeviceTransport transport, IApplicationPlatformProvider applicationPlatformProvider)
        : base(transport, new LoggerProvider(), applicationPlatformProvider)
    {
        disposed = false;
        runningFirmwareApp = FirmwareApp.Invalid;
        protocolLock = new object();
    }

    public static CargoClient CreateRestrictedClient(IBandInfo deviceInfo)
    {
        if (deviceInfo == null)
        {
            ArgumentNullException ex = new ArgumentNullException("deviceInfo");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (!(deviceInfo is BluetoothDeviceInfo))
        {
            Logger.Log(LogLevel.Error, "deviceInfo is not BluetoothDeviceInfo");
            throw new ArgumentException("deviceInfo");
        }
        CargoClient cargoClient = null;
        IDeviceTransport deviceTransport = BluetoothTransport.Create(deviceInfo, new LoggerProvider(), 2);
        try
        {
            cargoClient = new CargoClient(deviceTransport, StoreApplicationPlatformProvider.Current);
            cargoClient.InitializeCachedProperties();
            Logger.Log(LogLevel.Info, "Created CargoClient (Restricted)");
            return cargoClient;
        }
        catch
        {
            if (cargoClient != null)
            {
                cargoClient.Dispose();
            }
            else
            {
                deviceTransport.Dispose();
            }
            throw;
        }
    }

    public Task<SyncResult> ObsoleteSyncDeviceToCloudAsync(CancellationToken cancellationToken, IProgress<SyncProgress> progress = null, bool logsOnly = false)
    {
        SyncTasks syncTasks = ((!logsOnly) ? (SyncTasks.TimeAndTimeZone | SyncTasks.EphemerisFile | SyncTasks.TimeZoneFile | SyncTasks.DeviceCrashDump | SyncTasks.DeviceInstrumentation | SyncTasks.UserProfile | SyncTasks.SensorLog | SyncTasks.WebTiles) : (SyncTasks.TimeAndTimeZone | SyncTasks.EphemerisFile | SyncTasks.UserProfileFirmwareBytes | SyncTasks.SensorLog | SyncTasks.WebTilesForced));
        return SyncDeviceToCloudAsync(cancellationToken, progress, syncTasks);
    }

    public Task<SyncResult> SyncRequiredBandInfoAsync(CancellationToken cancellationToken, IProgress<SyncProgress> progress = null)
    {
        SyncTasks syncTasks = SyncTasks.TimeAndTimeZone | SyncTasks.EphemerisFile | SyncTasks.UserProfileFirmwareBytes | SyncTasks.SensorLog;
        return SyncDeviceToCloudAsync(cancellationToken, progress, syncTasks);
    }

    public Task<SyncResult> SyncAuxiliaryBandInfoAsync(CancellationToken cancellationToken)
    {
        SyncTasks syncTasks = SyncTasks.TimeZoneFile | SyncTasks.DeviceCrashDump | SyncTasks.DeviceInstrumentation | SyncTasks.WebTilesForced;
        return SyncDeviceToCloudAsync(cancellationToken, null, syncTasks);
    }

    public Task<SyncResult> SyncAllBandInfoAsync(CancellationToken cancellationToken)
    {
        SyncTasks syncTasks = SyncTasks.TimeAndTimeZone | SyncTasks.EphemerisFile | SyncTasks.TimeZoneFile | SyncTasks.DeviceCrashDump | SyncTasks.DeviceInstrumentation | SyncTasks.UserProfile | SyncTasks.SensorLog | SyncTasks.WebTiles;
        return SyncDeviceToCloudAsync(cancellationToken, null, syncTasks);
    }

    internal Task<SyncResult> SyncDeviceToCloudAsync(CancellationToken cancellationToken, IProgress<SyncProgress> progress, SyncTasks syncTasks)
    {
        return Task.Run(() => SyncDeviceToCloud(cancellationToken, progress, syncTasks));
    }

    private SyncResult SyncDeviceToCloud(CancellationToken cancellationToken, IProgress<SyncProgress> progress, SyncTasks syncTasks)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        KdkSyncProgress progressTracker = new KdkSyncProgress(progress, syncTasks);
        lock (loggerLock)
        {
            return SyncDeviceToCloudCore(cancellationToken, progressTracker, syncTasks);
        }
    }

    private SyncResult SyncDeviceToCloudCore(CancellationToken cancellationToken, KdkSyncProgress progressTracker, SyncTasks syncTasks)
    {
        bool doRethrows = false;
        Func<string, Action, string, long> LogDoAndMeasure = delegate(string logFirst, Action code, string logOnError)
        {
            Stopwatch stopwatch2 = Stopwatch.StartNew();
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(logFirst))
            {
                Logger.Log(LogLevel.Info, logFirst);
            }
            try
            {
                code();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Logger.LogException(LogLevel.Warning, e, logOnError);
                if (doRethrows)
                {
                    throw;
                }
            }
            stopwatch2.Stop();
            return stopwatch2.ElapsedMilliseconds;
        };
        Func<Action, string, long> func = (Action code, string logOnError) => LogDoAndMeasure(string.Empty, code, logOnError);
        Stopwatch stopwatch = Stopwatch.StartNew();
        SyncResult syncResult = new SyncResult();
        try
        {
            if (syncTasks.HasFlag(SyncTasks.TimeAndTimeZone))
            {
                syncResult.TimeZoneElapsed = func(delegate
                {
                    progressTracker.SetState(SyncState.CurrentTimeAndTimeZone);
                    SetCurrentTimeAndTimeZone(cancellationToken);
                    progressTracker.CurrentTimeAndTimeZoneProgress.Complete();
                }, "Exception occurred in UpdateDeviceTimeAndTimeZone");
            }
            if (cloudProvider == null)
            {
                InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
                Logger.LogException(LogLevel.Error, ex);
                throw ex;
            }
            if (syncTasks.HasFlag(SyncTasks.EphemerisFile))
            {
                bool updateWasDone = false;
                long value = LogDoAndMeasure("Updating ephemeris file (if needed)...", delegate
                {
                    progressTracker.SetState(SyncState.Ephemeris);
                    updateWasDone = UpdateEphemeris(cancellationToken, progressTracker.EphemerisProgress, forceUpdate: false);
                }, "Exception occurred when updating ephemeris data");
                if (updateWasDone)
                {
                    syncResult.EphemerisUpdateElapsed = value;
                }
                else
                {
                    syncResult.EphemerisCheckElapsed = value;
                }
            }
            if (syncTasks.HasFlag(SyncTasks.TimeZoneFile))
            {
                syncResult.TimeZoneElapsed = LogDoAndMeasure("Updating time zone file (if needed)...", delegate
                {
                    progressTracker.SetState(SyncState.TimeZoneData);
                    UpdateTimeZoneList(cancellationToken, progressTracker, forceUpdate: false, null);
                }, "Exception occurred when updating time zone data");
            }
            bool crashdumpDownloaded = false;
            if (syncTasks.HasFlag(SyncTasks.DeviceCrashDump))
            {
                syncResult.CrashDumpElapsed = LogDoAndMeasure("Syncing device crash dump...", delegate
                {
                    progressTracker.SetState(SyncState.DeviceCrashDump);
                    crashdumpDownloaded = GetCrashDumpFileFromDeviceAndPushToCloud(progressTracker.DeviceCrashDumpProgress, cancellationToken);
                }, "Exception occurred when getting crashDump file from device and pushing it to the cloud");
            }
            if (syncTasks.HasFlag(SyncTasks.DeviceInstrumentation))
            {
                LogDoAndMeasure("Syncing device instrumentation...", delegate
                {
                    progressTracker.SetState(SyncState.DeviceInstrumentation);
                    GetInstrumentationFileFromDeviceAndPushToCloud(progressTracker.DeviceInstrumentationProgress, cancellationToken, !crashdumpDownloaded);
                }, "Exception occurred when getting instrumentation file from device and pushing it to the cloud");
            }
            if (syncTasks.HasFlag(SyncTasks.UserProfile))
            {
                syncResult.UserProfileFullElapsed = func(delegate
                {
                    progressTracker.SetState(SyncState.UserProfile);
                    SyncUserProfile(cancellationToken);
                    progressTracker.UserProfileProgress.Complete();
                }, "Exception occurred in SyncUserProfile");
            }
            else if (syncTasks.HasFlag(SyncTasks.UserProfileFirmwareBytes))
            {
                syncResult.UserProfileFirmwareBytesElapsed = func(delegate
                {
                    progressTracker.SetState(SyncState.UserProfile);
                    SaveUserProfileFirmwareBytes(cancellationToken);
                    progressTracker.UserProfileProgress.Complete();
                }, "Exception occurred in SaveUserProfileFirmwareBytes");
            }
            if (syncTasks.HasFlag(SyncTasks.SensorLog))
            {
                doRethrows = true;
                syncResult.SensorLogElapsed = func(delegate
                {
                    progressTracker.SetState(SyncState.SensorLog);
                    SyncSensorLog(cancellationToken, progressTracker.LogSyncProgress).CopyToSyncResult(syncResult);
                }, "Exception occurred in SyncSensorLog()");
                doRethrows = false;
            }
            if (syncTasks.HasFlag(SyncTasks.WebTilesForced))
            {
                syncResult.WebTilesElapsed += func(delegate
                {
                    progressTracker.SetState(SyncState.WebTiles);
                    SyncWebTiles(forceSync: true, CancellationToken.None);
                    progressTracker.WebTilesProgress.Complete();
                }, "Exception occurred when syncing WebTiles");
            }
            else if (syncTasks.HasFlag(SyncTasks.WebTiles))
            {
                syncResult.WebTilesElapsed += func(delegate
                {
                    progressTracker.SetState(SyncState.WebTiles);
                    SyncWebTiles(forceSync: false, CancellationToken.None);
                    progressTracker.WebTilesProgress.Complete();
                }, "Exception occurred when syncing WebTiles");
            }
        }
        finally
        {
            stopwatch.Stop();
            progressTracker.SetState(SyncState.Done);
        }
        Logger.Log(LogLevel.Info, "Sync completed");
        syncResult.TotalTimeElapsed = stopwatch.ElapsedMilliseconds;
        return syncResult;
    }

    public Task<long> GetPendingLocalDataBytesAsync()
    {
        return Task.Run(() => GetPendingLocalDataBytes());
    }

    public long GetPendingLocalDataBytes()
    {
        CheckIfDisposed();
        long num = 0L;
        string[] files = storageProvider.GetFiles("PendingData");
        foreach (string text in files)
        {
            if (!text.EndsWith(".chunk.meta"))
            {
                string relativePath = Path.Combine(new string[2] { "PendingData", text });
                num += storageProvider.GetFileSize(relativePath);
            }
        }
        return num;
    }

    public Task<long> GetPendingDeviceDataBytesAsync()
    {
        return Task.Run(() => GetPendingDeviceDataBytes());
    }

    public long GetPendingDeviceDataBytes()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        long num = 0L;
        lock (loggerLock)
        {
            num = RemainingDeviceLogDataChunks();
            return num * 4096;
        }
    }

    public Task<IUserProfile> GetUserProfileFromDeviceAsync()
    {
        return Task.Run(() => GetUserProfileFromDevice());
    }

    public IUserProfile GetUserProfileFromDevice()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Obtaining the application profile from the band");
        int byteCount = UserProfile.GetAppDataSerializedByteCount(ConnectedAdminBandConstants);
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteInt32(byteCount);
        };
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoProfileGetDataApp, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck);
        UserProfile userProfile = UserProfile.DeserializeAppDataFromBand(cargoCommandReader, ConnectedAdminBandConstants);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        userProfile.DeviceSettings.DeviceId = DeviceUniqueId;
        userProfile.DeviceSettings.SerialNumber = SerialNumber;
        return userProfile;
    }

    public Task<IUserProfile> GetUserProfileAsync()
    {
        return Task.Run(() => GetUserProfile());
    }

    public IUserProfile GetUserProfile()
    {
        return GetUserProfile(CancellationToken.None);
    }

    public Task<IUserProfile> GetUserProfileAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => GetUserProfile(cancellationToken));
    }

    public IUserProfile GetUserProfile(CancellationToken cancellationToken)
    {
        CheckIfDisposed();
        if (cloudProvider == null)
        {
            ArgumentNullException ex = new ArgumentNullException("cloudProvider");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        return new UserProfile(cloudProvider.GetUserProfile(cancellationToken), ConnectedAdminBandConstants);
    }

    public Task SaveUserProfileAsync(IUserProfile profile, DateTimeOffset? updateTime = null)
    {
        return Task.Run(delegate
        {
            SaveUserProfile(profile, updateTime);
        });
    }

    public void SaveUserProfile(IUserProfile profile, DateTimeOffset? updateTime = null)
    {
        SaveUserProfile(profile, CancellationToken.None, updateTime);
    }

    public Task SaveUserProfileAsync(IUserProfile profile, CancellationToken cancellationToken, DateTimeOffset? updateTime = null)
    {
        return Task.Run(delegate
        {
            SaveUserProfile(profile, cancellationToken, updateTime);
        });
    }

    public void SaveUserProfile(IUserProfile profile, CancellationToken cancellationToken, DateTimeOffset? updateTimeN = null)
    {
        UserProfile userProfileImplementation = GetUserProfileImplementation(profile);
        CheckIfDisposed();
        if (cloudProvider == null)
        {
            ArgumentNullException ex = new ArgumentNullException("cloudProvider");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        SetUserProfileUpdateTime(userProfileImplementation, updateTimeN.HasValue ? updateTimeN.Value.ToUniversalTime() : ((DateTimeOffset)DateTime.UtcNow));
        try
        {
            CheckIfDisconnectedOrUpdateMode();
            GetDeviceMasteredUserProfileProperties(userProfileImplementation, forExplicitSave: true);
            ProfileSetAppData(userProfileImplementation);
        }
        catch
        {
            Logger.Log(LogLevel.Info, "SaveUserProfile -- Connection to Device Unavailable.  Saving to Cloud only.");
        }
        cloudProvider.SaveUserProfile(userProfileImplementation.ToCloudProfile(), createNew: false, cancellationToken);
    }

    public void SaveUserProfileToBandOnly(IUserProfile profile, DateTimeOffset? updateTimeN = null)
    {
        UserProfile userProfileImplementation = GetUserProfileImplementation(profile);
        CheckIfDisposed();
        SetUserProfileUpdateTime(userProfileImplementation, updateTimeN.HasValue ? updateTimeN.Value.ToUniversalTime() : ((DateTimeOffset)DateTime.UtcNow));
        try
        {
            CheckIfDisconnectedOrUpdateMode();
            ProfileSetAppData(userProfileImplementation);
        }
        catch
        {
            Logger.Log(LogLevel.Info, "SaveUserProfile -- Connection to Device Unavailable.");
        }
    }

    public Task SaveUserProfileToBandOnlyAsync(IUserProfile profile, DateTimeOffset? updateTimeN = null)
    {
        return Task.Run(delegate
        {
            SaveUserProfileToBandOnly(profile, updateTimeN);
        });
    }

    public Task SaveUserProfileFirmwareBytesAsync(CancellationToken cancellationToken)
    {
        return Task.Run(delegate
        {
            SaveUserProfileFirmwareBytes(cancellationToken);
        });
    }

    public void SaveUserProfileFirmwareBytes(CancellationToken cancellationToken)
    {
        if (cloudProvider == null)
        {
            ArgumentNullException ex = new ArgumentNullException("cloudProvider");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        CheckIfDisconnectedOrUpdateMode();
        byte[] firmwareBytes = ProfileGetFirmwareBytes();
        cloudProvider.SaveUserProfileFirmware(firmwareBytes, cancellationToken);
    }

    public Task ImportUserProfileAsync(CancellationToken cancellationToken)
    {
        return Task.Run(delegate
        {
            ImportUserProfile(cancellationToken);
        });
    }

    public void ImportUserProfile(CancellationToken cancellationToken)
    {
        UserProfile profile = (UserProfile)GetUserProfile(cancellationToken);
        ImportUserProfile(profile, cancellationToken);
    }

    public Task ImportUserProfileAsync(IUserProfile userProfile, CancellationToken cancellationToken)
    {
        return Task.Run(delegate
        {
            ImportUserProfile(userProfile, cancellationToken);
        });
    }

    public void ImportUserProfile(IUserProfile profile, CancellationToken cancellationToken)
    {
        UserProfile userProfileImplementation = GetUserProfileImplementation(profile);
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (cloudProvider == null)
        {
            ArgumentNullException ex = new ArgumentNullException("cloudProvider");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        SetUserProfileUpdateTime(userProfileImplementation, DateTimeOffset.UtcNow);
        cloudProvider.SaveUserProfile(userProfileImplementation.ToCloudProfile(), createNew: false, cancellationToken);
        GetDeviceMasteredUserProfileProperties(userProfileImplementation, forExplicitSave: true);
        ProfileSetAppData(userProfileImplementation);
        if (userProfileImplementation.DeviceSettings.FirmwareByteArray != null && userProfileImplementation.DeviceSettings.FirmwareByteArray.Length != 0)
        {
            ProfileSetFirmwareBytes(userProfileImplementation);
        }
    }

    public DeviceProfileStatus GetDeviceAndProfileLinkStatus(IUserProfile userProfile = null)
    {
        return GetDeviceAndProfileLinkStatus(CancellationToken.None, userProfile);
    }

    public Task<DeviceProfileStatus> GetDeviceAndProfileLinkStatusAsync(IUserProfile userProfile = null)
    {
        return Task.Run(() => GetDeviceAndProfileLinkStatus(CancellationToken.None, userProfile));
    }

    public Task<DeviceProfileStatus> GetDeviceAndProfileLinkStatusAsync(CancellationToken cancellationToken, IUserProfile userProfile = null)
    {
        return Task.Run(() => GetDeviceAndProfileLinkStatus(cancellationToken, userProfile));
    }

    public DeviceProfileStatus GetDeviceAndProfileLinkStatus(CancellationToken cancellationToken, IUserProfile profile = null)
    {
        UserProfile userProfile = null;
        if (profile != null)
        {
            userProfile = GetUserProfileImplementation(profile);
        }
        else if (cloudProvider == null)
        {
            ArgumentNullException ex = new ArgumentNullException("cloudProvider");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (userProfile == null)
        {
            userProfile = (UserProfile)GetUserProfile(cancellationToken);
        }
        return GetDeviceAndProfileLinkStatus(cancellationToken, userProfile.UserID, userProfile.ApplicationSettings.PairedDeviceId);
    }

    public Task<DeviceProfileStatus> GetDeviceAndProfileLinkStatusAsync(CancellationToken cancellationToken, Guid cloudUserId, Guid cloudDeviceId)
    {
        return Task.Run(() => GetDeviceAndProfileLinkStatus(cancellationToken, cloudUserId, cloudDeviceId));
    }

    private DeviceProfileStatus GetDeviceAndProfileLinkStatus(CancellationToken cancellationToken, Guid cloudUserId, Guid cloudDeviceId)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DeviceProfileStatus deviceProfileStatus = new DeviceProfileStatus();
        UserProfileHeader userProfileHeader = ProfileAppHeaderGet();
        if (cloudDeviceId == Guid.Empty)
        {
            deviceProfileStatus.UserLinkStatus = LinkStatus.Empty;
        }
        else if (cloudDeviceId == DeviceUniqueId)
        {
            deviceProfileStatus.UserLinkStatus = LinkStatus.Matching;
        }
        else
        {
            deviceProfileStatus.UserLinkStatus = LinkStatus.NonMatching;
        }
        if (userProfileHeader.UserID == Guid.Empty)
        {
            deviceProfileStatus.DeviceLinkStatus = LinkStatus.Empty;
        }
        else if (userProfileHeader.UserID == cloudUserId)
        {
            deviceProfileStatus.DeviceLinkStatus = LinkStatus.Matching;
        }
        else
        {
            deviceProfileStatus.DeviceLinkStatus = LinkStatus.NonMatching;
        }
        Logger.Log(LogLevel.Info, "Checking DeviceProfileLink: (UserDeviceID: {2} == DeviceID: {3}) is UserLinkStatus: {0} && (DeviceProfileID: {4} == UserProfileID: {5}) is DeviceLinkStatus: {1}", deviceProfileStatus.UserLinkStatus, deviceProfileStatus.DeviceLinkStatus, cloudDeviceId, DeviceUniqueId, userProfileHeader.UserID, cloudUserId);
        return deviceProfileStatus;
    }

    public Task LinkDeviceToProfileAsync(IUserProfile userProfile = null, bool importUserProfile = false)
    {
        return Task.Run(delegate
        {
            LinkDeviceToProfile(userProfile, importUserProfile);
        });
    }

    public void LinkDeviceToProfile(IUserProfile userProfile = null, bool importUserProfile = false)
    {
        LinkDeviceToProfile(CancellationToken.None, userProfile, importUserProfile);
    }

    public Task LinkDeviceToProfileAsync(CancellationToken cancellationToken, IUserProfile userProfile = null, bool importUserProfile = false)
    {
        return Task.Run(delegate
        {
            LinkDeviceToProfile(cancellationToken, userProfile, importUserProfile);
        });
    }

    public void LinkDeviceToProfile(CancellationToken cancellationToken, IUserProfile userProfile = null, bool importUserProfile = false)
    {
        SetDeviceProfileLink(setLink: true, cancellationToken, userProfile, importUserProfile);
    }

    public Task UnlinkDeviceFromProfileAsync(IUserProfile userProfile = null)
    {
        return Task.Run(delegate
        {
            UnlinkDeviceFromProfile(CancellationToken.None, userProfile);
        });
    }

    public void UnlinkDeviceFromProfile(IUserProfile userProfile = null)
    {
        SetDeviceProfileLink(setLink: false, CancellationToken.None, userProfile);
    }

    public Task UnlinkDeviceFromProfileAsync(CancellationToken cancellationToken, IUserProfile userProfile = null)
    {
        return Task.Run(delegate
        {
            UnlinkDeviceFromProfile(cancellationToken, userProfile);
        });
    }

    public void UnlinkDeviceFromProfile(CancellationToken cancellationToken, IUserProfile userProfile = null)
    {
        SetDeviceProfileLink(setLink: false, cancellationToken, userProfile);
    }

    internal void SetDeviceProfileLink(bool setLink, CancellationToken cancellationToken, IUserProfile profile = null, bool importUserProfile = false)
    {
        UserProfile userProfile = null;
        if (profile != null)
        {
            userProfile = GetUserProfileImplementation(profile);
        }
        CheckIfDisposed();
        if (cloudProvider == null)
        {
            ArgumentNullException ex = new ArgumentNullException("cloudProvider");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (userProfile == null)
        {
            userProfile = (UserProfile)GetUserProfile();
        }
        SetUserProfileUpdateTime(userProfile, DateTimeOffset.UtcNow);
        if (setLink)
        {
            userProfile.ApplicationSettings.PairedDeviceId = DeviceUniqueId;
            userProfile.DeviceSettings.DeviceId = DeviceUniqueId;
            userProfile.DeviceSettings.SerialNumber = SerialNumber;
        }
        else
        {
            userProfile.ApplicationSettings.PairedDeviceId = Guid.Empty;
            userProfile.DeviceSettings.DeviceId = Guid.Empty;
            userProfile.DeviceSettings.SerialNumber = null;
        }
        CloudProfileDeviceLink profile2 = userProfile.ToCloudProfileDeviceLink();
        cloudProvider.SaveDeviceLinkToUserProfile(profile2, cancellationToken);
        try
        {
            CheckIfDisconnectedOrUpdateMode();
            GetDeviceMasteredUserProfileProperties(userProfile, forExplicitSave: true);
            if (!setLink)
            {
                userProfile.UserID = Guid.Empty;
            }
            ProfileSetAppData(userProfile);
            if (setLink && importUserProfile && userProfile.DeviceSettings.FirmwareByteArray != null && userProfile.DeviceSettings.FirmwareByteArray.Length != 0)
            {
                ProfileSetFirmwareBytes(userProfile);
            }
        }
        catch
        {
            if (setLink)
            {
                throw;
            }
            Logger.Log(LogLevel.Info, "SetDeviceProfileLink -- Connection to Device Unavailable.  Unlinking Device on Cloud only.");
        }
    }

    public Task SyncUserProfileAsync(CancellationToken cancellationToken)
    {
        return Task.Run(delegate
        {
            SyncUserProfile(cancellationToken);
        });
    }

    public void SyncUserProfile(CancellationToken cancellationToken)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (cloudProvider == null)
        {
            ArgumentNullException ex = new ArgumentNullException("cloudProvider");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        bool flag = false;
        UserProfile userProfile = (UserProfile)GetUserProfile(cancellationToken);
        UserProfileHeader userProfileHeader = ProfileAppHeaderGet();
        flag = GetDeviceMasteredUserProfileProperties(userProfile, forExplicitSave: false);
        if (!userProfile.LastKDKSyncUpdateOn.HasValue || !userProfileHeader.LastKDKSyncUpdateOn.HasValue)
        {
            flag = true;
        }
        else if (userProfile.LastKDKSyncUpdateOn.Value > userProfileHeader.LastKDKSyncUpdateOn.Value)
        {
            flag = true;
        }
        else
        {
            _ = userProfile.LastKDKSyncUpdateOn.Value < userProfileHeader.LastKDKSyncUpdateOn.Value;
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (flag)
        {
            ProfileSetAppData(userProfile);
        }
        SaveUserProfileFirmwareBytes(cancellationToken);
    }

    private UserProfile GetUserProfileImplementation(IUserProfile profile)
    {
        if (profile == null)
        {
            throw new ArgumentNullException("profile");
        }
        return (profile as UserProfile) ?? throw new ArgumentException("Unexpected implementation", "profile");
    }

    private void SetUserProfileUpdateTime(UserProfile profile, DateTimeOffset updateTime)
    {
        profile.LastKDKSyncUpdateOn = new DateTime(updateTime.Year, updateTime.Month, updateTime.Day, updateTime.Hour, updateTime.Minute, updateTime.Second, DateTimeKind.Utc);
    }

    private bool GetDeviceMasteredUserProfileProperties(UserProfile profile, bool forExplicitSave)
    {
        int byteCount = UserProfile.GetAppDataSerializedByteCount(ConnectedAdminBandConstants);
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteInt32(byteCount);
        };
        Logger.Log(LogLevel.Info, "Obtaining the device mastered profile settings from the band");
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoProfileGetDataApp, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck);
        bool result = profile.DeserializeAndOverwriteDeviceMasteredAppDataFromBand(cargoCommandReader, ConnectedAdminBandConstants, forExplicitSave);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    private UserProfile GetDeviceMasteredUserProfileProperties()
    {
        int byteCount = UserProfile.GetAppDataSerializedByteCount(ConnectedAdminBandConstants);
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteInt32(byteCount);
        };
        Logger.Log(LogLevel.Info, "Obtaining the device mastered profile settings from the band");
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoProfileGetDataApp, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck);
        UserProfile result = UserProfile.DeserializeDeviceMasteredAppDataFromBand(cargoCommandReader, ConnectedAdminBandConstants);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    public Task<DateTime> GetDeviceUtcTimeAsync()
    {
        return Task.Run(() => GetDeviceUtcTime());
    }

    public DateTime GetDeviceUtcTime()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Verbose, "Getting Device UTC time");
        int serializedByteCount = CargoFileTime.GetSerializedByteCount();
        using CargoCommandReader reader = ProtocolBeginRead(DeviceCommands.CargoTimeGetUtcTime, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError);
        return CargoFileTime.DeserializeFromBandAsDateTime(reader);
    }

    public Task<DateTime> GetDeviceLocalTimeAsync()
    {
        return Task.Run(() => GetDeviceLocalTime());
    }

    public DateTime GetDeviceLocalTime()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Verbose, "Getting Device local time");
        int serializedByteCount = CargoSystemTime.GetSerializedByteCount();
        using ICargoReader reader = ProtocolBeginRead(DeviceCommands.CargoTimeGetLocalTime, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError);
        return CargoSystemTime.DeserializeFromBandAsDateTime(reader, DateTimeKind.Local);
    }

    public Task SetDeviceUtcTimeAsync()
    {
        return Task.Run(delegate
        {
            SetDeviceUtcTime();
        });
    }

    public void SetDeviceUtcTime()
    {
        SetDeviceUtcTime(DateTime.UtcNow);
    }

    public Task SetDeviceUtcTimeAsync(DateTime utc)
    {
        return Task.Run(delegate
        {
            SetDeviceUtcTime(utc);
        });
    }

    public void SetDeviceUtcTime(DateTime utc)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Verbose, "Setting Device UTC time");
        int serializedByteCount = CargoFileTime.GetSerializedByteCount();
        using CargoCommandWriter writer = ProtocolBeginWrite(DeviceCommands.CargoTimeSetUtcTime, serializedByteCount, CommandStatusHandling.DoNotThrow);
        CargoFileTime.SerializeToBandFromDateTime(writer, utc);
    }

    public Task<CargoTimeZoneInfo> GetDeviceTimeZoneAsync()
    {
        return Task.Run(() => GetDeviceTimeZone());
    }

    public CargoTimeZoneInfo GetDeviceTimeZone()
    {
        Logger.Log(LogLevel.Verbose, "Getting Device Time Zone");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        int serializedByteCount = CargoTimeZoneInfo.GetSerializedByteCount();
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoSystemSettingsGetTimeZone, serializedByteCount, CommandStatusHandling.DoNotCheck);
        CargoTimeZoneInfo result = CargoTimeZoneInfo.DeserializeFromBand(cargoCommandReader);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    public Task SetDeviceTimeZoneAsync(CargoTimeZoneInfo timeZone)
    {
        return Task.Run(delegate
        {
            SetDeviceTimeZone(timeZone);
        });
    }

    public void SetDeviceTimeZone(CargoTimeZoneInfo timeZone)
    {
        Logger.Log(LogLevel.Verbose, "Setting Device Time Zone");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        int serializedByteCount = CargoTimeZoneInfo.GetSerializedByteCount();
        using CargoCommandWriter writer = ProtocolBeginWrite(DeviceCommands.CargoSystemSettingsSetTimeZone, serializedByteCount, CommandStatusHandling.DoNotCheck);
        timeZone.SerializeToBand(writer);
    }

    public Task SetCurrentTimeAndTimeZoneAsync()
    {
        return Task.Run(delegate
        {
            SetCurrentTimeAndTimeZone(CancellationToken.None);
        });
    }

    public void SetCurrentTimeAndTimeZone()
    {
        SetCurrentTimeAndTimeZone(CancellationToken.None);
    }

    public Task SetCurrentTimeAndTimeZoneAsync(CancellationToken cancellationToken)
    {
        return Task.Run(delegate
        {
            SetCurrentTimeAndTimeZone(cancellationToken);
        });
    }

    public void SetCurrentTimeAndTimeZone(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        loggerProvider.Log(ProviderLogLevel.Info, "Syncing device UTC time (if allowed)...");
        bool flag = true;
        if (false)
        {
            DateTime deviceUtcTime = GetDeviceUtcTime();
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            flag = Math.Abs(DateTime.UtcNow.Subtract(deviceUtcTime).TotalSeconds) > 0.0;
        }
        if (flag)
        {
            SetDeviceUtcTime();
        }
        if (!cancellationToken.IsCancellationRequested)
        {
            loggerProvider.Log(ProviderLogLevel.Info, "Syncing device current time zone (if allowed)...");
            SetDeviceTimeZone(WindowsDateTime.GetWindowsCurrentTimeZone());
        }
    }

    public Task<bool> GetFirmwareBinariesValidationStatusAsync()
    {
        return Task.Run(() => GetFirmwareBinariesValidationStatus());
    }

    public bool GetFirmwareBinariesValidationStatus()
    {
        return GetFirmwareBinariesValidationStatusInternal();
    }

    private bool GetFirmwareBinariesValidationStatusInternal()
    {
        Logger.Log(LogLevel.Info, "Getting firmware binaries validation status");
        CheckIfDisposed();
        if (base.DeviceTransport == null)
        {
            InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        int num = CargoVersion.GetSerializedByteCount() * 3;
        bool flag = false;
        int bytesToRead = num + 4;
        using (CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoSRAMFWUpdateValidateAssets, bytesToRead, CommandStatusHandling.ThrowOnlySeverityError))
        {
            base.DeviceTransport.CargoStream.ReadTimeout = 20000;
            cargoCommandReader.ReadExactAndDiscard(num);
            flag = cargoCommandReader.ReadBool32();
        }
        Logger.Log(LogLevel.Info, "Firmware binaries validation status: {0}", flag ? "Valid" : "Invalid");
        return flag;
    }

    public Task<bool> GetDeviceOobeCompletedAsync()
    {
        return Task.Run(() => GetDeviceOobeCompleted());
    }

    public bool GetDeviceOobeCompleted()
    {
        CheckIfDisposed();
        if (base.DeviceTransport == null)
        {
            InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        Logger.Log(LogLevel.Verbose, "Getting OOBECompleted flag from device");
        bool completed = false;
        Action<ICargoReader> readData = delegate(ICargoReader r)
        {
            completed = r.ReadBool32();
        };
        ProtocolRead(DeviceCommands.CargoSystemSettingsOobeCompleteGet, 4, readData, 5000, CommandStatusHandling.ThrowAnyNonZero);
        return completed;
    }

    public Task<EphemerisCoverageDates> GetGpsEphemerisCoverageDatesFromDeviceAsync()
    {
        return Task.Run(() => GetGpsEphemerisCoverageDatesFromDevice());
    }

    public EphemerisCoverageDates GetGpsEphemerisCoverageDatesFromDevice()
    {
        CheckIfDisconnectedOrUpdateMode();
        EphemerisCoverageDates ephemerisCoverageDates = new EphemerisCoverageDates();
        try
        {
            int bytesToRead = CargoFileTime.GetSerializedByteCount() * 2;
            using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoGpsEphemerisCoverageDates, bytesToRead, CommandStatusHandling.DoNotCheck);
            ephemerisCoverageDates.StartDate = CargoFileTime.DeserializeFromBandAsDateTime(cargoCommandReader);
            ephemerisCoverageDates.EndDate = CargoFileTime.DeserializeFromBandAsDateTime(cargoCommandReader);
            if (cargoCommandReader.CommandStatus.Status != DeviceStatusCodes.Success)
            {
                ephemerisCoverageDates.StartDate = null;
                ephemerisCoverageDates.EndDate = null;
                Logger.Log(LogLevel.Info, "Ephemeris coverage dates were not found.  Proceeding as if no Ephemeris file on device");
                return ephemerisCoverageDates;
            }
            return ephemerisCoverageDates;
        }
        catch (Exception e)
        {
            Logger.LogException(LogLevel.Info, e, "Ephemeris coverage dates were not found.  Proceeding as if no Ephemeris file on device");
            ephemerisCoverageDates.StartDate = null;
            ephemerisCoverageDates.EndDate = null;
            return ephemerisCoverageDates;
        }
    }

    public Task<bool> UpdateGpsEphemerisDataAsync()
    {
        return Task.Run(() => UpdateEphemeris(CancellationToken.None, null, forceUpdate: false));
    }

    public bool UpdateGpsEphemerisData()
    {
        return UpdateEphemeris(CancellationToken.None, null, forceUpdate: false);
    }

    public Task<bool> UpdateGpsEphemerisDataAsync(CancellationToken cancellationToken, bool forceUpdate = false)
    {
        return Task.Run(() => UpdateEphemeris(cancellationToken, null, forceUpdate));
    }

    public bool UpdateGpsEphemerisData(CancellationToken cancellationToken, bool forceUpdate = false)
    {
        return UpdateEphemeris(cancellationToken, null, forceUpdate);
    }

    private bool UpdateEphemeris(CancellationToken cancellationToken, ProgressTrackerPrimitiveBase progress, bool forceUpdate)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        CheckIfStorageAvailable();
        if (cloudProvider == null)
        {
            throw new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (progress == null)
        {
            progress = new ProgressTrackerPrimitiveBase();
        }
        if (!forceUpdate && IsBandEphemerisFileGood())
        {
            progress.Complete();
            return false;
        }
        progress.AddStepsTotal(3);
        cancellationToken.ThrowIfCancellationRequested();
        VerifyLocalEphemerisFile(cancellationToken, progress, forceUpdate);
        progress.AddStepsCompleted(1);
        cancellationToken.ThrowIfCancellationRequested();
        string relativePath = Path.Combine(new string[2] { "Ephemeris", "EphemerisUpdate.bin" });
        using (Stream ephemerisData = storageProvider.OpenFileForRead(relativePath))
        {
            UpdateDeviceEphemerisData(ephemerisData, (int)storageProvider.GetFileSize(relativePath));
        }
        progress.Complete();
        return true;
    }

    private bool IsBandEphemerisFileGood()
    {
        EphemerisCoverageDates gpsEphemerisCoverageDatesFromDevice = GetGpsEphemerisCoverageDatesFromDevice();
        if (!gpsEphemerisCoverageDatesFromDevice.StartDate.HasValue || !gpsEphemerisCoverageDatesFromDevice.EndDate.HasValue || gpsEphemerisCoverageDatesFromDevice.EndDate <= gpsEphemerisCoverageDatesFromDevice.StartDate)
        {
            return false;
        }
        double num = (gpsEphemerisCoverageDatesFromDevice.EndDate.Value - gpsEphemerisCoverageDatesFromDevice.StartDate.Value).TotalSeconds * 0.5;
        return (DateTime.UtcNow - gpsEphemerisCoverageDatesFromDevice.StartDate.Value).TotalSeconds <= num;
    }

    private void VerifyLocalEphemerisFile(CancellationToken cancellationToken, ProgressTrackerPrimitiveBase progress, bool forceUpdate)
    {
        string text = string.Format("{0}.temp", new object[1] { "EphemerisVersion.json" });
        string text2 = string.Format("{0}.temp", new object[1] { "EphemerisUpdate.bin" });
        string relativePath = Path.Combine(new string[2] { "Ephemeris", "EphemerisVersion.json" });
        string relativePath2 = Path.Combine(new string[2] { "Ephemeris", "EphemerisUpdate.bin" });
        string text3 = Path.Combine(new string[2] { "Ephemeris", text });
        string text4 = Path.Combine(new string[2] { "Ephemeris", text2 });
        EphemerisCloudVersion ephemerisCloudVersion = null;
        bool flag = false;
        storageProvider.CreateFolder("Ephemeris");
        if (!forceUpdate && storageProvider.FileExists(relativePath))
        {
            try
            {
                using Stream inputStream = storageProvider.OpenFileForRead(relativePath, -1);
                ephemerisCloudVersion = DeserializeJson<EphemerisCloudVersion>(inputStream);
            }
            catch
            {
            }
            flag = storageProvider.FileExists(Path.Combine(new string[2] { "Ephemeris", "EphemerisUpdate.bin" }));
            if (flag && ephemerisCloudVersion != null && ephemerisCloudVersion.LastFileUpdatedTime.HasValue && ephemerisCloudVersion.LastFileUpdatedTime >= DateTime.UtcNow.AddHours(-12.0))
            {
                return;
            }
        }
        EphemerisCloudVersion ephemerisVersion = cloudProvider.GetEphemerisVersion(cancellationToken);
        progress.AddStepsCompleted(1);
        if (!forceUpdate && flag && ephemerisCloudVersion != null && ephemerisCloudVersion.EphemerisFileHeaderDataUrl == ephemerisVersion.EphemerisFileHeaderDataUrl && ephemerisCloudVersion.EphemerisProcessedFileDataUrl == ephemerisVersion.EphemerisProcessedFileDataUrl && ephemerisCloudVersion.LastFileUpdatedTime >= ephemerisVersion.LastFileUpdatedTime)
        {
            return;
        }
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using Stream outputStream = storageProvider.OpenFileForWrite(text3, append: false, 1024);
            SerializeJson(outputStream, ephemerisVersion);
        }
        catch (Exception innerException)
        {
            throw new BandException(CommonSR.EphemerisVersionDownloadError, innerException);
        }
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using Stream updateStream = storageProvider.OpenFileForWrite(text4, append: false);
            if (!cloudProvider.GetEphemeris(ephemerisVersion, updateStream, cancellationToken))
            {
                throw new BandException(CommonSR.EphemerisDownloadError);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (BandException)
        {
            throw;
        }
        catch (Exception innerException2)
        {
            throw new BandException(CommonSR.EphemerisDownloadError, innerException2);
        }
        cancellationToken.ThrowIfCancellationRequested();
        storageProvider.DeleteFile(relativePath);
        storageProvider.DeleteFile(relativePath2);
        try
        {
            storageProvider.RenameFile(text4, "Ephemeris", "EphemerisUpdate.bin");
            storageProvider.RenameFile(text3, "Ephemeris", "EphemerisVersion.json");
        }
        catch (Exception innerException3)
        {
            throw new BandException(CommonSR.EphemerisDownloadError, innerException3);
        }
    }

    public Task<uint> GetTimeZonesDataVersionFromDeviceAsync()
    {
        return Task.Run(() => GetTimeZonesDataVersionFromDevice());
    }

    public uint GetTimeZonesDataVersionFromDevice()
    {
        CheckIfDisconnectedOrUpdateMode();
        uint version = 0u;
        Action<ICargoReader> readData = delegate(ICargoReader r)
        {
            version = r.ReadUInt32();
        };
        try
        {
            ProtocolRead(DeviceCommands.CargoTimeZoneFileGetVersion, 4, readData);
        }
        catch (Exception e)
        {
            Logger.LogException(LogLevel.Info, e, "TimeZoneData Version was not found.  Proceeding as if no TimeZoneData file on device");
            version = 0u;
        }
        return version;
    }

    public Task<bool> UpdateTimeZoneListAsync(IUserProfile profile = null)
    {
        return Task.Run(() => UpdateTimeZoneList(CancellationToken.None, null, forceUpdate: false, profile));
    }

    public bool UpdateTimeZoneList(IUserProfile profile = null)
    {
        return UpdateTimeZoneList(CancellationToken.None, null, forceUpdate: false, profile);
    }

    public Task<bool> UpdateTimeZoneListAsync(CancellationToken cancellationToken, bool forceUpdate = false, IUserProfile profile = null)
    {
        return Task.Run(() => UpdateTimeZoneList(cancellationToken, null, forceUpdate, profile));
    }

    public bool UpdateTimeZoneList(CancellationToken cancellationToken, bool forceUpdate = false, IUserProfile profile = null)
    {
        return UpdateTimeZoneList(cancellationToken, null, forceUpdate, profile);
    }

    private bool UpdateTimeZoneList(CancellationToken cancellationToken, KdkSyncProgress progress, bool forceUpdate, IUserProfile profile)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        CheckIfStorageAvailable();
        if (cloudProvider == null)
        {
            InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        cancellationToken.ThrowIfCancellationRequested();
        if (progress != null)
        {
            progress.SetState(SyncState.TimeZoneData);
            progress.TimeZoneProgress.AddStepsTotal(3);
        }
        TimeZoneDataCloudVersion timeZoneDataCloudVersion = null;
        string relativePath = Path.Combine(new string[2] { "TimeZoneData", "TimeZoneData.json" });
        if (!forceUpdate && GetTimeZonesDataVersionFromDevice() == 0)
        {
            forceUpdate = true;
        }
        if (profile == null)
        {
            profile = GetDeviceMasteredUserProfileProperties();
        }
        if (!forceUpdate)
        {
            if (storageProvider.FileExists(relativePath))
            {
                try
                {
                    using Stream inputStream = storageProvider.OpenFileForRead(relativePath, -1);
                    timeZoneDataCloudVersion = DeserializeJson<TimeZoneDataCloudVersion>(inputStream);
                }
                catch (Exception e)
                {
                    Logger.LogException(LogLevel.Warning, e, "Exception occurred when reading the local timeZone version file");
                    timeZoneDataCloudVersion = null;
                }
            }
            if (timeZoneDataCloudVersion != null && timeZoneDataCloudVersion.LastModifiedDateTimeDevice.HasValue && timeZoneDataCloudVersion.LastCloudCheckDateTime.HasValue)
            {
                try
                {
                    TimeSpan timeSpan = DateTime.UtcNow - timeZoneDataCloudVersion.LastModifiedDateTimeDevice.Value;
                    TimeSpan timeSpan2 = DateTime.UtcNow - timeZoneDataCloudVersion.LastCloudCheckDateTime.Value;
                    if (timeZoneDataCloudVersion.Language != profile.DeviceSettings.LocaleSettings.Language)
                    {
                        Logger.Log(LogLevel.Info, "Time Zone Data language does not match band language. Forcing download and transfer to device.");
                        timeZoneDataCloudVersion = null;
                    }
                    else
                    {
                        if (Math.Abs(timeSpan2.Days) < 1)
                        {
                            Logger.Log(LogLevel.Info, "Time Zone Data downloaded within {0} day(s). No version or data downloads needed.", 1);
                            progress?.TimeZoneProgress.Complete();
                            return false;
                        }
                        if (Math.Abs(timeSpan.Days) > 60)
                        {
                            Logger.Log(LogLevel.Info, "Time Zone Data downloaded over {0} day(s) ago. Forcing download and transfer to device.", 60);
                            timeZoneDataCloudVersion = null;
                        }
                    }
                }
                catch
                {
                }
            }
        }
        TimeZoneDataCloudVersion timeZoneDataVersion = cloudProvider.GetTimeZoneDataVersion(profile, cancellationToken);
        if (timeZoneDataVersion == null)
        {
            BandCloudException ex2 = new BandCloudException(CommonSR.TimeZoneDataVersionDownloadError);
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        progress?.TimeZoneProgress.AddStepsCompleted(1);
        stopwatch.Stop();
        Logger.Log(LogLevel.Info, "Time to get Time Zone Data Version data: {0}", stopwatch.Elapsed);
        cancellationToken.ThrowIfCancellationRequested();
        if (timeZoneDataCloudVersion == null || timeZoneDataCloudVersion.LastModifiedDateTime != timeZoneDataVersion.LastModifiedDateTime)
        {
            stopwatch.Reset();
            stopwatch.Start();
            Stream stream = null;
            string relativePath2 = Path.Combine(new string[2] { "TimeZoneData", "TimeZoneUpdate.bin" });
            string text = Path.Combine(new string[2] { "TimeZoneData", "TimeZoneUpdateTemp.bin" });
            storageProvider.CreateFolder("TimeZoneData");
            if (storageProvider.FileExists(text))
            {
                storageProvider.DeleteFile(text);
            }
            try
            {
                stream = storageProvider.OpenFileForWrite(text, append: false);
            }
            catch (Exception innerException)
            {
                BandException ex3 = new BandException(string.Format(CommonSR.TimeZoneDownloadTempFileOpenError, new object[1] { text }), innerException);
                Logger.LogException(LogLevel.Error, ex3);
                throw ex3;
            }
            bool flag = false;
            using (stream)
            {
                flag = cloudProvider.GetTimeZoneData(timeZoneDataVersion, profile, stream);
            }
            int num = (int)storageProvider.GetFileSize(text);
            if (!flag || num == 0)
            {
                BandCloudException ex4 = new BandCloudException(CommonSR.TimeZoneDataDownloadError);
                Logger.LogException(LogLevel.Error, ex4);
                throw ex4;
            }
            storageProvider.RenameFile(text, "TimeZoneData", "TimeZoneUpdate.bin");
            stopwatch.Stop();
            Logger.Log(LogLevel.Info, "Time to get Time Zone Data: {0}", stopwatch.Elapsed);
            progress?.TimeZoneProgress.AddStepsCompleted(1);
            stopwatch.Reset();
            stopwatch.Start();
            cancellationToken.ThrowIfCancellationRequested();
            using (Stream timeZonesData = storageProvider.OpenFileForRead(relativePath2))
            {
                UpdateDeviceTimeZonesData(timeZonesData, num);
            }
            stopwatch.Stop();
            Logger.Log(LogLevel.Info, "Time to transfer Time Zone Data to device: {0}", stopwatch.Elapsed);
            timeZoneDataVersion.LastModifiedDateTimeDevice = DateTime.UtcNow;
            progress?.TimeZoneProgress.AddStepsCompleted(1);
        }
        else
        {
            progress.TimeZoneProgress.AddStepsCompleted(2);
            Logger.Log(LogLevel.Info, "Time Zone Data is recent enough and matches cloud. No data download needed.");
        }
        using (Stream outputStream = storageProvider.OpenFileForWrite(relativePath, append: false))
        {
            if (!timeZoneDataVersion.LastModifiedDateTimeDevice.HasValue && timeZoneDataCloudVersion != null && timeZoneDataCloudVersion.LastModifiedDateTimeDevice.HasValue)
            {
                timeZoneDataVersion.LastModifiedDateTimeDevice = timeZoneDataCloudVersion.LastModifiedDateTimeDevice;
            }
            timeZoneDataVersion.LastCloudCheckDateTime = DateTime.UtcNow;
            timeZoneDataVersion.Language = profile.DeviceSettings.LocaleSettings.Language;
            SerializeJson(outputStream, timeZoneDataVersion);
        }
        Logger.Log(LogLevel.Info, "TimeZone data version file saved locally");
        progress?.TimeZoneProgress.Complete();
        return true;
    }

    private bool GetCrashDumpFileFromDeviceAndPushToCloud(ProgressTrackerPrimitive progress, CancellationToken cancellationToken)
    {
        return GetFileFromDeviceAndPushToCloud(FileIndex.CrashDump, progress, cancellationToken, checkVersionFileBeforeDownload: false);
    }

    private bool GetInstrumentationFileFromDeviceAndPushToCloud(ProgressTrackerPrimitive progress, CancellationToken cancellationToken, bool doNeedToDownloadCheckBasedOnVersionFile)
    {
        return GetFileFromDeviceAndPushToCloud(FileIndex.Instrumentation, progress, cancellationToken, doNeedToDownloadCheckBasedOnVersionFile);
    }

    private bool GetFileFromDeviceAndPushToCloud(FileIndex fileIndex, ProgressTrackerPrimitive progress, CancellationToken cancellationToken, bool checkVersionFileBeforeDownload)
    {
        Logger.Log(LogLevel.Info, "Getting {0} file from device and pushing to cloud", fileIndex.ToString());
        if (fileIndex != FileIndex.CrashDump && fileIndex != FileIndex.Instrumentation)
        {
            ArgumentException ex = new ArgumentException(CommonSR.UnsupportedFileTypeToObtainFromDevice);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        progress.AddStepsTotal(2);
        bool flag = true;
        string text = fileIndex.ToString();
        if (storageProvider.DirectoryExists(text) && storageProvider.GetFiles(text).Length >= 20)
        {
            flag = false;
        }
        if (flag)
        {
            flag = GetFileFromDevice(fileIndex, checkVersionFileBeforeDownload);
        }
        progress.AddStepsCompleted(1);
        PushLocalFilesToCloud(fileIndex, cancellationToken);
        progress.Complete();
        return flag;
    }

    private DeviceFileSyncTimeInfo GetLocalDeviceFileSyncTimeInfo(string deviceFileSyncTimeInfoFileRelativePath)
    {
        DeviceFileSyncTimeInfo result = null;
        if (storageProvider.FileExists(deviceFileSyncTimeInfoFileRelativePath))
        {
            try
            {
                using Stream inputStream = storageProvider.OpenFileForRead(deviceFileSyncTimeInfoFileRelativePath, -1);
                result = DeserializeJson<DeviceFileSyncTimeInfo>(inputStream);
                return result;
            }
            catch (Exception e)
            {
                Logger.LogException(LogLevel.Warning, e, "Exception occurred when reading local deviceFileSync time info");
                return result;
            }
        }
        return result;
    }

    private bool NeedToDownloadFileFromDevice(FileIndex fileIndex, DeviceFileSyncTimeInfo localDeviceFileSyncTimeInfo)
    {
        if (localDeviceFileSyncTimeInfo != null)
        {
            TimeSpan zero = TimeSpan.Zero;
            if (fileIndex != FileIndex.Instrumentation)
            {
                ArgumentException ex = new ArgumentException(CommonSR.UnsupportedFileTypeToObtainFromDevice);
                Logger.LogException(LogLevel.Warning, ex);
                throw ex;
            }
            zero = TimeSpan.FromHours(168.0);
            DateTime? lastDeviceFileDownloadAttemptTime = localDeviceFileSyncTimeInfo.LastDeviceFileDownloadAttemptTime;
            if (lastDeviceFileDownloadAttemptTime.HasValue)
            {
                TimeSpan timeSpan = DateTime.UtcNow - lastDeviceFileDownloadAttemptTime.Value;
                bool flag = timeSpan >= zero;
                Logger.Log(LogLevel.Info, "Device file check {0}required; File Index: {1}, Minimum Time Between Checks: {2}, Last Checked: {3:MM/dd/yyyy HH:mm} ({4})", flag ? "" : "not ", fileIndex, zero, lastDeviceFileDownloadAttemptTime.Value.ToLocalTime(), timeSpan);
                return flag;
            }
        }
        Logger.Log(LogLevel.Info, "Device file check required; File Index: {0}, Last Checked: <unknown>", fileIndex);
        return true;
    }

    private void SaveDeviceFileSyncTimeInfoLocally(string localDeviceFileSyncTimeInfoFileRelativePath, DeviceFileSyncTimeInfo localDeviceFileSyncTimeInfo, FileIndex fileIndex)
    {
        if (fileIndex == FileIndex.Instrumentation)
        {
            if (localDeviceFileSyncTimeInfo == null)
            {
                localDeviceFileSyncTimeInfo = new DeviceFileSyncTimeInfo();
            }
            localDeviceFileSyncTimeInfo.LastDeviceFileDownloadAttemptTime = DateTime.UtcNow;
            storageProvider.CreateFolder(fileIndex.ToString());
            using (Stream outputStream = storageProvider.OpenFileForWrite(localDeviceFileSyncTimeInfoFileRelativePath, append: false, 512))
            {
                SerializeJson(outputStream, localDeviceFileSyncTimeInfo);
            }
            Logger.Log(LogLevel.Info, "Saved the deviceFileSyncTimeInfo data into local file");
            return;
        }
        ArgumentException ex = new ArgumentException(CommonSR.UnsupportedFileTypeToObtainFromDevice);
        Logger.LogException(LogLevel.Warning, ex);
        throw ex;
    }

    private bool GetFileFromDevice(FileIndex fileIndex, bool checkVersionFileBeforeDownload)
    {
        ushort num = 0;
        ushort num2 = 0;
        string text = fileIndex.ToString();
        switch (fileIndex)
        {
        case FileIndex.CrashDump:
            num = DeviceCommands.CargoCrashDumpGetFileSize;
            num2 = DeviceCommands.CargoCrashDumpGetAndDeleteFile;
            break;
        case FileIndex.Instrumentation:
            num = DeviceCommands.CargoInstrumentationGetFileSize;
            num2 = DeviceCommands.CargoInstrumentationGetFile;
            break;
        default:
        {
            ArgumentException ex = new ArgumentOutOfRangeException("fileIndex", CommonSR.UnsupportedFileTypeToObtainFromDevice);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        }
        string text2 = Path.Combine(new string[2] { text, "DeviceFileSyncTimeInfo.json" });
        DeviceFileSyncTimeInfo localDeviceFileSyncTimeInfo = null;
        if (checkVersionFileBeforeDownload)
        {
            localDeviceFileSyncTimeInfo = GetLocalDeviceFileSyncTimeInfo(text2);
            if (!NeedToDownloadFileFromDevice(fileIndex, localDeviceFileSyncTimeInfo))
            {
                return false;
            }
        }
        int num3 = (int)DeviceFileGetSize(num);
        if (num3 == 0)
        {
            Logger.Log(LogLevel.Info, "Device file check: File Index: {0}, Not Present", fileIndex);
        }
        else
        {
            Logger.Log(LogLevel.Info, "Device file download starting: File Index: {0}, Size: {1}", fileIndex, num3);
            storageProvider.CreateFolder(text);
            string format = string.Format("{{0}}-{{1:{0}}}.bin", new object[1] { "yyyyMMddHHmmssfff" });
            string relativePath = Path.Combine(new string[2]
            {
                text,
                string.Format(format, new object[2]
                {
                    fileIndex,
                    DateTime.UtcNow
                })
            });
            using (Stream stream = storageProvider.OpenFileForWrite(relativePath, append: false))
            {
                try
                {
                    using CargoCommandReader cargoCommandReader = ProtocolBeginRead(num2, num3, CommandStatusHandling.DoNotCheck);
                    while (cargoCommandReader.BytesRemaining > 0)
                    {
                        cargoCommandReader.CopyTo(stream, Math.Min(cargoCommandReader.BytesRemaining, 8192));
                    }
                    BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
                    stream.Flush();
                }
                catch (Exception innerException)
                {
                    Exception e = new BandException(string.Format(CommonSR.DeviceFileDownloadError, new object[1] { fileIndex }), innerException);
                    Logger.LogException(LogLevel.Error, e);
                    throw;
                }
            }
            Logger.Log(LogLevel.Info, "Device file download complete: File Index: {0}, Size: {1}", fileIndex, num3);
        }
        if (fileIndex == FileIndex.Instrumentation)
        {
            SaveDeviceFileSyncTimeInfoLocally(text2, localDeviceFileSyncTimeInfo, fileIndex);
        }
        return num3 > 0;
    }

    private void PushLocalFilesToCloud(FileIndex index, CancellationToken cancellationToken)
    {
        string text = index.ToString();
        if (!storageProvider.DirectoryExists(text))
        {
            return;
        }
        Logger.Log(LogLevel.Info, "Pushing local {0} files to the cloud", text);
        string[] files = storageProvider.GetFiles(text);
        foreach (string text2 in files)
        {
            if (!text2.Equals("DeviceFileSyncTimeInfo.json"))
            {
                string text3 = text + "\\" + text2;
                if (UploadFileToCloud(text3, index, cancellationToken))
                {
                    Logger.Log(LogLevel.Info, "Successfully uploaded file to cloud: {0}", text3);
                }
                else
                {
                    Logger.Log(LogLevel.Info, "File was already uploaded to cloud: {0}", text3);
                }
                storageProvider.DeleteFile(text3);
            }
        }
    }

    public Task<IFirmwareUpdateInfo> GetLatestAvailableFirmwareVersionAsync(List<KeyValuePair<string, string>> queryParams = null)
    {
        return Task.Run(() => GetLatestAvailableFirmwareVersion(queryParams));
    }

    public IFirmwareUpdateInfo GetLatestAvailableFirmwareVersion(List<KeyValuePair<string, string>> queryParams = null)
    {
        return GetLatestAvailableFirmwareVersion(CancellationToken.None, queryParams);
    }

    public Task<IFirmwareUpdateInfo> GetLatestAvailableFirmwareVersionAsync(CancellationToken cancellationToken, List<KeyValuePair<string, string>> queryParams = null)
    {
        return Task.Run(() => GetLatestAvailableFirmwareVersion(cancellationToken, queryParams));
    }

    public IFirmwareUpdateInfo GetLatestAvailableFirmwareVersion(CancellationToken cancellationToken, List<KeyValuePair<string, string>> queryParams = null)
    {
        Logger.Log(LogLevel.Info, "Getting latest available firmware version");
        CheckIfDisposed();
        if (cloudProvider == null)
        {
            InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (base.DeviceTransport == null)
        {
            InvalidOperationException ex2 = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        bool firmwareOnDeviceValid = false;
        if (DeviceTransportApp == RunningAppType.App)
        {
            firmwareOnDeviceValid = GetFirmwareBinariesValidationStatus();
        }
        return GetLatestAvailableFirmwareVersion(cancellationToken, FirmwareVersions, firmwareOnDeviceValid, queryParams);
    }

    internal IFirmwareUpdateInfo GetLatestAvailableFirmwareVersion(CancellationToken cancellationToken, FirmwareVersions deviceVersions, bool firmwareOnDeviceValid, List<KeyValuePair<string, string>> queryParams = null)
    {
        FirmwareUpdateInfo latestAvailableFirmwareVersion = cloudProvider.GetLatestAvailableFirmwareVersion(deviceVersions, firmwareOnDeviceValid, queryParams, cancellationToken);
        if (latestAvailableFirmwareVersion == null)
        {
            BandCloudException ex = new BandCloudException(CommonSR.FirmwareUpdateInfoError);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (latestAvailableFirmwareVersion.IsFirmwareUpdateAvailable)
        {
            Logger.Log(LogLevel.Info, "Firmware update is available: Version: {0}", latestAvailableFirmwareVersion.FirmwareVersion);
            try
            {
                int.Parse(latestAvailableFirmwareVersion.SizeInBytes);
                return latestAvailableFirmwareVersion;
            }
            catch
            {
                BandException ex2 = new BandException(CommonSR.InvalidUpdateDataSize);
                Logger.LogException(LogLevel.Error, ex2);
                throw ex2;
            }
        }
        return latestAvailableFirmwareVersion;
    }

    public Task<bool> UpdateFirmwareAsync(IFirmwareUpdateInfo updateInfo, IProgress<FirmwareUpdateProgress> progress = null)
    {
        return Task.Run(() => UpdateFirmware(updateInfo, CancellationToken.None, progress));
    }

    public Task<bool> UpdateFirmwareAsync(IFirmwareUpdateInfo updateInfo, CancellationToken cancellationToken, IProgress<FirmwareUpdateProgress> progress = null)
    {
        return Task.Run(() => UpdateFirmware(updateInfo, cancellationToken, progress));
    }

    public bool UpdateFirmware(IFirmwareUpdateInfo updateInfo, IProgress<FirmwareUpdateProgress> progress = null)
    {
        return UpdateFirmware(updateInfo, CancellationToken.None, progress);
    }

    public bool UpdateFirmware(IFirmwareUpdateInfo updateInfo, CancellationToken cancellationToken, IProgress<FirmwareUpdateProgress> progress = null)
    {
        if (updateInfo == null)
        {
            throw new ArgumentNullException("updateInfo");
        }
        if (!(updateInfo is FirmwareUpdateInfo updateInfo2))
        {
            throw new ArgumentException("Unexpected implementation", "updateInfo");
        }
        if (!updateInfo.IsFirmwareUpdateAvailable)
        {
            return false;
        }
        FirmwareUpdateOverallProgress firmwareUpdateOverallProgress = new FirmwareUpdateOverallProgress(progress, FirmwareUpdateOperation.DownloadAndUpdate);
        DownloadFirmwareUpdateInternal(updateInfo2, cancellationToken, firmwareUpdateOverallProgress);
        cancellationToken.ThrowIfCancellationRequested();
        bool result = PushFirmwareUpdateToDeviceInternal(updateInfo2, cancellationToken, firmwareUpdateOverallProgress);
        firmwareUpdateOverallProgress.SetState(FirmwareUpdateState.Done);
        return result;
    }

    public Task DownloadFirmwareUpdateAsync(IFirmwareUpdateInfo updateInfo)
    {
        return Task.Run(delegate
        {
            DownloadFirmwareUpdate(updateInfo, CancellationToken.None);
        });
    }

    public void DownloadFirmwareUpdate(IFirmwareUpdateInfo updateInfo)
    {
        DownloadFirmwareUpdate(updateInfo, CancellationToken.None);
    }

    public Task DownloadFirmwareUpdateAsync(IFirmwareUpdateInfo updateInfo, CancellationToken cancellationToken, IProgress<FirmwareUpdateProgress> progress = null)
    {
        return Task.Run(delegate
        {
            DownloadFirmwareUpdate(updateInfo, cancellationToken, progress);
        });
    }

    public void DownloadFirmwareUpdate(IFirmwareUpdateInfo updateInfo, CancellationToken cancellationToken, IProgress<FirmwareUpdateProgress> progress = null)
    {
        if (updateInfo == null)
        {
            throw new ArgumentNullException("updateInfo");
        }
        if (!(updateInfo is FirmwareUpdateInfo updateInfo2))
        {
            throw new ArgumentException("Unexpected implementation", "updateInfo");
        }
        CheckIfDisposed();
        CheckIfStorageAvailable();
        FirmwareUpdateOverallProgress firmwareUpdateOverallProgress = new FirmwareUpdateOverallProgress(progress, FirmwareUpdateOperation.DownloadOnly);
        DownloadFirmwareUpdateInternal(updateInfo2, cancellationToken, firmwareUpdateOverallProgress);
        firmwareUpdateOverallProgress.SetState(FirmwareUpdateState.Done);
    }

    public Task<bool> PushFirmwareUpdateToDeviceAsync(IFirmwareUpdateInfo updateInfo)
    {
        return Task.Run(() => PushFirmwareUpdateToDevice(updateInfo, CancellationToken.None));
    }

    public Task<bool> PushFirmwareUpdateToDeviceAsync(IFirmwareUpdateInfo updateInfo, CancellationToken cancellationToken, IProgress<FirmwareUpdateProgress> progress = null)
    {
        return Task.Run(() => PushFirmwareUpdateToDevice(updateInfo, cancellationToken, progress));
    }

    public bool PushFirmwareUpdateToDevice(IFirmwareUpdateInfo updateInfo)
    {
        return PushFirmwareUpdateToDevice(updateInfo, CancellationToken.None);
    }

    public bool PushFirmwareUpdateToDevice(IFirmwareUpdateInfo updateInfo, CancellationToken cancellationToken, IProgress<FirmwareUpdateProgress> progress = null)
    {
        if (updateInfo == null)
        {
            throw new ArgumentNullException("updateInfo");
        }
        if (!(updateInfo is FirmwareUpdateInfo updateInfo2))
        {
            throw new ArgumentException("Unexpected implementation", "updateInfo");
        }
        CheckIfDisposed();
        CheckIfStorageAvailable();
        FirmwareUpdateOverallProgress firmwareUpdateOverallProgress = new FirmwareUpdateOverallProgress(progress, FirmwareUpdateOperation.UpdateOnly);
        bool result = PushFirmwareUpdateToDeviceInternal(updateInfo2, cancellationToken, firmwareUpdateOverallProgress);
        firmwareUpdateOverallProgress.SetState(FirmwareUpdateState.Done);
        return result;
    }

    internal static JsonSerializerSettings GetJsonSerializerSettings()
    {
        return new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
    }

    internal static string SerializeJson(object value)
    {
        return JsonConvert.SerializeObject(value, GetJsonSerializerSettings());
    }

    internal static void SerializeJson(Stream outputStream, object value)
    {
        JsonSerializer jsonSerializer = JsonSerializer.Create(GetJsonSerializerSettings());
        using StreamWriter textWriter = new StreamWriter(outputStream, Encoding.UTF8, 128, leaveOpen: true);
        using JsonWriter jsonWriter = new JsonTextWriter(textWriter);
        jsonSerializer.Serialize(jsonWriter, value);
    }

    internal static T DeserializeJson<T>(Stream inputStream)
    {
        using StreamReader inputStream2 = new StreamReader(inputStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, 128, leaveOpen: true);
        return DeserializeJson<T>(inputStream2);
    }

    internal static T DeserializeJson<T>(string input)
    {
        using StringReader inputStream = new StringReader(input);
        return DeserializeJson<T>(inputStream);
    }

    internal static T DeserializeJson<T>(TextReader inputStream)
    {
        JsonSerializer jsonSerializer = JsonSerializer.Create(GetJsonSerializerSettings());
        using JsonReader reader = new JsonTextReader(inputStream);
        return jsonSerializer.Deserialize<T>(reader);
    }

    public Task UpdateLogProcessingAsync(List<LogProcessingStatus> fileInfoList, EventHandler<LogProcessingUpdatedEventArgs> notificationHandler, bool singleCallback, CancellationToken cancellationToken)
    {
        return Task.Run(delegate
        {
            UpdateLogProcessing(fileInfoList, notificationHandler, singleCallback, cancellationToken);
        });
    }

    public void UpdateLogProcessing(List<LogProcessingStatus> fileInfoList, EventHandler<LogProcessingUpdatedEventArgs> notificationHandler, bool singleCallback, CancellationToken cancellationToken)
    {
        if (fileInfoList == null)
        {
            throw new ArgumentNullException("fileInfoList");
        }
        Logger.Log(LogLevel.Info, "UpdateLogProcessing Called; Files: {0}", fileInfoList.Count);
        if (fileInfoList.Count == 0)
        {
            return;
        }
        TimeSpan timeSpan = TimeSpan.FromSeconds(2.0);
        TimeSpan timeout = TimeSpan.FromSeconds(4.0);
        LinkedList<LogProcessingStatus> linkedList = new LinkedList<LogProcessingStatus>();
        Dictionary<string, LogProcessingStatus> dictionary = new Dictionary<string, LogProcessingStatus>();
        List<LogProcessingStatus> list = new List<LogProcessingStatus>();
        List<LogProcessingStatus> list2 = new List<LogProcessingStatus>();
        int num = 0;
        bool flag = true;
        foreach (LogProcessingStatus item in fileInfoList.OrderBy((LogProcessingStatus lps) => lps.KnownStatus))
        {
            linkedList.AddLast(item);
            dictionary.Add(item.UploadId, item);
        }
        while (linkedList.Count > 0)
        {
            Dictionary<string, LogUploadStatusInfo> dictionary2 = null;
            int num2 = 0;
            int num3 = 0;
            int num4 = 0;
            int num5 = Math.Min(linkedList.Count, 25);
            DateTime knownStatus = linkedList.Take(25).Last().KnownStatus;
            DateTime utcNow = DateTime.UtcNow;
            TimeSpan timeSpan2 = utcNow - knownStatus;
            if (timeSpan2 < timeSpan)
            {
                cancellationToken.WaitAndThrowIfCancellationRequested(timeSpan - timeSpan2);
            }
            Logger.Log(LogLevel.Info, "Executing Upload Status Query; File IDs: {0}", num5);
            try
            {
                IEnumerable<string> uploadIDs = from lps in linkedList.Take(25)
                    select lps.UploadId;
                dictionary2 = cloudProvider.GetLogProcessingUpdate(uploadIDs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Logger.LogException(LogLevel.Error, e);
                if (++num >= 3)
                {
                    BandException ex2 = new BandException(CommonSR.LogProcessingStatusDownloadError);
                    Logger.Log(LogLevel.Error, "UpdateLogProcessing stopped due to repeated failures to downloaded logs.");
                    foreach (LogProcessingStatus item2 in linkedList)
                    {
                        Logger.Log(LogLevel.Error, "Log File: Upload Id: {0}, Status: Unknown (multiple attempts failed)", item2.UploadId);
                    }
                    throw ex2;
                }
                cancellationToken.WaitAndThrowIfCancellationRequested(timeout);
                continue;
            }
            if (num > 0)
            {
                num--;
            }
            utcNow = DateTime.UtcNow;
            for (int i = 0; i < num5; i++)
            {
                LogProcessingStatus value = linkedList.First.Value;
                linkedList.RemoveFirst();
                if (!dictionary2.TryGetValue(value.UploadId, out var value2))
                {
                    value.KnownStatus = utcNow;
                    linkedList.AddLast(value);
                    continue;
                }
                switch (value2.UploadStatus)
                {
                case LogUploadStatus.UploadPathSent:
                case LogUploadStatus.QueuedForETL:
                case LogUploadStatus.ActivitiesProcessingDone:
                case LogUploadStatus.EventsProcessingBlocked:
                    value.KnownStatus = utcNow;
                    linkedList.AddLast(value);
                    if (!dictionary.ContainsKey(value.UploadId))
                    {
                        dictionary.Add(value.UploadId, value);
                        flag = true;
                    }
                    num3++;
                    break;
                case LogUploadStatus.UploadDone:
                case LogUploadStatus.EventsProcessingDone:
                    list.Add(value);
                    flag = true;
                    dictionary.Remove(value.UploadId);
                    num2++;
                    break;
                default:
                    list2.Add(value);
                    flag = true;
                    num4++;
                    break;
                }
            }
            Logger.Log(LogLevel.Info, "Upload Status Query Result: Complete: {0}, Still Processing: {1}, Not Recognized: {2}", num2, num3, num4);
            if (notificationHandler != null && flag && !singleCallback)
            {
                DoLogCallback(notificationHandler, list, dictionary.Values, list2);
                flag = false;
            }
        }
        if (notificationHandler != null && flag)
        {
            DoLogCallback(notificationHandler, list, dictionary.Values, list2);
        }
    }

    private void DoLogCallback(EventHandler<LogProcessingUpdatedEventArgs> notificationHandler, IEnumerable<LogProcessingStatus> completedFiles, IEnumerable<LogProcessingStatus> processingFiles, IEnumerable<LogProcessingStatus> notRecognizedFiles)
    {
        try
        {
            Logger.Log(LogLevel.Info, "Callback for Log Updates; Complete: {0}, Still Processing: {1}, Not Recognized: {2}", completedFiles.Count(), processingFiles.Count(), notRecognizedFiles.Count());
            notificationHandler(this, new LogProcessingUpdatedEventArgs(completedFiles, processingFiles, notRecognizedFiles));
        }
        catch
        {
        }
    }

    public Task SetGoalsAsync(Goals goals)
    {
        return Task.Run(delegate
        {
            SetGoals(goals);
        });
    }

    public void SetGoals(Goals goals)
    {
        if (goals == null)
        {
            throw new ArgumentNullException("goals");
        }
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Setting goals on the device:");
        Logger.Log(LogLevel.Info, "CaloriesEnabled = {0}, CaloriesGoal = {1}", goals.CaloriesEnabled, goals.CaloriesGoal);
        Logger.Log(LogLevel.Info, "DistanceEnabled = {0}, DistanceGoal = {1}", goals.DistanceEnabled, goals.DistanceGoal);
        Logger.Log(LogLevel.Info, "StepsEnabled = {0}, StepsGoal = {1}", goals.StepsEnabled, goals.StepsGoal);
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoGoalTrackerSet, Goals.GetSerializedByteCount(ConnectedAdminBandConstants), CommandStatusHandling.DoNotCheck);
        goals.SerializeToBand(cargoCommandWriter, ConnectedAdminBandConstants);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
    }

    public Task SetWorkoutPlanAsync(Stream workoutPlansStream)
    {
        return Task.Run(delegate
        {
            SetWorkoutPlan(workoutPlansStream);
        });
    }

    public void SetWorkoutPlan(Stream workoutPlansStream)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (workoutPlansStream == null)
        {
            ArgumentNullException ex = new ArgumentNullException("workoutPlansStream");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        UploadFitnessPlan(workoutPlansStream, (int)workoutPlansStream.Length);
    }

    public Task SetWorkoutPlanAsync(byte[] workoutPlansData)
    {
        return Task.Run(delegate
        {
            SetWorkoutPlan(workoutPlansData);
        });
    }

    public void SetWorkoutPlan(byte[] workoutPlanData)
    {
        Logger.Log(LogLevel.Info, "Setting workout plan data on the device");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (workoutPlanData == null)
        {
            ArgumentNullException ex = new ArgumentNullException("workoutPlanData");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        using MemoryStream fitnessPlan = new MemoryStream(workoutPlanData, writable: false);
        UploadFitnessPlan(fitnessPlan, workoutPlanData.Length);
    }

    public Task<IList<WorkoutActivity>> GetWorkoutActivitiesAsync()
    {
        return Task.Run(() => GetWorkoutActivities());
    }

    internal IList<WorkoutActivity> GetWorkoutActivities()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        CheckIfNotEnvoy();
        int bytesToRead = WorkoutActivity.GetSerializedByteCount() * 15 + 4 + 10;
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoPersistedAppDataGetWorkoutActivities, bytesToRead, CommandStatusHandling.ThrowOnlySeverityError);
        List<WorkoutActivity> list = new List<WorkoutActivity>(15);
        for (int i = 0; i < 15; i++)
        {
            list.Add(WorkoutActivity.DeserializeFromBand(cargoCommandReader));
        }
        uint val = cargoCommandReader.ReadUInt32();
        cargoCommandReader.ReadExactAndDiscard(10);
        return list.Take((int)Math.Min(val, 15u)).ToList();
    }

    public Task SetWorkoutActivitiesAsync(IList<WorkoutActivity> activities)
    {
        return Task.Run(delegate
        {
            SetWorkoutActivities(activities);
        });
    }

    internal void SetWorkoutActivities(IList<WorkoutActivity> activities)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        CheckIfNotEnvoy();
        if (activities == null)
        {
            throw new ArgumentNullException("activities");
        }
        if (activities.Count == 0 || activities.Count > 15)
        {
            throw new ArgumentOutOfRangeException("activities");
        }
        if (activities.Contains(null))
        {
            throw new InvalidDataException();
        }
        int serializedByteCount = WorkoutActivity.GetSerializedByteCount();
        int dataSize = serializedByteCount * 15 + 4 + 10;
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoPersistedAppDataSetWorkoutActivities, dataSize, CommandStatusHandling.ThrowOnlySeverityError);
        int i;
        for (i = 0; i < activities.Count; i++)
        {
            activities[i].SerializeToBand(cargoCommandWriter);
        }
        for (; i < 15; i++)
        {
            cargoCommandWriter.WriteByte(0, serializedByteCount);
        }
        cargoCommandWriter.WriteUInt32((uint)activities.Count);
        cargoCommandWriter.WriteByte(0, 10);
    }

    public Task<int> GetGolfCourseMaxSizeAsync()
    {
        return Task.Run(() => GetGolfCourseMaxSize());
    }

    public int GetGolfCourseMaxSize()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoGolfCourseFileGetMaxSize, 4, CommandStatusHandling.ThrowAnyNonZero);
        return cargoCommandReader.ReadInt32();
    }

    public Task SetGolfCourseAsync(Stream golfCourseStream, int length = -1)
    {
        return Task.Run(delegate
        {
            SetGolfCourse(golfCourseStream, length);
        });
    }

    public void SetGolfCourse(Stream golfCourseStream, int length = -1)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (golfCourseStream == null)
        {
            throw new ArgumentNullException("golfCourseStream");
        }
        if (length < 0)
        {
            try
            {
                length = (int)(golfCourseStream.Length - golfCourseStream.Position);
            }
            catch (Exception innerException)
            {
                throw new Exception("Unable to discover stream length", innerException);
            }
        }
        Logger.Log(LogLevel.Info, "Setting golf course data on the device");
        UploadGolfCourse(golfCourseStream, length);
    }

    public Task SetGolfCourseAsync(byte[] golfCourseData)
    {
        return Task.Run(delegate
        {
            SetGolfCourse(golfCourseData);
        });
    }

    public void SetGolfCourse(byte[] golfCourseData)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (golfCourseData == null)
        {
            throw new ArgumentNullException("golfCourseData");
        }
        Logger.Log(LogLevel.Info, "Setting golf course data on the device");
        using MemoryStream golfCourse = new MemoryStream(golfCourseData, writable: false);
        UploadGolfCourse(golfCourse, golfCourseData.Length);
    }

    public Task NavigateToScreenAsync(CargoScreen screen)
    {
        return Task.Run(delegate
        {
            NavigateToScreen(screen);
        });
    }

    public void NavigateToScreen(CargoScreen screen)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Navigating to screen: {0}", screen.ToString());
        Action<ICargoWriter> writeData = delegate(ICargoWriter w)
        {
            w.WriteUInt16((ushort)screen);
        };
        ProtocolWriteWithData(DeviceCommands.CargoFireballUINavigateToScreen, 2, writeData);
    }

    public Task<OobeStage> GetOobeStageAsync()
    {
        return Task.Run(() => GetOobeStage());
    }

    public OobeStage GetOobeStage()
    {
        OobeStage stage = OobeStage.AskPhoneType;
        ProtocolRead(readData: delegate(ICargoReader r)
        {
            stage = (OobeStage)r.ReadUInt16();
        }, commandId: DeviceCommands.CargoOobeGetStage, dataSize: 2);
        return stage;
    }

    public Task SetOobeStageAsync(OobeStage stage)
    {
        return Task.Run(delegate
        {
            SetOobeStage(stage);
        });
    }

    public void SetOobeStage(OobeStage stage)
    {
        if ((int)stage >= 100)
        {
            throw new ArgumentOutOfRangeException("stage");
        }
        loggerProvider.Log(ProviderLogLevel.Info, "Setting OOBE Stage: {0}", stage);
        Action<ICargoWriter> writeData = delegate(ICargoWriter w)
        {
            w.WriteUInt16((ushort)stage);
        };
        ProtocolWriteWithData(DeviceCommands.CargoOobeSetStage, 2, writeData);
    }

    public Task FinalizeOobeAsync()
    {
        return Task.Run(delegate
        {
            FinalizeOobe();
        });
    }

    public void FinalizeOobe()
    {
        ProtocolWrite(DeviceCommands.CargoOobeFinalize);
    }

    public Task<string[]> GetPhoneCallResponsesAsync()
    {
        return Task.Run(() => GetPhoneCallResponses());
    }

    public string[] GetPhoneCallResponses()
    {
        Logger.Log(LogLevel.Info, "Getting phone call responses");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        string[] allResponses = GetAllResponses();
        string[] array = new string[4];
        Array.Copy(allResponses, 0, array, 0, 4);
        return array;
    }

    public Task SetPhoneCallResponsesAsync(string response1, string response2, string response3, string response4)
    {
        return Task.Run(delegate
        {
            SetPhoneCallResponses(response1, response2, response3, response4);
        });
    }

    public void SetPhoneCallResponses(string response1, string response2, string response3, string response4)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (response1 == null)
        {
            ArgumentNullException ex = new ArgumentNullException("response1");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (response2 == null)
        {
            ArgumentNullException ex2 = new ArgumentNullException("response2");
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        if (response3 == null)
        {
            ArgumentNullException ex3 = new ArgumentNullException("response3");
            Logger.LogException(LogLevel.Error, ex3);
            throw ex3;
        }
        if (response4 == null)
        {
            ArgumentNullException ex4 = new ArgumentNullException("response4");
            Logger.LogException(LogLevel.Error, ex4);
            throw ex4;
        }
        if (response1.Length > 160)
        {
            ArgumentException ex5 = new ArgumentException(string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2] { "response1", 160 }));
            Logger.LogException(LogLevel.Error, ex5);
            throw ex5;
        }
        if (response2.Length > 160)
        {
            ArgumentException ex6 = new ArgumentException(string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2] { "response2", 160 }));
            Logger.LogException(LogLevel.Error, ex6);
            throw ex6;
        }
        if (response3.Length > 160)
        {
            ArgumentException ex7 = new ArgumentException(string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2] { "response3", 160 }));
            Logger.LogException(LogLevel.Error, ex7);
            throw ex7;
        }
        if (response4.Length > 160)
        {
            ArgumentException ex8 = new ArgumentException(string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2] { "response4", 160 }));
            Logger.LogException(LogLevel.Error, ex8);
            throw ex8;
        }
        Logger.Log(LogLevel.Info, "Setting phone call responses - ");
        SetResponse(0, response1);
        SetResponse(1, response2);
        SetResponse(2, response3);
        SetResponse(3, response4);
    }

    public Task<string[]> GetSmsResponsesAsync()
    {
        return Task.Run(() => GetSmsResponses());
    }

    public string[] GetSmsResponses()
    {
        Logger.Log(LogLevel.Info, "Getting SMS responses");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        string[] allResponses = GetAllResponses();
        string[] array = new string[4];
        Array.Copy(allResponses, 4, array, 0, 4);
        return array;
    }

    public Task SetSmsResponsesAsync(string response1, string response2, string response3, string response4)
    {
        return Task.Run(delegate
        {
            SetSmsResponses(response1, response2, response3, response4);
        });
    }

    public void SetSmsResponses(string response1, string response2, string response3, string response4)
    {
        if (response1 == null)
        {
            ArgumentNullException ex = new ArgumentNullException("response1");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (response2 == null)
        {
            ArgumentNullException ex2 = new ArgumentNullException("response2");
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        if (response3 == null)
        {
            ArgumentNullException ex3 = new ArgumentNullException("response3");
            Logger.LogException(LogLevel.Error, ex3);
            throw ex3;
        }
        if (response4 == null)
        {
            ArgumentNullException ex4 = new ArgumentNullException("response4");
            Logger.LogException(LogLevel.Error, ex4);
            throw ex4;
        }
        if (response1.Length > 160)
        {
            Logger.Log(LogLevel.Warning, string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2] { "response1", 160 }));
            response1 = response1.Substring(0, 160);
        }
        if (response2.Length > 160)
        {
            Logger.Log(LogLevel.Warning, string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2] { "response2", 160 }));
            response2 = response2.Substring(0, 160);
        }
        if (response3.Length > 160)
        {
            Logger.Log(LogLevel.Warning, string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2] { "response3", 160 }));
            response3 = response3.Substring(0, 160);
        }
        if (response4.Length > 160)
        {
            Logger.Log(LogLevel.Warning, string.Format(CommonSR.ResponseStringExceedsMaxLength, new object[2] { "response4", 160 }));
            response4 = response4.Substring(0, 160);
        }
        Logger.Log(LogLevel.Info, "Setting SMS responses - ");
        SetResponse(4, response1);
        SetResponse(5, response2);
        SetResponse(6, response3);
        SetResponse(7, response4);
    }

    public Task<CargoRunDisplayMetrics> GetRunDisplayMetricsAsync()
    {
        return Task.Run(() => GetRunDisplayMetrics());
    }

    public CargoRunDisplayMetrics GetRunDisplayMetrics()
    {
        Logger.Log(LogLevel.Info, "Getting run display metrics");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        int serializedByteCount = CargoRunDisplayMetrics.GetSerializedByteCount(ConnectedAdminBandConstants);
        using CargoCommandReader reader = ProtocolBeginRead(DeviceCommands.CargoPersistedAppDataGetRunMetrics, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError);
        return CargoRunDisplayMetrics.DeserializeFromBand(reader, ConnectedAdminBandConstants);
    }

    public Task SetRunDisplayMetricsAsync(CargoRunDisplayMetrics cargoRunDisplayMetrics)
    {
        return Task.Run(delegate
        {
            SetRunDisplayMetrics(cargoRunDisplayMetrics);
        });
    }

    public void SetRunDisplayMetrics(CargoRunDisplayMetrics cargoRunDisplayMetrics)
    {
        Logger.Log(LogLevel.Info, "Setting run display metrics");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (cargoRunDisplayMetrics == null)
        {
            ArgumentNullException ex = new ArgumentNullException("cargoRunDisplayMetrics");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (!cargoRunDisplayMetrics.IsValid(ConnectedAdminBandConstants))
        {
            ArgumentException ex2 = new ArgumentException(CommonSR.InvalidCargoRunDisplayMetrics);
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        int serializedByteCount = CargoRunDisplayMetrics.GetSerializedByteCount(ConnectedAdminBandConstants);
        using CargoCommandWriter writer = ProtocolBeginWrite(DeviceCommands.CargoPersistedAppDataSetRunMetrics, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError);
        cargoRunDisplayMetrics.SerializeToBand(writer, ConnectedAdminBandConstants);
    }

    public Task<CargoBikeDisplayMetrics> GetBikeDisplayMetricsAsync()
    {
        return Task.Run(() => GetBikeDisplayMetrics());
    }

    public CargoBikeDisplayMetrics GetBikeDisplayMetrics()
    {
        Logger.Log(LogLevel.Info, "Getting bike display metrics");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        int serializedByteCount = CargoBikeDisplayMetrics.GetSerializedByteCount(ConnectedAdminBandConstants);
        using CargoCommandReader reader = ProtocolBeginRead(DeviceCommands.CargoPersistedAppDataGetBikeMetrics, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError);
        return CargoBikeDisplayMetrics.DeserializeFromBand(reader, ConnectedAdminBandConstants);
    }

    public Task SetBikeDisplayMetricsAsync(CargoBikeDisplayMetrics cargoBikeDisplayMetrics)
    {
        return Task.Run(delegate
        {
            SetBikeDisplayMetrics(cargoBikeDisplayMetrics);
        });
    }

    public void SetBikeDisplayMetrics(CargoBikeDisplayMetrics cargoBikeDisplayMetrics)
    {
        Logger.Log(LogLevel.Info, "Setting bike display metrics");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (cargoBikeDisplayMetrics == null)
        {
            ArgumentNullException ex = new ArgumentNullException("cargoBikeDisplayMetrics");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (!cargoBikeDisplayMetrics.IsValid(ConnectedAdminBandConstants))
        {
            ArgumentException ex2 = new ArgumentException(CommonSR.InvalidCargoBikeDisplayMetrics);
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        int serializedByteCount = CargoBikeDisplayMetrics.GetSerializedByteCount(ConnectedAdminBandConstants);
        using CargoCommandWriter writer = ProtocolBeginWrite(DeviceCommands.CargoPersistedAppDataSetBikeMetrics, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError);
        cargoBikeDisplayMetrics.SerializeToBand(writer, ConnectedAdminBandConstants);
    }

    public Task SetBikeSplitMultiplierAsync(int multiplier)
    {
        return Task.Run(delegate
        {
            SetBikeSplitMultiplier(multiplier);
        });
    }

    public void SetBikeSplitMultiplier(int multiplier)
    {
        Logger.Log(LogLevel.Info, "Setting bike split multiplier");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (multiplier < 1 || multiplier > 255)
        {
            throw new ArgumentOutOfRangeException("multiplier");
        }
        Action<ICargoWriter> writeData = delegate(ICargoWriter w)
        {
            w.WriteInt32(multiplier);
        };
        ProtocolWriteWithData(DeviceCommands.CargoPersistedAppDataSetBikeSplitMult, 4, writeData);
    }

    public Task<int> GetBikeSplitMultiplierAsync()
    {
        return Task.Run(() => GetBikeSplitMultiplier());
    }

    public int GetBikeSplitMultiplier()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Getting bike split multiplier");
        int bikeSplitMultiplier = 0;
        Action<ICargoReader> readData = delegate(ICargoReader r)
        {
            bikeSplitMultiplier = r.ReadInt32();
        };
        ProtocolRead(DeviceCommands.CargoPersistedAppDataGetBikeSplitMult, 4, readData);
        return bikeSplitMultiplier;
    }

    public Task<SleepNotification> GetSleepNotificationAsync()
    {
        return Task.Run(() => GetSleepNotification());
    }

    internal SleepNotification GetSleepNotification()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        CheckIfNotEnvoy();
        int serializedByteCount = SleepNotification.GetSerializedByteCount();
        using CargoCommandReader reader = ProtocolBeginRead(DeviceCommands.CargoPersistedAppDataGetSleepNotification, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError);
        return SleepNotification.DeserializeFromBand(reader);
    }

    public Task SetSleepNotificationAsync(SleepNotification notification)
    {
        return Task.Run(delegate
        {
            SetSleepNotification(notification);
        });
    }

    internal void SetSleepNotification(SleepNotification notification)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        CheckIfNotEnvoy();
        if (notification == null)
        {
            throw new ArgumentNullException("notification");
        }
        int serializedByteCount = SleepNotification.GetSerializedByteCount();
        using CargoCommandWriter writer = ProtocolBeginWrite(DeviceCommands.CargoPersistedAppDataSetSleepNotification, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError);
        notification.SerializeToBand(writer);
    }

    public Task DisableSleepNotificationAsync()
    {
        return Task.Run(delegate
        {
            DisableSleepNotification();
        });
    }

    internal void DisableSleepNotification()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        CheckIfNotEnvoy();
        ProtocolWrite(DeviceCommands.CargoPersistedAppDataDisableSleepNotification);
    }

    public Task<LightExposureNotification> GetLightExposureNotificationAsync()
    {
        return Task.Run(() => GetLightExposureNotification());
    }

    internal LightExposureNotification GetLightExposureNotification()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        CheckIfNotEnvoy();
        int serializedByteCount = LightExposureNotification.GetSerializedByteCount();
        using CargoCommandReader reader = ProtocolBeginRead(DeviceCommands.CargoPersistedAppDataGetLightExposureNotification, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError);
        return LightExposureNotification.DeserializeFromBand(reader);
    }

    public Task SetLightExposureNotificationAsync(LightExposureNotification notification)
    {
        return Task.Run(delegate
        {
            SetLightExposureNotification(notification);
        });
    }

    internal void SetLightExposureNotification(LightExposureNotification notification)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        CheckIfNotEnvoy();
        if (notification == null)
        {
            throw new ArgumentNullException("notification");
        }
        int serializedByteCount = LightExposureNotification.GetSerializedByteCount();
        using CargoCommandWriter writer = ProtocolBeginWrite(DeviceCommands.CargoPersistedAppDataSetLightExposureNotification, serializedByteCount, CommandStatusHandling.ThrowOnlySeverityError);
        notification.SerializeToBand(writer);
    }

    public Task DisableLightExposureNotificationAsync()
    {
        return Task.Run(delegate
        {
            DisableLightExposureNotification();
        });
    }

    internal void DisableLightExposureNotification()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        CheckIfNotEnvoy();
        ProtocolWrite(DeviceCommands.CargoPersistedAppDataDisableLightExposureNotification);
    }

    public Task<CargoRunStatistics> GetLastRunStatisticsAsync()
    {
        return Task.Run(() => GetLastRunStatistics());
    }

    public CargoRunStatistics GetLastRunStatistics()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Getting last run statistics");
        int serializedByteCount = CargoRunStatistics.GetSerializedByteCount();
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoPersistedStatisticsRunGet, serializedByteCount, CommandStatusHandling.DoNotCheck);
        CargoRunStatistics result = CargoRunStatistics.DeserializeFromBand(cargoCommandReader);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    public Task<CargoWorkoutStatistics> GetLastWorkoutStatisticsAsync()
    {
        return Task.Run(() => GetLastWorkoutStatistics());
    }

    public CargoWorkoutStatistics GetLastWorkoutStatistics()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Getting last workout statistics");
        int serializedByteCount = CargoWorkoutStatistics.GetSerializedByteCount();
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoPersistedStatisticsWorkoutGet, serializedByteCount, CommandStatusHandling.DoNotCheck);
        CargoWorkoutStatistics result = CargoWorkoutStatistics.DeserializeFromBand(cargoCommandReader);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    public Task<CargoSleepStatistics> GetLastSleepStatisticsAsync()
    {
        return Task.Run(() => GetLastSleepStatistics());
    }

    public CargoSleepStatistics GetLastSleepStatistics()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Getting last workout statistics");
        int serializedByteCount = CargoSleepStatistics.GetSerializedByteCount();
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoPersistedStatisticsSleepGet, serializedByteCount, CommandStatusHandling.DoNotCheck);
        CargoSleepStatistics result = CargoSleepStatistics.DeserializeFromBand(cargoCommandReader);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    public Task<CargoGuidedWorkoutStatistics> GetLastGuidedWorkoutStatisticsAsync()
    {
        return Task.Run(() => GetLastGuidedWorkoutStatistics());
    }

    public CargoGuidedWorkoutStatistics GetLastGuidedWorkoutStatistics()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Getting last guided workout statistics");
        int serializedByteCount = CargoGuidedWorkoutStatistics.GetSerializedByteCount();
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoPersistedStatisticsGuidedWorkoutGet, serializedByteCount, CommandStatusHandling.DoNotCheck);
        CargoGuidedWorkoutStatistics result = CargoGuidedWorkoutStatistics.DeserializeFromBand(cargoCommandReader);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    public Task SensorSubscribeAsync(SensorType subscriptionType)
    {
        return Task.Run(delegate
        {
            SensorSubscribe(subscriptionType);
        });
    }

    public void SensorSubscribe(SensorType subscriptionType)
    {
        CheckIfDisposed();
        if (subscriptionType == SensorType.LogEntry)
        {
            throw new ArgumentOutOfRangeException("subscriptionType");
        }
        lock (base.StreamingLock)
        {
            if (!IsSensorSubscribed(subscriptionType))
            {
                Logger.Log(LogLevel.Info, "Subscribing to the sensor module: {0}", subscriptionType);
                bool flag = false;
                Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
                {
                    w.WriteByte((byte)subscriptionType);
                    w.WriteBool32(b: false);
                };
                ProtocolWriteWithArgs(DeviceCommands.CargoRemoteSubscriptionSubscribe, 5, writeArgBuf);
                lock (base.SubscribedSensorTypes)
                {
                    flag = base.SubscribedSensorTypes.Count == 0;
                    base.SubscribedSensorTypes.Add((byte)subscriptionType);
                }
                if (flag)
                {
                    StartOrAwakeStreamingSubscriptionTasks();
                }
            }
        }
    }

    public Task SensorUnsubscribeAsync(SensorType subscriptionType)
    {
        return Task.Run(delegate
        {
            SensorUnsubscribe(subscriptionType);
        });
    }

    public void SensorUnsubscribe(SensorType subscriptionType)
    {
        CheckIfDisposed();
        lock (base.StreamingLock)
        {
            if (IsSensorSubscribed(subscriptionType))
            {
                Logger.Log(LogLevel.Info, "Unsubscribing from the sensor module: {0}", subscriptionType.ToString());
                Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
                {
                    w.WriteByte((byte)subscriptionType);
                };
                bool flag = false;
                ProtocolWriteWithArgs(DeviceCommands.CargoRemoteSubscriptionUnsubscribe, 1, writeArgBuf);
                lock (base.SubscribedSensorTypes)
                {
                    base.SubscribedSensorTypes.Remove((byte)subscriptionType);
                    flag = base.SubscribedSensorTypes.Count == 0;
                }
                if (flag)
                {
                    StopStreamingSubscriptionTasks();
                }
            }
        }
    }

    private bool IsSensorSubscribed(SensorType subscriptionType)
    {
        return base.SubscribedSensorTypes.Contains((byte)subscriptionType);
    }

    protected override void StartOrAwakeStreamingSubscriptionTasks()
    {
        if (base.StreamingTask == null)
        {
            base.StreamingTaskCancel = new CancellationTokenSource();
            Logger.Log(LogLevel.Info, "Starting the streaming task...");
            base.StreamingTask = Task.Run(delegate
            {
                StreamBandData(null, base.StreamingTaskCancel.Token);
            });
        }
    }

    protected override void StopStreamingSubscriptionTasks()
    {
        if (base.StreamingTask != null)
        {
            Logger.Log(LogLevel.Info, "Signaling the streaming task to stop...");
            base.StreamingTaskCancel.Cancel();
            base.StreamingTask.Wait();
            base.StreamingTaskCancel.Dispose();
            base.StreamingTaskCancel = null;
            base.StreamingTask = null;
            Logger.Log(LogLevel.Info, "Streaming task has stopped");
        }
    }

    protected override void OnDisconnected(TransportDisconnectedEventArgs args)
    {
        if (args.Reason == TransportDisconnectedReason.TransportIssue)
        {
            this.Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public void CloseSession()
    {
        Dispose();
    }

    public Task SyncWebTilesAsync(bool forceSync, CancellationToken cancellationToken)
    {
        return Task.Run(delegate
        {
            SyncWebTiles(forceSync, cancellationToken);
        });
    }

    private void SyncWebTiles(bool forceSync, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartStrip startStripNoImages = GetStartStripNoImages();
        IList<Guid> installedWebTileIds = WebTileManagerFactory.Instance.GetInstalledWebTileIds();
        for (int i = 0; i < installedWebTileIds.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!startStripNoImages.Contains(installedWebTileIds[i]))
            {
                WebTileManagerFactory.Instance.UninstallWebTileAsync(installedWebTileIds[i]).Wait();
                installedWebTileIds.RemoveAt(i);
                i--;
            }
        }
        for (int j = 0; j < startStripNoImages.Count; j++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AdminBandTile adminBandTile = startStripNoImages[j];
            if (!adminBandTile.IsWebTile)
            {
                continue;
            }
            try
            {
                if (!SyncWebTile(startStripNoImages, adminBandTile, installedWebTileIds, forceSync))
                {
                    j--;
                }
            }
            catch (Exception e)
            {
                loggerProvider.LogException(ProviderLogLevel.Error, e, "Error syncing Webtile Name: {0}, Id: {1}.", adminBandTile.Name, adminBandTile.TileId);
            }
        }
    }

    public Task SyncWebTileAsync(Guid tileId, CancellationToken cancellationToken)
    {
        return Task.Run(delegate
        {
            SyncWebTile(tileId, cancellationToken);
        });
    }

    private void SyncWebTile(Guid tileId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartStrip startStripNoImages = GetStartStripNoImages();
        IList<Guid> installedWebTileIds = WebTileManagerFactory.Instance.GetInstalledWebTileIds();
        cancellationToken.ThrowIfCancellationRequested();
        int num = startStripNoImages.IndexOf(tileId);
        if (num >= 0)
        {
            AdminBandTile adminBandTile = startStripNoImages[num];
            if (adminBandTile.IsWebTile)
            {
                SyncWebTile(startStripNoImages, adminBandTile, installedWebTileIds, forceSync: true);
            }
        }
    }

    private bool SyncWebTile(StartStrip startStrip, AdminBandTile webTile, IList<Guid> installedWebTiles, bool forceSync)
    {
        IWebTile webTile2 = null;
        if (installedWebTiles.Contains(webTile.Id))
        {
            webTile2 = WebTileManagerFactory.Instance.GetWebTile(webTile.Id);
        }
        if (webTile2 != null)
        {
            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            if (forceSync || webTile2.HasRefreshIntervalElapsed(utcNow))
            {
                NotificationDialog notificationDialog = null;
                bool clearPages;
                bool sendAsMessage;
                List<PageData> pages = webTile2.Refresh(out clearPages, out sendAsMessage, out notificationDialog);
                SendPagesToBand(webTile2.TileId, pages, clearPages, sendAsMessage);
                webTile2.SaveLastSync(utcNow);
                if (notificationDialog != null)
                {
                    Guid tileId = webTile2.TileId;
                    BandNotificationFlags flagbits = BandNotificationFlags.ForceNotificationDialog;
                    ShowDialogHelper(tileId, notificationDialog.Title ?? "", notificationDialog.Body ?? "", CancellationToken.None, flagbits);
                }
            }
            return true;
        }
        startStrip.Remove(webTile);
        SetStartStrip(startStrip);
        return false;
    }

    private void SendPagesToBand(Guid tileId, List<PageData> pages, bool clearPages, bool sendAsMessage)
    {
        if (clearPages)
        {
            RemovePages(tileId, CancellationToken.None);
        }
        if (pages == null)
        {
            return;
        }
        if (sendAsMessage)
        {
            foreach (PageData page in pages)
            {
                string elementTextData = GetElementTextData(page, 1);
                string elementTextData2 = GetElementTextData(page, 2);
                DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                if (!string.IsNullOrWhiteSpace(elementTextData) && !string.IsNullOrWhiteSpace(elementTextData2))
                {
                    SendMessage(tileId, elementTextData, elementTextData2, utcNow, MessageFlags.None, CancellationToken.None);
                }
            }
            return;
        }
        SetPages(tileId, CancellationToken.None, pages);
    }

    private string GetElementTextData(PageData pageData, int elementId)
    {
        if (pageData.Values.FirstOrDefault((PageElementData d) => d.ElementId == elementId) is TextBlockData textBlockData && textBlockData.Text != null)
        {
            return textBlockData.Text.Trim();
        }
        return string.Empty;
    }

    public void EnableRetailDemoMode()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Activating retail demo mode.");
        ProtocolWrite(DeviceCommands.CargoSystemSettingsEnableDemoMode);
    }

    public Task EnableRetailDemoModeAsync()
    {
        return Task.Run(delegate
        {
            EnableRetailDemoMode();
        });
    }

    public void DisableRetailDemoMode()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Shutting down retail demo mode.");
        ProtocolWrite(DeviceCommands.CargoSystemSettingsDisableDemoMode);
    }

    public Task DisableRetailDemoModeAsync()
    {
        return Task.Run(delegate
        {
            DisableRetailDemoMode();
        });
    }

    public void CargoSystemSettingsFactoryReset()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Factory Resetting Band.");
        int num = 128;
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteByte(0);
        };
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoSystemSettingsFactoryReset, 1, num, writeArgBuf, CommandStatusHandling.ThrowOnlySeverityError);
        cargoCommandReader.ReadExactAndDiscard(num);
    }

    public Task CargoSystemSettingsFactoryResetAsync()
    {
        return Task.Run(delegate
        {
            CargoSystemSettingsFactoryReset();
        });
    }

    public Task GenerateSensorLogAsync(TimeSpan duration)
    {
        return Task.Run(delegate
        {
            GenerateSensorLog(duration);
        });
    }

    public void GenerateSensorLog(TimeSpan duration)
    {
        LoggerEnable();
        LoggerSubscribe(SensorType.AccelGyro_2_4_MS_16G);
        platformProvider.Sleep((int)duration.TotalMilliseconds);
        LoggerUnsubscribe(SensorType.AccelGyro_2_4_MS_16G);
    }

    internal void LoggerSubscribe(SensorType subscriptionType)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Subscribing to subscription logger");
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteByte((byte)subscriptionType);
            w.WriteBool32(b: false);
        };
        ProtocolWriteWithArgs(DeviceCommands.CargoSubscriptionLoggerSubscribe, 5, writeArgBuf);
    }

    internal void LoggerUnsubscribe(SensorType subscriptionType)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Unsubscribing from subscription logger");
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteByte((byte)subscriptionType);
        };
        ProtocolWriteWithArgs(DeviceCommands.CargoSubscriptionLoggerUnsubscribe, 1, writeArgBuf);
    }

    public Task LoggerEnableAsync()
    {
        return Task.Run(delegate
        {
            LoggerEnable();
        });
    }

    public void LoggerEnable()
    {
        Logger.Log(LogLevel.Info, "Enabling cargo logger");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        ProtocolWrite(DeviceCommands.CargoLoggerEnableLogging);
    }

    public Task LoggerDisableAsync()
    {
        return Task.Run(delegate
        {
            LoggerDisable();
        });
    }

    public void LoggerDisable()
    {
        Logger.Log(LogLevel.Info, "Disabling cargo logger");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        ProtocolWrite(DeviceCommands.CargoLoggerDisableLogging);
    }

    internal void ClearCache()
    {
        CheckIfDisposed();
        storageProvider.DeleteFolder("Ephemeris");
        storageProvider.DeleteFolder("TimeZoneData");
        storageProvider.DeleteFolder("FirmwareUpdate");
    }

    public void StartCortana()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        ProtocolWrite(DeviceCommands.CargoCortanaStart);
    }

    public void StopCortana()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        ProtocolWrite(DeviceCommands.CargoCortanaStop);
    }

    public void CancelCortana()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        ProtocolWrite(DeviceCommands.CargoCortanaCancel);
    }

    public void SendCortanaNotification(CortanaStatus status, string message)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (!Enum.IsDefined(typeof(CortanaStatus), status))
        {
            throw new ArgumentException("Invalid status parameter", "status");
        }
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }
        if (message.Length > 160)
        {
            throw new ArgumentOutOfRangeException("message", "message length exceeded.");
        }
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoCortanaNotification, 326, CommandStatusHandling.DoNotCheck);
        cargoCommandWriter.WriteUInt16((ushort)status);
        cargoCommandWriter.WriteUInt16(320);
        cargoCommandWriter.WriteByte(0);
        cargoCommandWriter.WriteByte(0);
        cargoCommandWriter.WriteStringWithPadding(message, 160);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
    }

    public Task<byte[]> EFlashReadAsync(uint address, uint numBytesToRead)
    {
        Logger.Log(LogLevel.Verbose, "[CargoClient.EFlashReadAsync()] Invoked");
        return Task.Run(() => EFlashRead(address, numBytesToRead));
    }

    public byte[] EFlashRead(uint address, uint numBytesToRead)
    {
        Logger.Log(LogLevel.Verbose, "[CargoClient.EFlashRead()] Invoked");
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteUInt32(address);
            w.WriteUInt32(numBytesToRead);
        };
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoEFlashRead, 8, (int)numBytesToRead, writeArgBuf, CommandStatusHandling.DoNotCheck);
        byte[] result = cargoCommandReader.ReadExact((int)numBytesToRead);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    public Task<byte[]> LogChunkReadAsync(uint address, uint numBytesToRead)
    {
        Logger.Log(LogLevel.Verbose, "[CargoClient.LogChunkReadAsync()] Invoked");
        return Task.Run(() => LogChunkRead(address, numBytesToRead));
    }

    public byte[] LogChunkRead(uint address, uint numBytesToRead)
    {
        Logger.Log(LogLevel.Verbose, "[CargoClient.LogChunkRead()] Invoked");
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoLoggerGetChunkData, (int)numBytesToRead, CommandStatusHandling.DoNotCheck);
        byte[] result = cargoCommandReader.ReadExact((int)numBytesToRead);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    public Task CallBadDeviceCommandAsync()
    {
        return Task.Run(delegate
        {
            CallBadDeviceCommand();
        });
    }

    public void CallBadDeviceCommand()
    {
        ProtocolWrite(1);
    }

    internal Task OobeCompleteClearAsync()
    {
        return Task.Run(delegate
        {
            OobeCompleteClear();
        });
    }

    internal void OobeCompleteClear()
    {
        ProtocolWrite(DeviceCommands.CargoSystemSettingsOobeCompleteClear);
    }

    internal Task OobeCompleteSetAsync()
    {
        return Task.Run(delegate
        {
            OobeCompleteSet();
        });
    }

    internal void OobeCompleteSet()
    {
        ProtocolWrite(DeviceCommands.CargoSystemSettingsOobeCompleteSet);
    }

    internal CargoClient(IDeviceTransport transport, CloudProvider cloudProvider, ILoggerProvider loggerProvider, IPlatformProvider platformProvider, IApplicationPlatformProvider applicationPlatformProvider)
        : base(transport, loggerProvider, applicationPlatformProvider)
    {
        this.cloudProvider = cloudProvider;
        this.platformProvider = platformProvider;
        loggerLock = new object();
        runningFirmwareApp = FirmwareApp.Invalid;
        Logger.Log(LogLevel.Verbose, "[CargoClient.CargoClient()] Object constructed");
    }

    internal void InitializeStorageProvider(IStorageProvider storageProvider)
    {
        if (storageProvider == null)
        {
            throw new ArgumentNullException("storageProvider");
        }
        this.storageProvider = storageProvider;
        this.storageProvider.CreateFolder("PendingData");
    }

    public Task<ushort> GetLogVersionAsync()
    {
        return Task.Run(() => GetLogVersion());
    }

    public ushort GetLogVersion()
    {
        CheckIfDisconnectedOrUpdateMode();
        Logger.Log(LogLevel.Info, "Invoking ProtocolReadStruct on KDevice for command: CargoCoreModuleGetLogVersion");
        ushort result = 0;
        Action<ICargoReader> readData = delegate(ICargoReader r)
        {
            result = r.ReadUInt16();
        };
        ProtocolRead(DeviceCommands.CargoCoreModuleGetLogVersion, 2, readData);
        return result;
    }

    private void PopulateUploadMetadata(UploadMetaData metadata)
    {
        metadata.UTCTimeZoneOffsetInMinutes = (int)DateTimeOffset.Now.Offset.TotalMinutes;
        metadata.HostOSVersion = platformProvider.GetHostOSVersion().ToString();
        metadata.HostAppVersion = platformProvider.GetAssemblyVersion();
        metadata.HostOS = platformProvider.GetHostOS();
    }

    private void LoggerFlush(CancellationToken cancel, uint maxBusyRetries = 4u)
    {
        uint num = 0u;
        while (true)
        {
            CargoStatus status = ProtocolWrite(DeviceCommands.CargoLoggerFlush, 5000, swallowStatusReadException: false, CommandStatusHandling.DoNotCheck);
            if (status.Status == DeviceStatusCodes.Success)
            {
                break;
            }
            if (status.Status == DeviceStatusCodes.DataLoggerBusy)
            {
                bool flag = num < maxBusyRetries;
                Logger.Log(LogLevel.Warning, "LoggerFlush(): Device error DeviceStatusCodes.DataLoggerBusy (0x{0:X}); attempt {1}, {2}", status.Status, num + 1, flag ? "retrying..." : "giving up");
                if (flag)
                {
                    cancel.WaitAndThrowIfCancellationRequested(DeviceConstants.DefaultLoggerFlushBusyRetryDelay);
                    goto IL_00aa;
                }
            }
            BandClient.CheckStatus(status, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
            goto IL_00aa;
            IL_00aa:
            num++;
        }
        Logger.Log(LogLevel.Info, "LoggerFlush(): Successful on attempt {0}", num + 1);
    }

    private LogMetadataRange GetChunkRangeMetadata(int chunkCount)
    {
        int serializedByteCount = LogMetadataRange.GetSerializedByteCount();
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteInt32(chunkCount);
        };
        LogMetadataRange logMetadataRange;
        using (CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoLoggerGetChunkRangeMetadata, 4, serializedByteCount, writeArgBuf, CommandStatusHandling.DoNotCheck))
        {
            logMetadataRange = LogMetadataRange.DeserializeFromBand(cargoCommandReader);
            BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        }
        if (logMetadataRange.EndingSeqNumber < logMetadataRange.StartingSeqNumber || logMetadataRange.ByteCount > chunkCount * 4096)
        {
            Logger.Log(LogLevel.Warning, "The device returned an invalid metadata structure. RequestedChunkRangeSize = {0}. Returned ChunkRangeMetadata = (BytesCount = {1}, Start = {2}, End = {3}).", chunkCount, logMetadataRange.ByteCount, logMetadataRange.StartingSeqNumber, logMetadataRange.EndingSeqNumber);
            throw new InvalidOperationException("Invalid sensor log metadata.");
        }
        return logMetadataRange;
    }

    private void DeleteChunkRange(LogMetadataRange range)
    {
        int serializedByteCount = LogMetadataRange.GetSerializedByteCount();
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoLoggerDeleteChunkRange, serializedByteCount, CommandStatusHandling.DoNotCheck);
        range.SerializeToBand(cargoCommandWriter);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
    }

    private int RemainingDeviceLogDataChunks()
    {
        int bytesToRead = 8;
        uint num = 0u;
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoLoggerGetChunkCounts, bytesToRead, CommandStatusHandling.DoNotCheck);
        num = cargoCommandReader.ReadUInt32();
        cargoCommandReader.ReadExactAndDiscard(4);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return (int)num;
    }

    private double CalculateTransferKbitsPerSecond(long ellapsedMilliseconds, long bytes)
    {
        return (double)(bytes * 8) / ((double)ellapsedMilliseconds / 1000.0) / 1000.0;
    }

    private double CalculateTransferKbytesPerSecond(long ellapsedMilliseconds, long bytes)
    {
        return Math.Round((double)bytes / (double)ellapsedMilliseconds * 1000.0 / 1024.0, 2);
    }

    internal LogSyncResult SyncSensorLog(CancellationToken cancellationToken, ProgressTrackerPrimitive progress)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (cloudProvider == null)
        {
            throw new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
        }
        int num = platformProvider.MaxChunkRange * 10;
        LogSyncResult logSyncResult = new LogSyncResult();
        logSyncResult.LogFilesProcessing = new List<LogProcessingStatus>();
        int num2 = 0;
        int num3 = 0;
        UploadMetaData uploadMetadata = GetUploadMetadata();
        uploadMetadata.DeviceMetadataHint = "band";
        int num4 = num + 1;
        Stopwatch stopwatch = new Stopwatch();
        Stopwatch uploadWatch = new Stopwatch();
        Stopwatch stopwatch2 = Stopwatch.StartNew();
        int num5 = 0;
        LoggerFlush(cancellationToken);
        while (num2 > 0 || num4 > num)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stopwatch.Start();
            if (num4 > num)
            {
                int num6 = RemainingDeviceLogDataChunks();
                int num7 = num6 * 4096;
                progress.AddStepsTotal((num7 - num3) * 2);
                if (logSyncResult.DownloadedSensorLogBytes == 0L)
                {
                    Logger.Log(LogLevel.Info, "Sensor Log Sync beginning: Total Chunks: {0}, Total Estimated Bytes: {1}", num6, num7);
                }
                else
                {
                    Logger.Log(LogLevel.Info, "Sensor Log Sync data re-evaluated: Additional Chunks: {0}, Additional Bytes: {1}, Total Chunks: {2}, Total Estimated Bytes: {3}", num6 - num2, num7 - num3, num6, num7);
                }
                num2 = num6;
                num3 = num7;
                num4 = 0;
            }
            LogMetadataRange rangeMetadata = GetChunkRangeMetadata(platformProvider.MaxChunkRange);
            int num8 = (int)((rangeMetadata.ByteCount != 0) ? (rangeMetadata.EndingSeqNumber - rangeMetadata.StartingSeqNumber + 1) : 0);
            cancellationToken.ThrowIfCancellationRequested();
            if (num8 == 0)
            {
                break;
            }
            Logger.Log(LogLevel.Info, "Downloading log chunk range: ID's {0} - {1}, Chunks: {2}, Bytes: {3}", rangeMetadata.StartingSeqNumber, rangeMetadata.EndingSeqNumber, num8, rangeMetadata.ByteCount);
            string uploadId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            uploadMetadata.StartSequenceId = (int)rangeMetadata.StartingSeqNumber;
            uploadMetadata.EndSequenceId = (int)rangeMetadata.EndingSeqNumber;
            MemoryPipeStream transferPipe = new MemoryPipeStream((int)rangeMetadata.ByteCount, progress, loggerProvider);
            try
            {
                Task<FileUploadStatus> task = Task.Run(delegate
                {
                    uploadWatch.Start();
                    FileUploadStatus result = cloudProvider.UploadFileToCloud(transferPipe, LogFileTypes.Sensor, uploadId, uploadMetadata, cancellationToken);
                    uploadWatch.Stop();
                    return result;
                });
                try
                {
                    Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
                    {
                        rangeMetadata.SerializeToBand(w);
                    };
                    using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoLoggerGetChunkRangeData, LogMetadataRange.GetSerializedByteCount(), (int)rangeMetadata.ByteCount, writeArgBuf, CommandStatusHandling.DoNotCheck);
                    bool flag = false;
                    while (cargoCommandReader.BytesRemaining > 1)
                    {
                        int num9 = Math.Min(cargoCommandReader.BytesRemaining - 1, Math.Min(8192, cargoCommandReader.BytesRemaining));
                        try
                        {
                            cargoCommandReader.CopyTo(transferPipe, num9);
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
                        logSyncResult.DownloadedSensorLogBytes += num9;
                    }
                    if (!flag)
                    {
                        byte value = cargoCommandReader.ReadByte();
                        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowAnyNonZero, loggerProvider);
                        transferPipe.WriteByte(value);
                        logSyncResult.DownloadedSensorLogBytes++;
                    }
                }
                catch (Exception ex3)
                {
                    try
                    {
                        transferPipe.SetEndOfStream();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    if (ex3 is BandIOException)
                    {
                        throw;
                    }
                    throw new BandIOException(ex3.Message, ex3);
                }
                stopwatch.Stop();
                try
                {
                    task.Wait();
                }
                catch (AggregateException ex4)
                {
                    if (ex4.InnerExceptions.Count == 1)
                    {
                        throw ex4.InnerException;
                    }
                    throw;
                }
                if (task.Result != 0)
                {
                    logSyncResult.LogFilesProcessing.Add(new LogProcessingStatus(uploadId, DateTime.UtcNow));
                }
            }
            finally
            {
                if (transferPipe != null)
                {
                    ((IDisposable)transferPipe).Dispose();
                }
            }
            stopwatch.Start();
            DeleteChunkRange(rangeMetadata);
            stopwatch.Stop();
            logSyncResult.UploadedSensorLogBytes += rangeMetadata.ByteCount;
            num2 -= num8;
            num3 -= (int)rangeMetadata.ByteCount;
            num4 += num8;
            num5++;
        }
        stopwatch2.Stop();
        progress.Complete();
        logSyncResult.DownloadKbitsPerSecond = CalculateTransferKbitsPerSecond(stopwatch.ElapsedMilliseconds, logSyncResult.DownloadedSensorLogBytes);
        logSyncResult.DownloadKbytesPerSecond = CalculateTransferKbytesPerSecond(stopwatch.ElapsedMilliseconds, logSyncResult.DownloadedSensorLogBytes);
        logSyncResult.UploadKbitsPerSecond = CalculateTransferKbitsPerSecond(uploadWatch.ElapsedMilliseconds, logSyncResult.UploadedSensorLogBytes);
        logSyncResult.UploadKbytesPerSecond = CalculateTransferKbytesPerSecond(uploadWatch.ElapsedMilliseconds, logSyncResult.UploadedSensorLogBytes);
        logSyncResult.DownloadTime = stopwatch.ElapsedMilliseconds;
        logSyncResult.UploadTime = uploadWatch.ElapsedMilliseconds;
        logSyncResult.RanToCompletion = true;
        Logger.Log(LogLevel.Info, "Log download: {0} bytes, {1}, {2} KB/s", logSyncResult.DownloadedSensorLogBytes, stopwatch.Elapsed, logSyncResult.DownloadKbytesPerSecond);
        Logger.Log(LogLevel.Info, "Log upload: {0} bytes, {1}, {2} KB/s", logSyncResult.UploadedSensorLogBytes, uploadWatch.Elapsed, logSyncResult.UploadKbytesPerSecond);
        Logger.Log(LogLevel.Info, "Log sync: {0} bytes, {1}, {2} KB/s", logSyncResult.UploadedSensorLogBytes, stopwatch2.Elapsed, CalculateTransferKbytesPerSecond(stopwatch2.ElapsedMilliseconds, logSyncResult.UploadedSensorLogBytes));
        Logger.Log(LogLevel.Info, "Log sync: Chunk Ranges: {0}, Still Processing: {1}", num5, logSyncResult.LogFilesProcessing.Count);
        return logSyncResult;
    }

    public void DownloadSensorLog(Stream stream, int chunkRangeSize)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (chunkRangeSize > 256)
        {
            throw new ArgumentOutOfRangeException("chunkRangeSize");
        }
        LoggerFlush(CancellationToken.None);
        int num = RemainingDeviceLogDataChunks();
        Logger.Log(LogLevel.Verbose, "Starting to download the sensor log. TotalChunksToDownload = {0}", num);
        int num2 = num;
        while (num2 > 0)
        {
            int num3 = Math.Min(chunkRangeSize, num2);
            LogMetadataRange rangeMetadata = GetChunkRangeMetadata(num3);
            if (rangeMetadata.ByteCount == 0)
            {
                Logger.Log(LogLevel.Warning, "Someone is downloading/deleting the sensor log concurrently. We were expecting RemainingChunksToDownload = {0}, but the ChunkRangeMetadata came as (BytesCount = {1}, Start = {2}, End = {3}).", num2, rangeMetadata.ByteCount, rangeMetadata.StartingSeqNumber, rangeMetadata.EndingSeqNumber);
                break;
            }
            try
            {
                Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
                {
                    rangeMetadata.SerializeToBand(w);
                };
                using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoLoggerGetChunkRangeData, LogMetadataRange.GetSerializedByteCount(), (int)rangeMetadata.ByteCount, writeArgBuf, CommandStatusHandling.DoNotCheck);
                while (cargoCommandReader.BytesRemaining > 1)
                {
                    int count = Math.Min(cargoCommandReader.BytesRemaining - 1, Math.Min(8192, cargoCommandReader.BytesRemaining));
                    cargoCommandReader.CopyTo(stream, count);
                }
                byte value = cargoCommandReader.ReadByte();
                BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowAnyNonZero, loggerProvider);
                stream.WriteByte(value);
            }
            catch (BandIOException)
            {
                throw;
            }
            catch (Exception ex2)
            {
                throw new BandIOException(ex2.Message, ex2);
            }
            DeleteChunkRange(rangeMetadata);
            num2 -= num3;
        }
    }

    private UploadMetaData GetUploadMetadata()
    {
        UploadMetaData uploadMetaData = new UploadMetaData
        {
            DeviceId = DeviceUniqueId.ToString(),
            DeviceSerialNumber = SerialNumber,
            DeviceVersion = FirmwareVersions.ApplicationVersion.ToString(4),
            LogVersion = GetLogVersion(),
            PcbId = FirmwareVersions.PcbId.ToString()
        };
        PopulateUploadMetadata(uploadMetaData);
        return uploadMetaData;
    }

    private void UpdateDeviceEphemerisData(Stream ephemerisData, int length)
    {
        try
        {
            using (CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoSystemSettingsSetEphemerisFile, length, CommandStatusHandling.DoNotCheck))
            {
                ICargoStream cargoStream = base.DeviceTransport.CargoStream;
                cargoStream.WriteTimeout = 30000;
                cargoStream.ReadTimeout = 30000;
                cargoCommandWriter.CopyFromStream(ephemerisData);
                BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
            }
            Logger.Log(LogLevel.Info, "Sent ephemeris data to the device");
        }
        catch (BandIOException)
        {
            throw;
        }
        catch (Exception ex2)
        {
            throw new BandIOException(ex2.Message, ex2);
        }
    }

    private void UpdateDeviceTimeZonesData(Stream timeZonesData, int length)
    {
        try
        {
            using (CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoTimeSetTimeZoneFile, length, CommandStatusHandling.DoNotCheck))
            {
                ICargoStream cargoStream = base.DeviceTransport.CargoStream;
                cargoStream.WriteTimeout = 30000;
                cargoStream.ReadTimeout = 30000;
                cargoCommandWriter.CopyFromStream(timeZonesData);
                BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
            }
            Logger.Log(LogLevel.Info, "Sent time zone data to the device");
        }
        catch (BandIOException)
        {
            throw;
        }
        catch (Exception ex2)
        {
            throw new BandIOException(ex2.Message, ex2);
        }
    }

    private void UploadFitnessPlan(Stream fitnessPlan, int length)
    {
        using (CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoFitnessPlansWriteFile, length, CommandStatusHandling.DoNotCheck))
        {
            ICargoStream cargoStream = base.DeviceTransport.CargoStream;
            cargoStream.WriteTimeout = 30000;
            cargoStream.ReadTimeout = 30000;
            cargoCommandWriter.CopyFromStream(fitnessPlan);
            BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        }
        Logger.Log(LogLevel.Info, "Sent fitness plan data to the device");
    }

    private void UploadGolfCourse(Stream golfCourse, int length)
    {
        using (CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoGolfCourseFileWrite, length, CommandStatusHandling.DoNotCheck))
        {
            ICargoStream cargoStream = base.DeviceTransport.CargoStream;
            cargoStream.WriteTimeout = 30000;
            cargoStream.ReadTimeout = 30000;
            cargoCommandWriter.CopyFromStream(golfCourse);
            BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        }
        Logger.Log(LogLevel.Info, "Sent golf course data to the device");
    }

    private uint DeviceFileGetSize(ushort deviceCommand)
    {
        uint fileSize = 0u;
        Action<ICargoReader> readData = delegate(ICargoReader r)
        {
            fileSize = r.ReadUInt32();
        };
        if (ProtocolRead(deviceCommand, 4, readData, 5000, CommandStatusHandling.DoNotThrow).Status != DeviceStatusCodes.Success)
        {
            return 0u;
        }
        return fileSize;
    }

    public Task<string> GetProductSerialNumberAsync()
    {
        return Task.Run(() => GetProductSerialNumber());
    }

    public string GetProductSerialNumber()
    {
        int num = 12;
        string text = null;
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoGetProductSerialNumber, num, CommandStatusHandling.ThrowOnlySeverityError);
        StringBuilder stringBuilder = new StringBuilder(num);
        while (cargoCommandReader.BytesRemaining > 0)
        {
            byte b = cargoCommandReader.ReadByte();
            if (b >= 48 && b <= 57)
            {
                stringBuilder.Append((char)b);
            }
            else
            {
                stringBuilder.Append('0');
            }
        }
        return stringBuilder.ToString();
    }

    private Guid GetDeviceUniqueId()
    {
        CheckIfDisposed();
        Logger.Log(LogLevel.Verbose, "Retrieving the device unique ID");
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoCoreModuleGetUniqueID, 66, CommandStatusHandling.DoNotThrow);
        cargoCommandReader.ReadExactAndDiscard(2);
        string input = cargoCommandReader.ReadString(32);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        try
        {
            return Guid.Parse(input);
        }
        catch (Exception innerException)
        {
            BandException ex = new BandException(CommonSR.InvalidGuidFromDevice, innerException);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
    }

    public Task<bool> GpsIsEnabledAsync()
    {
        return Task.Run(() => GpsIsEnabled());
    }

    public bool GpsIsEnabled()
    {
        bool enabled = false;
        ProtocolRead(readData: delegate(ICargoReader r)
        {
            enabled = r.ReadBool32();
        }, commandId: DeviceCommands.CargoGpsIsEnabled, dataSize: 4);
        return enabled;
    }

    private UserProfileHeader ProfileAppHeaderGet()
    {
        int byteCount = UserProfileHeader.GetSerializedByteCount();
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteInt32(byteCount);
        };
        Logger.Log(LogLevel.Info, "Obtaining the header portion of the application profile from the KDevice");
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoProfileGetDataApp, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck);
        UserProfileHeader result = UserProfileHeader.DeserializeFromBand(cargoCommandReader);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    private UserProfileHeader ProfileFirmwareHeaderGet()
    {
        int byteCount = UserProfileHeader.GetSerializedByteCount();
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteInt32(byteCount);
        };
        Logger.Log(LogLevel.Info, "Obtaining the header portion of the application profile from the KDevice");
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoProfileGetDataFW, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck);
        UserProfileHeader result = UserProfileHeader.DeserializeFromBand(cargoCommandReader);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    private byte[] ProfileGetFirmwareBytes()
    {
        int serializedByteCount = UserProfileHeader.GetSerializedByteCount();
        int byteCount = serializedByteCount + 256;
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteInt32(byteCount);
        };
        Logger.Log(LogLevel.Info, "Obtaining the profile firmware bytes from the Band");
        using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoProfileGetDataFW, 4, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck);
        cargoCommandReader.ReadExactAndDiscard(serializedByteCount);
        byte[] result = cargoCommandReader.ReadExact(256);
        BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        return result;
    }

    private void ProfileSetFirmwareBytes(UserProfile profile)
    {
        Logger.Log(LogLevel.Info, "Saving the firmware profile on the KDevice");
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoProfileSetDataFW, UserProfile.GetFirmwareBytesSerializedByteCount(), CommandStatusHandling.DoNotCheck);
        profile.SerializeFirmwareBytesToBand(cargoCommandWriter, ConnectedAdminBandConstants);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
    }

    private void ProfileSetAppData(UserProfile profile)
    {
        Logger.Log(LogLevel.Info, "Saving the application profile on the band");
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoProfileSetDataApp, UserProfile.GetAppDataSerializedByteCount(ConnectedAdminBandConstants), CommandStatusHandling.DoNotCheck);
        profile.SerializeAppDataToBand(cargoCommandWriter, ConnectedAdminBandConstants);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
    }

    private void CheckIfUpdateValidForDevice(string firmwareVersion)
    {
        uint num = uint.Parse(firmwareVersion.Split('.')[2]);
        int build = FirmwareVersions.ApplicationVersion.Build;
        if ((long)build < 5100L && num >= 5100)
        {
            BandException ex = new BandException(string.Format(CommonSR.ObsoleteFirmwareVersionOnDevice, new object[2] { build, num }));
            Logger.LogException(LogLevel.Warning, ex);
            throw ex;
        }
    }

    private void DownloadFirmwareUpdateInternal(FirmwareUpdateInfo updateInfo, CancellationToken cancellationToken, FirmwareUpdateOverallProgress progressTracker)
    {
        string firmwareUpdateVersionFileRelativePath = Path.Combine(new string[2] { "FirmwareUpdate", "FirmwareUpdate.json" });
        string relativePath = Path.Combine(new string[2] { "FirmwareUpdate", "FirmwareUpdate.bin" });
        string text = Path.Combine(new string[2] { "FirmwareUpdate", "FirmwareUpdateTemp.bin" });
        FirmwareUpdateInfo firmwareUpdateVersionFromLocalFile = GetFirmwareUpdateVersionFromLocalFile(firmwareUpdateVersionFileRelativePath);
        if (firmwareUpdateVersionFromLocalFile != null && firmwareUpdateVersionFromLocalFile.FirmwareVersion.Equals(updateInfo.FirmwareVersion) && storageProvider.FileExists(relativePath))
        {
            progressTracker.DownloadFirmwareProgress.Complete();
            return;
        }
        if (cloudProvider == null)
        {
            InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredCloudConnection);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        cancellationToken.ThrowIfCancellationRequested();
        progressTracker.SetState(FirmwareUpdateState.DownloadingUpdate);
        if (storageProvider.FileExists(text))
        {
            storageProvider.DeleteFile(text);
        }
        storageProvider.CreateFolder("FirmwareUpdate");
        Stream stream = null;
        try
        {
            stream = storageProvider.OpenFileForWrite(text, append: false);
        }
        catch (Exception innerException)
        {
            BandException ex2 = new BandException(string.Format(CommonSR.FirmwareUpdateDownloadTempFileOpenError, new object[1] { text }), innerException);
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        Exception ex3 = null;
        using (stream)
        {
            try
            {
                cloudProvider.GetFirmwareUpdate(updateInfo, stream, cancellationToken);
            }
            catch (OperationCanceledException ex4)
            {
                ex3 = ex4;
            }
            catch (Exception innerException2)
            {
                ex3 = new BandCloudException(CommonSR.FirmwareUpdateDownloadError, innerException2);
            }
            if (ex3 == null)
            {
                long num = long.Parse(updateInfo.SizeInBytes);
                if (stream.Length != num)
                {
                    ex3 = new BandException(string.Format(CommonSR.FirmwareUpdateDownloadTempFileSizeMismatchError, new object[2] { stream.Length, num }));
                }
            }
            if (ex3 == null)
            {
                stream.Seek(0L, SeekOrigin.Begin);
                byte[] left = platformProvider.ComputeHashMd5(stream);
                if (!AreEqual(left, Convert.FromBase64String(updateInfo.HashMd5)))
                {
                    ex3 = new BandException(CommonSR.FirmwareUpdateIntegrityError);
                }
            }
        }
        if (ex3 != null)
        {
            try
            {
                storageProvider.DeleteFile(text);
            }
            catch (Exception)
            {
            }
            Logger.LogException(LogLevel.Error, ex3);
            throw ex3;
        }
        storageProvider.RenameFile(text, "FirmwareUpdate", "FirmwareUpdate.bin");
        SaveFirmwareUpdateVersionFileLocally(firmwareUpdateVersionFileRelativePath, updateInfo);
        Logger.Log(LogLevel.Info, "Firmware update file downloaded successfully");
        progressTracker.DownloadFirmwareProgress.Complete();
    }

    private bool AreEqual(byte[] left, byte[] right)
    {
        if (right.Length != left.Length)
        {
            return false;
        }
        for (int i = 0; i < left.Length; i++)
        {
            if (right[i] != left[i])
            {
                return false;
            }
        }
        return true;
    }

    private void SaveFirmwareUpdateVersionFileLocally(string firmwareUpdateVersionFileRelativePath, FirmwareUpdateInfo updateInfo)
    {
        using Stream outputStream = storageProvider.OpenFileForWrite(firmwareUpdateVersionFileRelativePath, append: false, 4096);
        SerializeJson(outputStream, updateInfo);
    }

    private FirmwareUpdateInfo GetFirmwareUpdateVersionFromLocalFile(string firmwareUpdateVersionFileRelativePath)
    {
        FirmwareUpdateInfo result = null;
        if (storageProvider.FileExists(firmwareUpdateVersionFileRelativePath))
        {
            try
            {
                using Stream inputStream = storageProvider.OpenFileForRead(firmwareUpdateVersionFileRelativePath, -1);
                result = DeserializeJson<FirmwareUpdateInfo>(inputStream);
                return result;
            }
            catch (Exception)
            {
                return result;
            }
        }
        return result;
    }

    private bool PushFirmwareUpdateToDeviceInternal(FirmwareUpdateInfo updateInfo, CancellationToken cancellationToken, FirmwareUpdateOverallProgress progressTracker)
    {
        bool result = false;
        Logger.Log(LogLevel.Info, "Verified that the firmware update is valid for the device");
        cancellationToken.ThrowIfCancellationRequested();
        string text = Path.Combine(new string[2] { "FirmwareUpdate", "FirmwareUpdate.bin" });
        if (storageProvider.FileExists(text))
        {
            int.Parse(updateInfo.SizeInBytes);
            UploadDeviceFirmware(text, progressTracker);
        }
        string value = FirmwareVersions.ApplicationVersion.ToString();
        if (updateInfo.FirmwareVersion.Equals(value) && GetFirmwareBinariesValidationStatusInternal())
        {
            result = true;
            Logger.Log(LogLevel.Info, "Verified that the firmware update is successfully installed on the device");
        }
        return result;
    }

    private void BootIntoUpdateMode()
    {
        lock (protocolLock)
        {
            ProtocolWrite(DeviceCommands.CargoSRAMFWUpdateBootIntoUpdateMode, 5000, swallowStatusReadException: true);
            base.DeviceTransport.Disconnect();
        }
    }

    private void WriteFirmwareUpdate(Stream updateFileStream, int updateFileSize, ProgressTrackerPrimitive progressTracker)
    {
        try
        {
            WriteFirmwareUpdateHelper(updateFileStream, updateFileSize, progressTracker);
        }
        catch (BandIOException)
        {
            throw;
        }
        catch (Exception ex2)
        {
            throw new BandIOException(ex2.Message, ex2);
        }
    }

    private void WriteFirmwareUpdateHelper(Stream updateFileStream, int updateFileSize, ProgressTrackerPrimitive progressTracker)
    {
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoSRAMFWUpdateLoadData, updateFileSize, CommandStatusHandling.DoNotCheck);
        Stopwatch stopwatch = Stopwatch.StartNew();
        ICargoStream cargoStream = base.DeviceTransport.CargoStream;
        cargoStream.WriteTimeout = 30000;
        cargoStream.ReadTimeout = 30000;
        while (cargoCommandWriter.BytesRemaining > 0)
        {
            int count = Math.Min(cargoCommandWriter.BytesRemaining, 8192);
            count = cargoCommandWriter.CopyFromStream(updateFileStream, count);
            if (count == 0)
            {
                throw new EndOfStreamException();
            }
            cargoCommandWriter.Flush();
            progressTracker.AddStepsCompleted(count);
        }
        Logger.Log(LogLevel.Info, "Firmware upload complete: {0} bytes, {1}Kbytes/second", cargoCommandWriter.Length, Math.Round((double)cargoCommandWriter.Length / (double)stopwatch.ElapsedMilliseconds * 1000.0 / 1024.0, 2));
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowAnyNonZero, loggerProvider);
    }

    private void UploadDeviceFirmware(string updateFilePath, FirmwareUpdateOverallProgress progressTracker)
    {
        CheckIfDisposed();
        if (base.DeviceTransport == null)
        {
            InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        Logger.Log(LogLevel.Info, "Updating the device firmware with the firmware update binary...");
        bool flag = DeviceTransportApp == RunningAppType.TwoUp;
        using Stream stream = storageProvider.OpenFileForRead(updateFilePath);
        progressTracker.Send2UpUpdateToDeviceProgress.AddStepsTotal((int)stream.Length);
        progressTracker.WaitToConnectAfter2UpUpdateProgress.AddStepsTotal((int)DeviceConstants.Firmware2UpUpdateConnectExpectedWaitTime.TotalMilliseconds);
        progressTracker.SendUpdateToDeviceProgress.AddStepsTotal((int)stream.Length);
        progressTracker.WaitToConnectAfterUpdateProgress.AddStepsTotal((int)DeviceConstants.FirmwareUpAppUpdateConnectExpectedWaitTime.TotalMilliseconds);
        if (flag)
        {
            progressTracker.SetTo2UpUpdate();
            UploadDeviceFirmware2UpMode(stream, (int)stream.Length, progressTracker);
            Logger.Log(LogLevel.Info, "2up mode update complete. Attempting subsequent UpApp mode update...");
            stream.Seek(0L, SeekOrigin.Begin);
        }
        UploadDeviceFirmwareAppMode(stream, (int)stream.Length, flag, progressTracker);
    }

    private void UploadDeviceFirmware2UpMode(Stream updateFileStream, int updateFileSize, FirmwareUpdateOverallProgress progressTracker)
    {
        Logger.Log(LogLevel.Warning, "Device is in 2up mode. Attempting rescue mode update...");
        Logger.Log(LogLevel.Info, "Writing firmware update to the device...");
        progressTracker.SetState(FirmwareUpdateState.SendingUpdateToDevice);
        lock (protocolLock)
        {
            WriteFirmwareUpdate(updateFileStream, updateFileSize, progressTracker.Send2UpUpdateToDeviceProgress);
            base.DeviceTransport.Disconnect();
        }
        progressTracker.SetState(FirmwareUpdateState.WaitingtoConnectAfterUpdate);
        WaitForDeviceRebootAfterFirmwareWrite(DeviceConstants.Firmware2UpUpdateConnectExpectedWaitTime, verifyId: false, progressTracker.WaitToConnectAfter2UpUpdateProgress);
    }

    private void UploadDeviceFirmwareAppMode(Stream updateFileStream, int updateFileSize, bool post2UpMode, FirmwareUpdateOverallProgress progressTracker)
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
                flag = GetDeviceOobeCompleted();
            }
            catch
            {
            }
            if (flag)
            {
                Logger.Log(LogLevel.Info, "Downloading sensor logs prior to firmware update...");
                SyncSensorLog(CancellationToken.None, progressTracker.LogSyncProgress);
            }
            else
            {
                Logger.Log(LogLevel.Info, "Device in OOBE mode.  Not downloading sensor logs.");
            }
        }
        Logger.Log(LogLevel.Info, "Booting the device into update mode...");
        progressTracker.LogSyncProgress.Complete();
        progressTracker.SetState(FirmwareUpdateState.BootingToUpdateMode);
        BootIntoUpdateMode();
        byte b = 0;
        platformProvider.Sleep(5000);
        while (!base.DeviceTransport.IsConnected)
        {
            if (b > 0)
            {
                platformProvider.Sleep(500);
            }
            try
            {
                base.DeviceTransport.Connect(1);
                if (!IsSameDeviceAfterReboot(verifyVersions: true, verifyId: false))
                {
                    base.DeviceTransport.Disconnect();
                    BandException ex = new BandException("Wrong device");
                    Logger.LogException(LogLevel.Error, ex);
                    throw ex;
                }
            }
            catch
            {
                b = (byte)(b + 1);
                if (b > 40)
                {
                    BandIOException ex2 = new BandIOException(CommonSR.DeviceReconnectMaxAttemptsExceeded);
                    Logger.LogException(LogLevel.Error, ex2, "Exception occurred prior to firmware upload, but after BootIntoUpdateMode command");
                    throw ex2;
                }
            }
        }
        InitializeCachedProperties();
        if (DeviceTransportApp != RunningAppType.UpApp)
        {
            BandException ex3 = new BandException(CommonSR.DeviceNotInUpdateMode);
            Logger.LogException(LogLevel.Error, ex3);
            throw ex3;
        }
        progressTracker.BootToUpdateModeProgress.Complete();
        progressTracker.SetState(FirmwareUpdateState.SendingUpdateToDevice);
        Logger.Log(LogLevel.Info, "Writing firmware update to the device...");
        lock (protocolLock)
        {
            WriteFirmwareUpdate(updateFileStream, updateFileSize, progressTracker.SendUpdateToDeviceProgress);
            base.DeviceTransport.Disconnect();
        }
        progressTracker.SetState(FirmwareUpdateState.WaitingtoConnectAfterUpdate);
        WaitForDeviceRebootAfterFirmwareWrite(DeviceConstants.FirmwareUpAppUpdateConnectExpectedWaitTime, verifyId: true, progressTracker.WaitToConnectAfterUpdateProgress);
    }

    private bool IsSameDeviceAfterReboot(bool verifyVersions, bool verifyId)
    {
        FirmwareVersions firmwareVersions = GetFirmwareVersionsFromBand().ToFirmwareVersions();
        if (firmwareVersions.PcbId != FirmwareVersions.PcbId || firmwareVersions.BootloaderVersion != FirmwareVersions.BootloaderVersion || (verifyVersions && (firmwareVersions.UpdaterVersion != FirmwareVersions.UpdaterVersion || firmwareVersions.ApplicationVersion != FirmwareVersions.ApplicationVersion)))
        {
            return false;
        }
        if (!verifyId)
        {
            return true;
        }
        GetDeviceSerialAndUniqueId(out var _, out var uniqueId);
        return uniqueId == DeviceUniqueId;
    }

    private void WaitForDeviceRebootAfterFirmwareWrite(TimeSpan expectedWait, bool verifyId, ProgressTrackerPrimitive progressTracker)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        Stopwatch stopwatch2 = new Stopwatch();
        DateTime dateTime = DateTime.MinValue;
        while (stopwatch.Elapsed < DeviceConstants.FirmwareUpdateConnectMaxWaitTime)
        {
            stopwatch2.Restart();
            if (stopwatch.Elapsed >= DeviceConstants.FirmwareUpdateInitialConnectWait && DateTime.UtcNow - dateTime >= DeviceConstants.FirmwareUpdateConnectRetryInterval)
            {
                if (AttemptReconnect(verifyId))
                {
                    progressTracker.Complete();
                    return;
                }
                dateTime = DateTime.UtcNow;
                progressTracker.AddStepsCompleted((int)stopwatch2.ElapsedMilliseconds);
                stopwatch2.Restart();
            }
            platformProvider.Sleep(1000);
            progressTracker.AddStepsCompleted((int)stopwatch2.ElapsedMilliseconds);
        }
        BandIOException ex = new BandIOException(CommonSR.DeviceReconnectMaxAttemptsExceeded);
        Logger.LogException(LogLevel.Error, ex, "Exception occurred after firmware upload, when trying to check if device exited update mode");
        throw ex;
    }

    private bool AttemptReconnect(bool verifyId)
    {
        CheckIfDisposed();
        try
        {
            base.DeviceTransport.Connect(1);
        }
        catch (Exception e)
        {
            Logger.LogException(LogLevel.Warning, e, "Post FW-Update connection attempt failed.");
            return false;
        }
        try
        {
            if (!IsSameDeviceAfterReboot(verifyVersions: false, verifyId))
            {
                Logger.Log(LogLevel.Warning, "Post FW-Update connected to wrong device.");
                lock (protocolLock)
                {
                    base.DeviceTransport.Disconnect();
                }
                return false;
            }
            InitializeCachedProperties();
            if (runningFirmwareApp == FirmwareApp.TwoUp || runningFirmwareApp == FirmwareApp.UpApp)
            {
                Logger.Log(LogLevel.Warning, "Post FW-Update connection attempt still in Update Mode.");
                lock (protocolLock)
                {
                    base.DeviceTransport.Disconnect();
                }
                return false;
            }
            Logger.Log(LogLevel.Info, "Device out of update mode after firmware upload");
            if (cloudProvider != null)
            {
                cloudProvider.SetUserAgent(platformProvider.GetDefaultUserAgent(FirmwareVersions), appOverride: false);
            }
        }
        catch (Exception e2)
        {
            Logger.LogException(LogLevel.Warning, e2, "Post FW-Update exception after successful connection.");
            lock (protocolLock)
            {
                base.DeviceTransport.Disconnect();
            }
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
                flag = PollDataSubscription();
            }
            catch
            {
            }
            if (!flag)
            {
                stop.WaitHandle.WaitOne(5000);
            }
        }
        Logger.Log(LogLevel.Info, "Polling streaming task exiting...");
    }

    private bool PollDataSubscription()
    {
        int num;
        lock (protocolLock)
        {
            using (CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoRemoteSubscriptionGetDataLength, 4, CommandStatusHandling.ThrowOnlySeverityError))
            {
                num = cargoCommandReader.ReadInt32();
            }
            if (num > 0)
            {
                using CargoCommandReader reader = ProtocolBeginRead(DeviceCommands.CargoRemoteSubscriptionGetData, num, CommandStatusHandling.ThrowOnlySeverityError);
                ParseRemoteSubscriptionSample(reader);
            }
        }
        return num > 0;
    }

    private void ParseRemoteSubscriptionSample(CargoCommandReader reader)
    {
        int num = 1;
        while (reader.BytesRemaining > 0)
        {
            RemoteSubscriptionSampleHeader remoteSubscriptionSampleHeader;
            try
            {
                remoteSubscriptionSampleHeader = RemoteSubscriptionSampleHeader.DeserializeFromBand(reader);
            }
            catch (Exception innerException)
            {
                Exception ex = new BandIOException($"An exception occurred reading subscription sample header #{num}", innerException);
                Logger.LogException(LogLevel.Warning, ex);
                throw ex;
            }
            try
            {
                ParseSensorPayload(reader, remoteSubscriptionSampleHeader);
            }
            catch (Exception innerException2)
            {
                Exception ex2 = new BandIOException(string.Format("An exception occurred parsing subscription data payload #{0}; Type: {1}, Size: {2}", new object[3] { num, remoteSubscriptionSampleHeader.SubscriptionType, remoteSubscriptionSampleHeader.SampleSize }), innerException2);
                Logger.LogException(LogLevel.Warning, ex2);
                throw ex2;
            }
            num++;
        }
    }

    private void ParseSensorPayload(CargoCommandReader reader, RemoteSubscriptionSampleHeader sampleHeader)
    {
        switch (sampleHeader.SubscriptionType)
        {
        case SubscriptionType.BatteryGauge:
        {
            EventHandler<BatteryGaugeUpdatedEventArgs> batteryGaugeUpdated;
            if ((batteryGaugeUpdated = this.BatteryGaugeUpdated) != null)
            {
                BatteryGaugeUpdatedEventArgs e = BatteryGaugeUpdatedEventArgs.DeserializeFromBand(reader);
                try
                {
                    batteryGaugeUpdated(this, e);
                    break;
                }
                catch
                {
                    break;
                }
            }
            reader.ReadExactAndDiscard(BatteryGaugeUpdatedEventArgs.GetSerializedByteCount());
            break;
        }
        case SubscriptionType.LogEntry:
            if (this.LogEntryUpdated != null)
            {
                LogEntryUpdatedEventArgs.DeserializeFromBand(reader);
            }
            else
            {
                LogEntryUpdatedEventArgs.DeserializeFromBand(reader);
            }
            break;
        default:
            reader.ReadExactAndDiscard(sampleHeader.SampleSize);
            break;
        }
    }

    private bool UploadFileToCloud(string relativeFilePath, FileIndex fileIndex, CancellationToken cancellationToken)
    {
        using Stream fileStream = storageProvider.OpenFileForRead(relativeFilePath, -1);
        string uploadId = string.Format("{0}-{1}", new object[2]
        {
            storageProvider.GetFileCreationTimeUtc(relativeFilePath).ToString("yyyyMMddHHmmssfff"),
            (int)fileIndex
        });
        return UploadFileToCloud(fileStream, fileIndex, uploadId, cancellationToken);
    }

    public bool UploadFileToCloud(Stream fileStream, FileIndex fileIndex, string uploadId, CancellationToken cancellationToken)
    {
        LogCompressionAlgorithm compressionAlgorithm = LogCompressionAlgorithm.uncompressed;
        int logVersion = 0;
        LogFileTypes logFileTypes;
        switch (fileIndex)
        {
        case FileIndex.Instrumentation:
            logFileTypes = LogFileTypes.Telemetry;
            break;
        case FileIndex.CrashDump:
            logFileTypes = LogFileTypes.CrashDump;
            break;
        default:
        {
            logFileTypes = LogFileTypes.Unknown;
            BandCloudException ex = new BandCloudException(CommonSR.UnsupportedFileTypeForCloudUpload);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        }
        return UploadFileToCloud(fileStream, logFileTypes, uploadId, logVersion, compressionAlgorithm, null, cancellationToken);
    }

    public Task<bool> UploadFileToCloudAsync(Stream fileStream, LogFileTypes fileType, string uploadId, int logVersion, LogCompressionAlgorithm compressionAlgorithm, string compressedFileCRC, CancellationToken cancellationToken)
    {
        return Task.Run(() => UploadFileToCloud(fileStream, fileType, uploadId, logVersion, compressionAlgorithm, compressedFileCRC, cancellationToken));
    }

    public bool UploadFileToCloud(Stream fileStream, LogFileTypes fileType, string uploadId, int logVersion, LogCompressionAlgorithm compressionAlgorithm, string compressedFileCRC, CancellationToken cancellationToken)
    {
        UploadMetaData uploadMetaData = new UploadMetaData();
        if (base.DeviceTransport != null)
        {
            uploadMetaData.DeviceId = DeviceUniqueId.ToString();
            uploadMetaData.DeviceSerialNumber = SerialNumber;
            uploadMetaData.DeviceVersion = FirmwareVersions.ApplicationVersion.ToString(4);
            uploadMetaData.LogVersion = logVersion;
            uploadMetaData.PcbId = FirmwareVersions.PcbId.ToString();
        }
        if (fileType == LogFileTypes.Sensor)
        {
            uploadMetaData.DeviceMetadataHint = "band";
        }
        uploadMetaData.CompressionAlgorithm = compressionAlgorithm;
        PopulateUploadMetadata(uploadMetaData);
        return cloudProvider.UploadFileToCloud(fileStream, fileType, uploadId, uploadMetaData, cancellationToken) == FileUploadStatus.UploadDone;
    }

    public bool UploadCrashDumpToCloud(Stream fileStream, FirmwareVersions deviceVersions, string uploadId, int logVersion, CancellationToken cancellationToken)
    {
        UploadMetaData uploadMetaData = new UploadMetaData();
        uploadMetaData.DeviceId = DeviceUniqueId.ToString();
        uploadMetaData.DeviceSerialNumber = SerialNumber;
        uploadMetaData.CompressionAlgorithm = LogCompressionAlgorithm.uncompressed;
        uploadMetaData.DeviceVersion = deviceVersions.ApplicationVersion.ToString(4);
        uploadMetaData.LogVersion = logVersion;
        uploadMetaData.PcbId = deviceVersions.PcbId.ToString();
        PopulateUploadMetadata(uploadMetaData);
        uploadMetaData.DeviceSerialNumber = "000000000000";
        return cloudProvider.UploadFileToCloud(fileStream, LogFileTypes.CrashDump, uploadId, uploadMetaData, cancellationToken) == FileUploadStatus.UploadDone;
    }

    private void SetResponse(byte index, string response)
    {
        int dataSize = 323;
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoFireballUISetSmsResponse, dataSize, CommandStatusHandling.ThrowOnlySeverityError);
        cargoCommandWriter.WriteByte(index);
        cargoCommandWriter.WriteStringWithPadding(response, 161);
    }

    private string[] GetAllResponses()
    {
        string[] array = new string[8];
        int bytesToRead = 322 * array.Length;
        try
        {
            using CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoFireballUIGetAllSmsResponse, bytesToRead, CommandStatusHandling.DoNotCheck);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = cargoCommandReader.ReadString(161);
            }
            BandClient.CheckStatus(cargoCommandReader.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
        }
        catch (BandIOException)
        {
            throw;
        }
        catch (Exception ex2)
        {
            throw new BandIOException(ex2.Message, ex2);
        }
        Logger.Log(LogLevel.Info, "Retrieved all responses from the device, phoneCallResponses will be in the first half, SMS responses will be in the second half of the response array");
        return array;
    }

    private void CheckIfStorageAvailable()
    {
        if (storageProvider == null)
        {
            Logger.Log(LogLevel.Error, CommonSR.OperationRequiredStorageProvider);
            throw new InvalidOperationException(CommonSR.OperationRequiredStorageProvider);
        }
    }

    public Task SetMeTileImageAsync(BandImage image, uint imageId = uint.MaxValue)
    {
        return Task.Run(delegate
        {
            SetMeTileImage(image, imageId);
        });
    }

    public void SetMeTileImage(BandImage image, uint imageId = uint.MaxValue)
    {
        Logger.Log(LogLevel.Info, "Setting the Me tile on the device");
        SetMeTileImageInternal(image, imageId, CancellationToken.None);
    }

    public BandImage GetMeTileImage()
    {
        Logger.Log(LogLevel.Info, "Getting the Me Tile image");
        return GetMeTileImageInternal(CancellationToken.None);
    }

    public Task<uint> GetMeTileIdAsync()
    {
        return Task.Run(() => GetMeTileId());
    }

    public uint GetMeTileId()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        uint result = 0u;
        ProtocolRead(readData: delegate(ICargoReader r)
        {
            result = r.ReadUInt32();
        }, commandId: DeviceCommands.CargoSystemSettingsGetMeTileImageID, dataSize: 4, timeout: 60000);
        return result;
    }

    public Task SendSmsNotificationAsync(uint callId, string name, string body, DateTime timestamp)
    {
        return Task.Run(delegate
        {
            SendSmsNotification(callId, name, body, timestamp);
        });
    }

    public void SendSmsNotification(uint callID, string name, string body, DateTime timestamp, NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (name == null)
        {
            ArgumentNullException ex = new ArgumentNullException("name");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (body == null)
        {
            ArgumentNullException ex2 = new ArgumentNullException("body");
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        CargoSms notification = new CargoSms(callID, name, body, timestamp, flagbits);
        SendNotification(NotificationID.Sms, NotificationPBMessageType.Messaging, notification);
    }

    public Task SendSmsNotificationAsync(CargoSms sms)
    {
        return Task.Run(delegate
        {
            SendSmsNotification(sms);
        });
    }

    public void SendSmsNotification(CargoSms sms)
    {
        Logger.Log(LogLevel.Info, "Sending SMS Notification");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        SendNotification(NotificationID.Sms, NotificationPBMessageType.Messaging, sms);
    }

    public Task SendIncomingCallNotificationAsync(CargoCall call)
    {
        return Task.Run(delegate
        {
            SendIncomingCallNotification(call);
        });
    }

    public void SendIncomingCallNotification(CargoCall call)
    {
        Logger.Log(LogLevel.Info, "Sending incoming call notification");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        call.CallType = CargoCall.PhoneCallType.Incoming;
        SendNotification(NotificationID.IncomingCall, NotificationPBMessageType.Messaging, call);
    }

    public Task SendAnsweredCallNotificationAsync(CargoCall call)
    {
        return Task.Run(delegate
        {
            SendAnsweredCallNotification(call);
        });
    }

    public void SendAnsweredCallNotification(CargoCall call)
    {
        Logger.Log(LogLevel.Info, "Sending answered call notification");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        call.CallType = CargoCall.PhoneCallType.Answered;
        SendNotification(NotificationID.AnsweredCall, NotificationPBMessageType.Messaging, call);
    }

    public Task SendHangupCallNotificationAsync(CargoCall call)
    {
        return Task.Run(delegate
        {
            SendHangupCallNotification(call);
        });
    }

    public void SendHangupCallNotification(CargoCall call)
    {
        Logger.Log(LogLevel.Info, "Sending hangup call notification");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        call.CallType = CargoCall.PhoneCallType.Hangup;
        SendNotification(NotificationID.HangupCall, NotificationPBMessageType.Messaging, call);
    }

    public Task SendMissedCallNotificationAsync(CargoCall call)
    {
        return Task.Run(delegate
        {
            SendMissedCallNotification(call);
        });
    }

    public void SendMissedCallNotification(CargoCall call)
    {
        Logger.Log(LogLevel.Info, "Sending missed call notification");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        call.CallType = CargoCall.PhoneCallType.Missed;
        SendNotification(NotificationID.MissedCall, NotificationPBMessageType.Messaging, call);
    }

    public Task SendVoiceMailCallNotificationAsync(CargoCall call)
    {
        return Task.Run(delegate
        {
            SendVoiceMailCallNotification(call);
        });
    }

    public void SendVoiceMailCallNotification(CargoCall call)
    {
        Logger.Log(LogLevel.Info, "Sending voice mail call notification");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        call.CallType = CargoCall.PhoneCallType.VoiceMail;
        SendNotification(NotificationID.Voicemail, NotificationPBMessageType.Messaging, call);
    }

    public Task SendEmailNotificationAsync(string name, string subject, DateTime timestamp)
    {
        return Task.Run(delegate
        {
            SendEmailNotification(name, subject, timestamp);
        });
    }

    public void SendEmailNotification(string name, string subject, DateTime timestamp)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }
        if (subject == null)
        {
            throw new ArgumentNullException("subject");
        }
        Logger.Log(LogLevel.Info, "Sending email to NotificationANCSEmailAppGuid");
        NotificationEmail notification = new NotificationEmail
        {
            Name = name,
            Subject = subject,
            TimeStamp = timestamp
        };
        SendNotification(NotificationID.Messaging, NotificationPBMessageType.Messaging, notification);
    }

    public Task SendTileDialogAsync(Guid tileId, string lineOne, string lineTwo, NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings, bool forceDialog = false, bool throwErrorStatus = false)
    {
        return Task.Run(delegate
        {
            SendTileDialog(tileId, lineOne, lineTwo, flagbits, forceDialog, throwErrorStatus);
        });
    }

    public void SendTileDialog(Guid tileId, string lineOne, string lineTwo, NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings, bool forceDialog = false, bool throwErrorStatus = false)
    {
        ShowDialogHelper(tileId, lineOne, lineTwo, CancellationToken.None, flagbits.ToBandNotificationFlags());
    }

    public Task SendTileMessageAsync(Guid tileId, TileMessage message, bool throwErrorStatus = false)
    {
        return Task.Run(delegate
        {
            SendTileMessage(tileId, message, throwErrorStatus);
        });
    }

    public void SendTileMessage(Guid tileId, TileMessage message, bool throwErrorStatus = false)
    {
        Logger.Log(LogLevel.Info, "Sending tile message to tileId:{0}. Flags:{1}", tileId, message.Flags);
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        NotificationMessaging notification = new NotificationMessaging(tileId)
        {
            Timestamp = (message.timestampHasValue ? message.Timestamp : DateTime.FromFileTime(0L)),
            Title = message.Title,
            Body = message.Body,
            Flags = (byte)message.Flags
        };
        SendNotification(Microsoft.Band.Notifications.NotificationID.Messaging, NotificationPBMessageType.Messaging, notification);
    }

    public Task SendPageUpdateAsync(Guid tileId, Guid pageId, ushort pageLayoutIndex, IList<ITilePageElement> textFields)
    {
        return Task.Run(delegate
        {
            SendPageUpdate(tileId, pageId, pageLayoutIndex, textFields);
        });
    }

    public void SendPageUpdate(Guid tileId, Guid pageId, ushort pageLayoutIndex, IList<ITilePageElement> textFields)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (textFields == null)
        {
            throw new ArgumentNullException("textFields");
        }
        if (textFields.Count == 0)
        {
            throw new ArgumentException(string.Format(CommonSR.GenericCountZero, new object[1] { "textFields" }), "textFields");
        }
        SetPages(tileId, CancellationToken.None, new PageData(pageId, pageLayoutIndex, PageElementsAdminToPublic(textFields)).AsEnumerable());
    }

    private IEnumerable<PageElementData> PageElementsAdminToPublic(IEnumerable<ITilePageElement> elements)
    {
        foreach (ITilePageElement iElement in elements)
        {
            if (iElement is TileTextbox)
            {
                TileTextbox tileTextbox = iElement as TileTextbox;
                yield return new TextBlockData((short)tileTextbox.ElementId, tileTextbox.TextboxValue);
                continue;
            }
            if (iElement is TileWrappableTextbox)
            {
                TileWrappableTextbox tileWrappableTextbox = iElement as TileWrappableTextbox;
                yield return new WrappedTextBlockData((short)tileWrappableTextbox.ElementId, tileWrappableTextbox.TextboxValue);
                continue;
            }
            if (iElement is TileIconbox)
            {
                TileIconbox tileIconbox = iElement as TileIconbox;
                yield return new IconData((short)tileIconbox.ElementId, tileIconbox.IconIndex);
                continue;
            }
            if (iElement is TileBarcode)
            {
                TileBarcode tileBarcode = iElement as TileBarcode;
                yield return new BarcodeData(tileBarcode.CodeType switch
                {
                    BarcodeType.Code39 => Microsoft.Band.Tiles.Pages.BarcodeType.Code39, 
                    BarcodeType.Pdf417 => Microsoft.Band.Tiles.Pages.BarcodeType.Pdf417, 
                    _ => throw new InvalidDataException("Unrecognized bar code type encountered"), 
                }, (short)tileBarcode.ElementId, tileBarcode.BarcodeValue);
                continue;
            }
            throw new InvalidDataException("Unrecognized tile page element type encountered");
        }
    }

    public Task ClearTileAsync(Guid tileId)
    {
        return Task.Run(delegate
        {
            ClearTile(tileId);
        });
    }

    public void ClearTile(Guid tileId)
    {
        Logger.Log(LogLevel.Info, "Clearing tile");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        RemovePages(tileId, CancellationToken.None);
    }

    public Task ClearPageAsync(Guid tileId, Guid pageId)
    {
        return Task.Run(delegate
        {
            ClearPage(tileId, pageId);
        });
    }

    public void ClearPage(Guid tileId, Guid pageId)
    {
        Logger.Log(LogLevel.Info, "Clearing page");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        NotificationGenericClearPage notification = new NotificationGenericClearPage(tileId, pageId);
        SendNotification(NotificationID.GenericClearPage, NotificationPBMessageType.TileManagement, notification);
    }

    public Task SendCalendarEventsAsync(CalendarEvent[] events)
    {
        return Task.Run(delegate
        {
            SendCalendarEvents(events);
        });
    }

    public void SendCalendarEvents(CalendarEvent[] events)
    {
        Logger.Log(LogLevel.Info, "Sending Calendar events");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (events == null)
        {
            ArgumentNullException ex = new ArgumentNullException("events");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (events.Length > 8)
        {
            ArgumentException ex2 = new ArgumentException(string.Format(CommonSR.AppointmentsExceedLimit, new object[1] { (ushort)8 }));
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
        ClearTile(new Guid("ec149021-ce45-40e9-aeee-08f86e4746a7"));
        foreach (CalendarEvent notification in events)
        {
            SendNotification(NotificationID.CalendarEventAdd, NotificationPBMessageType.CalendarUpdate, notification);
        }
    }

    public Task VibrateAsync(AdminVibrationType vibrationType)
    {
        return Task.Run(delegate
        {
            Vibrate(vibrationType);
        });
    }

    public void Vibrate(AdminVibrationType vibrationType)
    {
        VibrateHelper(vibrationType.ToBandVibrationType(), CancellationToken.None);
    }

    internal Task SendKeyboardMessageAsync(KeyboardCmdSample sample)
    {
        return Task.Run(delegate
        {
            SendKeyboardMessage(sample);
        });
    }

    internal void SendKeyboardMessage(KeyboardCmdSample sample)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoKeyboardCmd, 407, CommandStatusHandling.DoNotCheck);
        cargoCommandWriter.WriteByte((byte)sample.KeyboardMsgType);
        cargoCommandWriter.WriteByte(sample.NumOfCandidates);
        cargoCommandWriter.WriteByte(sample.WordIndex);
        cargoCommandWriter.WriteUInt32(sample.DataLength);
        cargoCommandWriter.Write(sample.Datafield, 0, 400);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
    }

    private void SendNotification(NotificationID notificationId, NotificationPBMessageType notificationPbType, NotificationBase notification)
    {
        int argBufSize = 0;
        Action<ICargoWriter> writeArgBuf = null;
        ushort commandId;
        int byteCount;
        switch (base.BandTypeConstants.BandType)
        {
        case BandType.Cargo:
            commandId = DeviceCommands.CargoNotification;
            byteCount = 2 + notification.GetSerializedByteCount();
            break;
        case BandType.Envoy:
            commandId = DeviceCommands.CargoNotificationProtoBuf;
            byteCount = notification.GetSerializedProtobufByteCount();
            argBufSize = 4;
            writeArgBuf = delegate(ICargoWriter w)
            {
                w.WriteUInt16((ushort)byteCount);
                w.WriteUInt16((ushort)notificationPbType);
            };
            break;
        default:
            throw new InvalidOperationException();
        }
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(commandId, argBufSize, byteCount, writeArgBuf, CommandStatusHandling.DoNotCheck);
        switch (base.BandTypeConstants.BandType)
        {
        case BandType.Cargo:
            cargoCommandWriter.WriteUInt16((ushort)notificationId);
            notification.SerializeToBand(cargoCommandWriter);
            break;
        case BandType.Envoy:
        {
            CodedOutputStream codedOutputStream = new CodedOutputStream(cargoCommandWriter, byteCount);
            notification.SerializeProtobufToBand(codedOutputStream);
            codedOutputStream.Flush();
            break;
        }
        }
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
    }

    public async Task PersonalizeDeviceAsync(StartStrip startStrip = null, BandImage image = null, BandTheme color = null, uint imageId = uint.MaxValue, IDictionary<Guid, BandTheme> customColors = null)
    {
        await Task.Run(delegate
        {
            PersonalizeDevice(startStrip, image, color, imageId, customColors);
        });
    }

    public void PersonalizeDevice(StartStrip startStrip = null, BandImage image = null, BandTheme theme = null, uint imageId = uint.MaxValue, IDictionary<Guid, BandTheme> customThemes = null)
    {
        Logger.Log(LogLevel.Verbose, "Personalizing device");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        if (image != null)
        {
            ValidateMeTileImage(image, imageId);
        }
        if (startStrip != null)
        {
            SetStartStripValidator(startStrip);
        }
        if (customThemes != null)
        {
            SetTileThemesValidator(customThemes);
        }
        RunUsingSynchronizedFirmwareUI(delegate
        {
            if (image != null)
            {
                SetMeTileImageInternal(image, imageId);
            }
            if (theme != null)
            {
                SetThemeInternal(theme);
            }
            if (startStrip != null)
            {
                SetStartStripHelperInsideSync(startStrip);
            }
            if (customThemes != null)
            {
                SetTileThemesHelper(customThemes);
            }
        }, delegate
        {
            if (startStrip != null)
            {
                SetStartStripHelperOutsideSync(startStrip);
            }
        });
    }

    internal new void InitializeCachedProperties()
    {
        base.InitializeCachedProperties();
        FirmwareVersions = base.FirmwareVersions.ToFirmwareVersions();
        loggerProvider.Log(ProviderLogLevel.Verbose, "Firmware versions:");
        loggerProvider.Log(ProviderLogLevel.Verbose, "Bootloader version: {0}", FirmwareVersions.BootloaderVersion);
        loggerProvider.Log(ProviderLogLevel.Verbose, "Updater version: {0}", FirmwareVersions.UpdaterVersion);
        loggerProvider.Log(ProviderLogLevel.Verbose, "Application version: {0}", FirmwareVersions.ApplicationVersion);
        loggerProvider.Log(ProviderLogLevel.Verbose, "Running App: {0}", base.FirmwareApp);
        if (base.FirmwareApp == FirmwareApp.App && (SerialNumber == null || !(DeviceUniqueId != Guid.Empty)))
        {
            GetDeviceSerialAndUniqueId(out var serial, out var uniqueId);
            SerialNumber = serial;
            DeviceUniqueId = uniqueId;
        }
    }

    private void GetDeviceSerialAndUniqueId(out string serial, out Guid uniqueId)
    {
        serial = GetProductSerialNumber();
        switch (ConnectedBandConstants.BandClass)
        {
        case BandClass.Cargo:
            uniqueId = GetDeviceUniqueId();
            return;
        case BandClass.Envoy:
            uniqueId = ConstructDeviceIdFromSerialNumber(serial);
            return;
        }
        loggerProvider.Log(ProviderLogLevel.Warning, $"Unrecognized band class; PcbId = {FirmwareVersions.PcbId}");
        uniqueId = Guid.Empty;
    }

    private static Guid ConstructDeviceIdFromSerialNumber(string serialNumber)
    {
        if (serialNumber.Length == 12)
        {
            return new Guid(string.Format("{0}{1}", new object[2] { "FFFFFFFF-FFFF-FFFF-FFFF-", serialNumber }));
        }
        if (serialNumber.Length < 12)
        {
            return new Guid(string.Format("{0}{1}{2}", new object[3]
            {
                "FFFFFFFF-FFFF-FFFF-FFFF-",
                new string('0', 12 - serialNumber.Length),
                serialNumber
            }));
        }
        return new Guid(string.Format("{0}{1}", new object[2]
        {
            "FFFFFFFF-FFFF-FFFF-FFFF-",
            serialNumber.Substring(0, 12)
        }));
    }

    public Task<RunningAppType> GetRunningAppAsync()
    {
        return Task.Run(() => GetRunningApp());
    }

    public RunningAppType GetRunningApp()
    {
        return runningFirmwareApp.ToRunningAppType();
    }

    public async Task SetDeviceThemeAsync(BandTheme color)
    {
        await Task.Run(delegate
        {
            SetDeviceTheme(color);
        });
    }

    public void SetDeviceTheme(BandTheme theme)
    {
        Logger.Log(LogLevel.Info, "Setting the first party theme");
        SetThemeInternal(theme, CancellationToken.None);
    }

    public async Task SetTileThemesAsync(Dictionary<Guid, BandTheme> customColors)
    {
        await Task.Run(delegate
        {
            SetTileThemes(customColors);
        });
    }

    public void SetTileThemes(Dictionary<Guid, BandTheme> customThemes)
    {
        Logger.Log(LogLevel.Info, "Setting the list of tile themes");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        SetTileThemesValidator(customThemes);
        RunUsingSynchronizedFirmwareUI(delegate
        {
            SetTileThemesHelper(customThemes);
        });
    }

    private void SetTileThemesValidator(IDictionary<Guid, BandTheme> customThemes)
    {
        if (customThemes == null)
        {
            ArgumentNullException ex = new ArgumentNullException("customColors");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        foreach (KeyValuePair<Guid, BandTheme> customTheme in customThemes)
        {
            ValidateTileTheme(customTheme.Value, customTheme.Key);
        }
    }

    private void SetTileThemesHelper(IDictionary<Guid, BandTheme> customThemes)
    {
        foreach (KeyValuePair<Guid, BandTheme> customTheme in customThemes)
        {
            SetTileThemeInternal(customTheme.Value, customTheme.Key);
        }
    }

    public async Task SetTileThemeAsync(BandTheme color, Guid id)
    {
        await Task.Run(delegate
        {
            SetTileTheme(color, id);
        });
    }

    public void SetTileTheme(BandTheme theme, Guid id)
    {
        Logger.Log(LogLevel.Info, "Setting the tile theme");
        SetTileThemeInternal(theme, id, CancellationToken.None);
    }

    public async Task<BandTheme> GetDeviceThemeAsync()
    {
        return await Task.Run(() => GetDeviceTheme());
    }

    public BandTheme GetDeviceTheme()
    {
        Logger.Log(LogLevel.Info, "Getting first party theme");
        return GetThemeInternal(CancellationToken.None);
    }

    public async Task ResetThemeColorsAsync()
    {
        await Task.Run(delegate
        {
            ResetThemeColors();
        });
    }

    public void ResetThemeColors()
    {
        Logger.Log(LogLevel.Info, "Resetting theme colors");
        ResetThemeInternal(CancellationToken.None);
    }

    public Task<StartStrip> GetStartStripAsync()
    {
        return Task.Run(() => GetStartStrip());
    }

    public StartStrip GetStartStrip()
    {
        Logger.Log(LogLevel.Verbose, "Getting start strip");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        return new StartStrip(from t in GetInstalledTiles()
            select t.ToAdminBandTile());
    }

    public Task<IList<AdminBandTile>> GetDefaultTilesAsync()
    {
        return Task.Run(() => GetDefaultTiles());
    }

    public IList<AdminBandTile> GetDefaultTiles()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        return (from tileData in GetDefaultTilesInternal()
            select tileData.ToAdminBandTile()).ToList();
    }

    public Task<AdminBandTile> GetTileAsync(Guid id)
    {
        return Task.Run(() => GetTile(id));
    }

    public AdminBandTile GetTile(Guid id)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        return (from t in GetInstalledTiles()
            where t.AppID == id
            select t.ToAdminBandTile()).FirstOrDefault();
    }

    public Task<StartStrip> GetStartStripNoImagesAsync()
    {
        return Task.Run(() => GetStartStripNoImages());
    }

    public StartStrip GetStartStripNoImages()
    {
        Logger.Log(LogLevel.Verbose, "Getting start strip without images");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        return new StartStrip(from t in GetInstalledTilesNoIcons()
            select t.ToAdminBandTile());
    }

    public Task<IList<AdminBandTile>> GetDefaultTilesNoImagesAsync()
    {
        return Task.Run(() => GetDefaultTilesNoImages());
    }

    public IList<AdminBandTile> GetDefaultTilesNoImages()
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        return (from t in GetDefaultTilesNoIconsInternal()
            select t.ToAdminBandTile()).ToList();
    }

    public Task<AdminBandTile> GetTileNoImageAsync(Guid id)
    {
        return Task.Run(() => GetTileNoImage(id));
    }

    public AdminBandTile GetTileNoImage(Guid id)
    {
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        return (from t in GetInstalledTilesNoIcons()
            where t.AppID == id
            select t.ToAdminBandTile()).FirstOrDefault();
    }

    public Task SetStartStripAsync(StartStrip tiles)
    {
        return Task.Run(delegate
        {
            SetStartStrip(tiles);
        });
    }

    public void SetStartStrip(StartStrip tiles)
    {
        Logger.Log(LogLevel.Verbose, "Setting start strip");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        SetStartStripValidator(tiles);
        RunUsingSynchronizedFirmwareUI(delegate
        {
            SetStartStripHelperInsideSync(tiles);
        }, delegate
        {
            SetStartStripHelperOutsideSync(tiles);
        });
    }

    private void SetStartStripValidator(StartStrip tiles)
    {
        if (tiles == null)
        {
            ArgumentNullException ex = new ArgumentNullException("tiles");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (tiles.Count > GetTileCapacity() || tiles.Count > GetTileMaxAllocatedCapacity())
        {
            ArgumentException ex2 = new ArgumentException(string.Format(CommonSR.GenericCountMax, new object[1] { "tiles" }));
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
    }

    private void SetStartStripHelperInsideSync(StartStrip tiles)
    {
        IList<AdminBandTile> list = (from t in GetDefaultTilesNoIconsInternal()
            select t.ToAdminBandTile()).ToList();
        IList<AdminBandTile> list2 = (from t in GetInstalledTilesNoIcons()
            select t.ToAdminBandTile()).ToList();
        for (int i = 0; i < list2.Count; i++)
        {
            if (!tiles.Contains(list2[i].Id))
            {
                UnregisterTileIcons(list2[i].Id);
                list2.RemoveAt(i);
                i--;
            }
        }
        Logger.Log(LogLevel.Info, "Removed all tiles that are currently on device, but shouldn't be");
        for (int j = 0; j < tiles.Count; j++)
        {
            bool flag = false;
            foreach (AdminBandTile item in list)
            {
                if (item.Id == tiles[j].Id)
                {
                    flag = true;
                    break;
                }
            }
            bool flag2 = false;
            foreach (AdminBandTile item2 in list2)
            {
                if (item2.Id == tiles[j].Id)
                {
                    flag2 = true;
                    break;
                }
            }
            if (flag && flag2)
            {
                continue;
            }
            if (flag && !flag2)
            {
                DynamicAppRegisterDefaultTile(tiles[j]);
                continue;
            }
            if (!flag && flag2)
            {
                if (tiles[j].Images != null)
                {
                    DynamicAppRegisterTileOrIcons(tiles[j], iconsAlreadyRegistered: true);
                    SetTileIconIndexes(tiles[j].Id, tiles[j].TileImageIndex, tiles[j].BadgeImageIndex, tiles[j].NotificationImageIndex);
                }
                continue;
            }
            if (tiles[j].Images == null)
            {
                throw new ArgumentException(CommonSR.NewTileRequiresImages);
            }
            DynamicAppRegisterTileOrIcons(tiles[j], iconsAlreadyRegistered: false);
            SetTileIconIndexes(tiles[j].Id, tiles[j].TileImageIndex, tiles[j].BadgeImageIndex, tiles[j].NotificationImageIndex);
        }
        InstalledAppListSet(tiles);
        for (int k = 0; k < tiles.Count; k++)
        {
            if (tiles[k].Theme != null)
            {
                SetTileThemeInternal(tiles[k].Theme, tiles[k].Id);
            }
        }
    }

    private void SetStartStripHelperOutsideSync(StartStrip tiles)
    {
        foreach (AdminBandTile tile in tiles)
        {
            Logger.Log(LogLevel.Verbose, "Apply all queued-up Layout removal actions for tile: {0}", tile.Name);
            foreach (uint item in tile.LayoutsToRemove)
            {
                DynamicPageLayoutRemoveLayout(tile.Id, item);
            }
            Logger.Log(LogLevel.Verbose, "Apply all queued-up Layout add/overwrite actions for tile: {0}", tile.Name);
            foreach (KeyValuePair<uint, TileLayout> layout in tile.Layouts)
            {
                DynamicPageLayoutSetSerializedLayout(tile.Id, layout.Key, layout.Value.layoutBlob);
            }
        }
        Logger.Log(LogLevel.Info, "Finished layout operations on the tiles");
    }

    public Task UpdateTileAsync(AdminBandTile tile)
    {
        return Task.Run(delegate
        {
            UpdateTile(tile);
        });
    }

    public void UpdateTile(AdminBandTile tile)
    {
        Logger.Log(LogLevel.Verbose, "Updating tile");
        CheckIfDisposed();
        CheckIfDisconnectedOrUpdateMode();
        UpdateTileValidator(tile);
        List<AdminBandTile> list = (from t in GetDefaultTilesNoIconsInternal()
            select t.ToAdminBandTile()).ToList();
        IList<AdminBandTile> list2 = (from t in GetInstalledTilesNoIcons()
            select t.ToAdminBandTile()).ToList();
        bool isDefault = false;
        foreach (AdminBandTile item in (IEnumerable<AdminBandTile>)list)
        {
            if (item.Id == tile.Id)
            {
                isDefault = true;
                break;
            }
        }
        bool alreadyOnDevice = false;
        foreach (AdminBandTile item2 in list2)
        {
            if (item2.Id == tile.Id)
            {
                alreadyOnDevice = true;
                break;
            }
        }
        RunUsingSynchronizedFirmwareUI(delegate
        {
            UpdateTileHelperInsideSync(tile, isDefault, alreadyOnDevice);
        }, delegate
        {
            UpdateTileHelperOutsideSync(tile, alreadyOnDevice);
        });
    }

    private void UpdateTileValidator(AdminBandTile tile)
    {
        if (tile == null)
        {
            ArgumentNullException ex = new ArgumentNullException("tile");
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
    }

    private void UpdateTileHelperInsideSync(AdminBandTile tile, bool isDefault, bool alreadyOnDevice)
    {
        if (!alreadyOnDevice)
        {
            return;
        }
        if (!(isDefault && alreadyOnDevice))
        {
            if (isDefault && !alreadyOnDevice)
            {
                DynamicAppRegisterDefaultTile(tile);
            }
            else if (!isDefault && alreadyOnDevice)
            {
                if (tile.Images != null)
                {
                    DynamicAppRegisterTileOrIcons(tile, iconsAlreadyRegistered: true);
                    SetTileIconIndexes(tile.Id, tile.TileImageIndex, tile.BadgeImageIndex, tile.NotificationImageIndex);
                }
            }
            else
            {
                if (tile.Images == null)
                {
                    ArgumentException ex = new ArgumentException(CommonSR.NewTileRequiresImages);
                    Logger.LogException(LogLevel.Error, ex);
                    throw ex;
                }
                DynamicAppRegisterTileOrIcons(tile, iconsAlreadyRegistered: false);
                SetTileIconIndexes(tile.Id, tile.TileImageIndex, tile.BadgeImageIndex, tile.NotificationImageIndex);
            }
        }
        InstalledAppListSetTile(tile);
    }

    private void UpdateTileHelperOutsideSync(AdminBandTile tile, bool alreadyOnDevice)
    {
        if (!alreadyOnDevice)
        {
            return;
        }
        Logger.Log(LogLevel.Info, "Apply all queued-up Layout removal actions");
        foreach (uint item in tile.LayoutsToRemove)
        {
            DynamicPageLayoutRemoveLayout(tile.Id, item);
        }
        Logger.Log(LogLevel.Info, "Apply all queued-up Layout add/overwrite actions");
        foreach (KeyValuePair<uint, TileLayout> layout in tile.Layouts)
        {
            DynamicPageLayoutSetSerializedLayout(tile.Id, layout.Key, layout.Value.layoutBlob);
        }
        Logger.Log(LogLevel.Info, "Finished layout operations on the tile");
    }

    public Task<uint> GetMaxTileCountAsync()
    {
        return Task.Run(() => GetMaxTileCount());
    }

    public uint GetMaxTileCount()
    {
        return GetTileCapacity();
    }

    private void SetIconIndexValidator(uint iconIndex)
    {
        CheckIfDisposed();
        if (base.DeviceTransport == null)
        {
            InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        if (iconIndex >= ConnectedBandConstants.MaxIconsPerTile)
        {
            ArgumentOutOfRangeException ex2 = new ArgumentOutOfRangeException("iconIndex");
            Logger.LogException(LogLevel.Error, ex2);
            throw ex2;
        }
    }

    public Task SetTileIconIndexAsync(Guid tileId, uint iconIndex)
    {
        return Task.Run(delegate
        {
            SetTileIconIndex(tileId, iconIndex);
        });
    }

    public void SetTileIconIndex(Guid tileId, uint iconIndex)
    {
        SetIconIndexValidator(iconIndex);
        SetMainIconIndex(tileId, iconIndex);
    }

    public Task SetTileBadgeIconIndexAsync(Guid tileId, uint iconIndex)
    {
        return Task.Run(delegate
        {
            SetTileBadgeIconIndex(tileId, iconIndex);
        });
    }

    public void SetTileBadgeIconIndex(Guid tileId, uint iconIndex)
    {
        SetIconIndexValidator(iconIndex);
        SetBadgeIconIndex(tileId, iconIndex);
    }

    public Task SetTileNotificationIconIndexAsync(Guid id, uint iconIndex)
    {
        return Task.Run(delegate
        {
            SetTileNotificationIconIndex(id, iconIndex);
        });
    }

    public void SetTileNotificationIconIndex(Guid tileId, uint iconIndex)
    {
        SetIconIndexValidator(iconIndex);
        SetNotificationIconIndex(tileId, iconIndex);
    }

    public Task<AdminTileSettings> GetTileSettingsAsync(Guid id)
    {
        return Task.Run(() => GetTileSettings(id));
    }

    public AdminTileSettings GetTileSettings(Guid id)
    {
        CheckIfDisposed();
        if (base.DeviceTransport == null)
        {
            InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        AdminTileSettings settings = AdminTileSettings.None;
        ProtocolRead(writeArgBuf: delegate(ICargoWriter w)
        {
            w.WriteGuid(id);
        }, readData: delegate(ICargoReader r)
        {
            settings = (AdminTileSettings)r.ReadUInt16();
        }, commandId: DeviceCommands.CargoInstalledAppListGetSettingsMask, argBufSize: 16, dataSize: 2);
        return settings;
    }

    public Task SetTileSettingsAsync(Guid tileId, AdminTileSettings settings)
    {
        return Task.Run(delegate
        {
            SetTileSettings(tileId, settings);
        });
    }

    public void SetTileSettings(Guid tileId, AdminTileSettings settings)
    {
        CheckIfDisposed();
        if (base.DeviceTransport == null)
        {
            InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        Action<ICargoWriter> writeData = delegate(ICargoWriter w)
        {
            w.WriteGuid(tileId);
            w.WriteUInt16((ushort)settings);
        };
        ProtocolWriteWithData(DeviceCommands.CargoInstalledAppListSetSettingsMask, 18, writeData);
    }

    public Task EnableTileSettingsAsync(Guid tileId, AdminTileSettings settings)
    {
        return Task.Run(delegate
        {
            EnableTileSettings(tileId, settings);
        });
    }

    public void EnableTileSettings(Guid tileId, AdminTileSettings settings)
    {
        ChangeTileSettings(tileId, settings, DeviceCommands.CargoInstalledAppListEnableSetting);
    }

    public Task DisableTileSettingsAsync(Guid tileId, AdminTileSettings settings)
    {
        return Task.Run(delegate
        {
            DisableTileSettings(tileId, settings);
        });
    }

    public void DisableTileSettings(Guid tileId, AdminTileSettings settings)
    {
        ChangeTileSettings(tileId, settings, DeviceCommands.CargoInstalledAppListDisableSetting);
    }

    private void ChangeTileSettings(Guid tileId, AdminTileSettings settings, ushort commandId)
    {
        CheckIfDisposed();
        if (base.DeviceTransport == null)
        {
            InvalidOperationException ex = new InvalidOperationException(CommonSR.OperationRequiredConnectedDevice);
            Logger.LogException(LogLevel.Error, ex);
            throw ex;
        }
        ushort num = (ushort)settings;
        ushort bitIndex = 0;
        while (num > 0)
        {
            if ((int)num % 2 != 0)
            {
                Action<ICargoWriter> writeData = delegate(ICargoWriter w)
                {
                    w.WriteGuid(tileId);
                    w.WriteUInt16(bitIndex);
                };
                ProtocolWriteWithData(commandId, 18, writeData);
            }
            num = (ushort)(num >> 1);
            bitIndex++;
        }
    }

    private void InstalledAppListSet(IList<AdminBandTile> orderedList)
    {
        List<TileData> list = new List<TileData>();
        for (int i = 0; i < orderedList.Count; i++)
        {
            TileData item = orderedList[i].ToTileData((uint)i);
            list.Add(item);
        }
        SetStartStripData(list, list.Count);
    }

    private AdminBandTile InstalledAppListGetTile(Guid guid)
    {
        Action<ICargoWriter> writeArgBuf = delegate(ICargoWriter w)
        {
            w.WriteGuid(guid);
        };
        int bytesToRead = 1024 + TileData.GetSerializedByteCount();
        using PooledBuffer pooledBuffer = BufferServer.GetBuffer(1024);
        TileData tileData;
        using (CargoCommandReader cargoCommandReader = ProtocolBeginRead(DeviceCommands.CargoInstalledAppListGetTile, 16, bytesToRead, writeArgBuf, CommandStatusHandling.ThrowOnlySeverityError))
        {
            base.DeviceTransport.CargoStream.ReadTimeout = 60000;
            cargoCommandReader.ReadExact(pooledBuffer.Buffer, 0, pooledBuffer.Length);
            tileData = TileData.DeserializeFromBand(cargoCommandReader);
            tileData.Icon = BandIconRleCodec.DecodeTileIconRle(pooledBuffer);
        }
        return tileData.ToAdminBandTile();
    }

    private void InstalledAppListSetTile(AdminBandTile tile)
    {
        TileData tileData = tile.ToTileData();
        Logger.Log(LogLevel.Info, "Invoking CargoInstalledAppListSetTile");
        using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoInstalledAppListSetTile, TileData.GetSerializedByteCount(), CommandStatusHandling.DoNotCheck);
        base.DeviceTransport.CargoStream.ReadTimeout = 60000;
        tileData.SerializeToBand(cargoCommandWriter);
        BandClient.CheckStatus(cargoCommandWriter.CommandStatus, CommandStatusHandling.ThrowOnlySeverityError, loggerProvider);
    }

    private void DynamicAppRegisterDefaultTile(AdminBandTile tile)
    {
        int dataSize = 20;
        Logger.Log(LogLevel.Verbose, "Invoking CargoDynamicAppRegisterApp for tile: {0}", tile.Name);
        try
        {
            using CargoCommandWriter cargoCommandWriter = ProtocolBeginWrite(DeviceCommands.CargoDynamicAppRegisterApp, dataSize, CommandStatusHandling.ThrowOnlySeverityError);
            cargoCommandWriter.WriteGuid(tile.Id);
            cargoCommandWriter.WriteInt32(0);
        }
        catch (BandIOException)
        {
            throw;
        }
        catch (Exception ex2)
        {
            throw new BandIOException(ex2.Message, ex2);
        }
    }

    private void DynamicAppRegisterTileOrIcons(AdminBandTile tile, bool iconsAlreadyRegistered)
    {
        RegisterTileIcons(tile.Id, tile.Name, tile.Images, iconsAlreadyRegistered);
    }
}
