// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.ICargoClient
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Microsoft.Band.Admin.LogProcessing;
using Microsoft.Band.Admin.Streaming;
using Microsoft.Band.Personalization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Band.Admin
{
  public interface ICargoClient : IBandClient, IDisposable
  {
    IDynamicBandConstants ConnectedBandConstants { get; }

    string UserAgent { get; set; }

    RunningAppType DeviceTransportApp { get; }

    Guid DeviceUniqueId { get; }

    string SerialNumber { get; }

    FirmwareVersions FirmwareVersions { get; }

    event EventHandler Disconnected;

    event EventHandler<BatteryGaugeUpdatedEventArgs> BatteryGaugeUpdated;

    event EventHandler<LogEntryUpdatedEventArgs> LogEntryUpdated;

    Task<SyncResult> ObsoleteSyncDeviceToCloudAsync(
      CancellationToken cancellationToken,
      IProgress<SyncProgress> progress = null,
      bool logsOnly = false);

    Task<SyncResult> SyncRequiredBandInfoAsync(
      CancellationToken cancellationToken,
      IProgress<SyncProgress> progress = null);

    Task<SyncResult> SyncAuxiliaryBandInfoAsync(CancellationToken cancellationToken);

    Task<SyncResult> SyncAllBandInfoAsync(CancellationToken cancellationToken);

    Task<long> GetPendingLocalDataBytesAsync();

    long GetPendingLocalDataBytes();

    Task<long> GetPendingDeviceDataBytesAsync();

    long GetPendingDeviceDataBytes();

    Task<IUserProfile> GetUserProfileFromDeviceAsync();

    IUserProfile GetUserProfileFromDevice();

    Task<IUserProfile> GetUserProfileAsync();

    IUserProfile GetUserProfile();

    Task<IUserProfile> GetUserProfileAsync(CancellationToken cancellationToken);

    IUserProfile GetUserProfile(CancellationToken cancellationToken);

    Task SaveUserProfileAsync(IUserProfile profile, DateTimeOffset? updateTime = null);

    void SaveUserProfile(IUserProfile profile, DateTimeOffset? updateTime = null);

    Task SaveUserProfileAsync(
      IUserProfile profile,
      CancellationToken cancellationToken,
      DateTimeOffset? updateTime = null);

    void SaveUserProfile(
      IUserProfile profile,
      CancellationToken cancellationToken,
      DateTimeOffset? updateTimeN = null);

    void SaveUserProfileToBandOnly(IUserProfile profile, DateTimeOffset? updateTime = null);

    Task SaveUserProfileToBandOnlyAsync(IUserProfile profile, DateTimeOffset? updateTimeN = null);

    Task SaveUserProfileFirmwareBytesAsync(CancellationToken cancellationToken);

    void SaveUserProfileFirmwareBytes(CancellationToken cancellationToken);

    Task ImportUserProfileAsync(CancellationToken cancellationToken);

    Task ImportUserProfileAsync(IUserProfile userProfile, CancellationToken cancellationToken);

    void ImportUserProfile(IUserProfile userProfile, CancellationToken cancellationToken);

    DeviceProfileStatus GetDeviceAndProfileLinkStatus(IUserProfile userProfile = null);

    Task<DeviceProfileStatus> GetDeviceAndProfileLinkStatusAsync(
      IUserProfile userProfile = null);

    Task<DeviceProfileStatus> GetDeviceAndProfileLinkStatusAsync(
      CancellationToken cancellationToken,
      IUserProfile userProfile = null);

    DeviceProfileStatus GetDeviceAndProfileLinkStatus(
      CancellationToken cancellationToken,
      IUserProfile userProfile = null);

    Task<DeviceProfileStatus> GetDeviceAndProfileLinkStatusAsync(
      CancellationToken cancellationToken,
      Guid cloudUserId,
      Guid cloudDeviceId);

    Task LinkDeviceToProfileAsync(IUserProfile userProfile = null, bool importUserProfile = false);

    void LinkDeviceToProfile(IUserProfile userProfile = null, bool importUserProfile = false);

    Task LinkDeviceToProfileAsync(
      CancellationToken cancellationToken,
      IUserProfile userProfile = null,
      bool importUserProfile = false);

    void LinkDeviceToProfile(
      CancellationToken cancellationToken,
      IUserProfile userProfile = null,
      bool importUserProfile = false);

    Task UnlinkDeviceFromProfileAsync(IUserProfile userProfile = null);

    void UnlinkDeviceFromProfile(IUserProfile userProfile = null);

    Task UnlinkDeviceFromProfileAsync(
      CancellationToken cancellationToken,
      IUserProfile userProfile = null);

    void UnlinkDeviceFromProfile(CancellationToken cancellationToken, IUserProfile userProfile = null);

    Task SyncUserProfileAsync(CancellationToken cancellationToken);

    void SyncUserProfile(CancellationToken cancellationToken);

    Task<DateTime> GetDeviceUtcTimeAsync();

    DateTime GetDeviceUtcTime();

    Task<DateTime> GetDeviceLocalTimeAsync();

    DateTime GetDeviceLocalTime();

    Task SetDeviceUtcTimeAsync();

    void SetDeviceUtcTime();

    Task SetDeviceUtcTimeAsync(DateTime utc);

    void SetDeviceUtcTime(DateTime utc);

    Task<CargoTimeZoneInfo> GetDeviceTimeZoneAsync();

    CargoTimeZoneInfo GetDeviceTimeZone();

    Task SetDeviceTimeZoneAsync(CargoTimeZoneInfo timeZone);

    void SetDeviceTimeZone(CargoTimeZoneInfo timeZone);

    Task SetCurrentTimeAndTimeZoneAsync();

    void SetCurrentTimeAndTimeZone();

    Task SetCurrentTimeAndTimeZoneAsync(CancellationToken cancellationToken);

    void SetCurrentTimeAndTimeZone(CancellationToken cancellationToken);

    Task<bool> GetFirmwareBinariesValidationStatusAsync();

    bool GetFirmwareBinariesValidationStatus();

    Task<bool> GetDeviceOobeCompletedAsync();

    bool GetDeviceOobeCompleted();

    Task<EphemerisCoverageDates> GetGpsEphemerisCoverageDatesFromDeviceAsync();

    EphemerisCoverageDates GetGpsEphemerisCoverageDatesFromDevice();

    Task<bool> UpdateGpsEphemerisDataAsync();

    bool UpdateGpsEphemerisData();

    Task<bool> UpdateGpsEphemerisDataAsync(
      CancellationToken cancellationToken,
      bool forceUpdate = false);

    bool UpdateGpsEphemerisData(CancellationToken cancellationToken, bool forceUpdate = false);

    Task<uint> GetTimeZonesDataVersionFromDeviceAsync();

    uint GetTimeZonesDataVersionFromDevice();

    Task<bool> UpdateTimeZoneListAsync(IUserProfile profile = null);

    bool UpdateTimeZoneList(IUserProfile profile = null);

    Task<bool> UpdateTimeZoneListAsync(
      CancellationToken cancellationToken,
      bool forceUpdate = false,
      IUserProfile profile = null);

    bool UpdateTimeZoneList(
      CancellationToken cancellationToken,
      bool forceUpdate = false,
      IUserProfile profile = null);

    Task<IFirmwareUpdateInfo> GetLatestAvailableFirmwareVersionAsync(
      List<KeyValuePair<string, string>> queryParams = null);

    IFirmwareUpdateInfo GetLatestAvailableFirmwareVersion(
      List<KeyValuePair<string, string>> queryParams = null);

    Task<IFirmwareUpdateInfo> GetLatestAvailableFirmwareVersionAsync(
      CancellationToken cancellationToken,
      List<KeyValuePair<string, string>> queryParams = null);

    IFirmwareUpdateInfo GetLatestAvailableFirmwareVersion(
      CancellationToken cancellationToken,
      List<KeyValuePair<string, string>> queryParams = null);

    Task<bool> UpdateFirmwareAsync(
      IFirmwareUpdateInfo updateInfo,
      IProgress<FirmwareUpdateProgress> progress = null);

    Task<bool> UpdateFirmwareAsync(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null);

    bool UpdateFirmware(IFirmwareUpdateInfo updateInfo, IProgress<FirmwareUpdateProgress> progress = null);

    bool UpdateFirmware(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null);

    Task DownloadFirmwareUpdateAsync(IFirmwareUpdateInfo updateInfo);

    void DownloadFirmwareUpdate(IFirmwareUpdateInfo updateInfo);

    Task DownloadFirmwareUpdateAsync(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null);

    void DownloadFirmwareUpdate(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null);

    Task<bool> PushFirmwareUpdateToDeviceAsync(IFirmwareUpdateInfo updateInfo);

    Task<bool> PushFirmwareUpdateToDeviceAsync(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null);

    bool PushFirmwareUpdateToDevice(IFirmwareUpdateInfo updateInfo);

    bool PushFirmwareUpdateToDevice(
      IFirmwareUpdateInfo updateInfo,
      CancellationToken cancellationToken,
      IProgress<FirmwareUpdateProgress> progress = null);

    Task UpdateLogProcessingAsync(
      List<LogProcessingStatus> fileInfoList,
      EventHandler<LogProcessingUpdatedEventArgs> notificationHandler,
      bool singleCallback,
      CancellationToken cancellationToken);

    void UpdateLogProcessing(
      List<LogProcessingStatus> filesProcessing,
      EventHandler<LogProcessingUpdatedEventArgs> notificationHandler,
      bool singleCallback,
      CancellationToken cancellationToken);

    Task SetGoalsAsync(Goals goals);

    void SetGoals(Goals goals);

    Task SetWorkoutPlanAsync(Stream workoutPlansStream);

    void SetWorkoutPlan(Stream workoutPlansStream);

    Task SetWorkoutPlanAsync(byte[] workoutPlansData);

    void SetWorkoutPlan(byte[] workoutPlanData);

    Task<int> GetGolfCourseMaxSizeAsync();

    int GetGolfCourseMaxSize();

    Task SetGolfCourseAsync(Stream golfCourseStream, int length = -1);

    void SetGolfCourse(Stream golfCourseStream, int length = -1);

    Task SetGolfCourseAsync(byte[] golfCourseData);

    void SetGolfCourse(byte[] golfCourseData);

    Task NavigateToScreenAsync(CargoScreen screen);

    void NavigateToScreen(CargoScreen screen);

    Task<OobeStage> GetOobeStageAsync();

    OobeStage GetOobeStage();

    Task SetOobeStageAsync(OobeStage stage);

    void SetOobeStage(OobeStage stage);

    Task FinalizeOobeAsync();

    void FinalizeOobe();

    Task<string[]> GetPhoneCallResponsesAsync();

    string[] GetPhoneCallResponses();

    Task SetPhoneCallResponsesAsync(
      string response1,
      string response2,
      string response3,
      string response4);

    void SetPhoneCallResponses(
      string response1,
      string response2,
      string response3,
      string response4);

    Task<string[]> GetSmsResponsesAsync();

    string[] GetSmsResponses();

    Task SetSmsResponsesAsync(
      string response1,
      string response2,
      string response3,
      string response4);

    void SetSmsResponses(string response1, string response2, string response3, string response4);

    Task<CargoRunDisplayMetrics> GetRunDisplayMetricsAsync();

    CargoRunDisplayMetrics GetRunDisplayMetrics();

    Task SetRunDisplayMetricsAsync(CargoRunDisplayMetrics cargoRunDisplayMetrics);

    void SetRunDisplayMetrics(CargoRunDisplayMetrics cargoRunDisplayMetrics);

    Task<CargoBikeDisplayMetrics> GetBikeDisplayMetricsAsync();

    CargoBikeDisplayMetrics GetBikeDisplayMetrics();

    Task SetBikeDisplayMetricsAsync(CargoBikeDisplayMetrics cargoBikeDisplayMetrics);

    void SetBikeDisplayMetrics(CargoBikeDisplayMetrics cargoBikeDisplayMetrics);

    Task SetBikeSplitMultiplierAsync(int multiplier);

    void SetBikeSplitMultiplier(int multiplier);

    Task<int> GetBikeSplitMultiplierAsync();

    int GetBikeSplitMultiplier();

    Task<CargoRunStatistics> GetLastRunStatisticsAsync();

    CargoRunStatistics GetLastRunStatistics();

    Task<CargoWorkoutStatistics> GetLastWorkoutStatisticsAsync();

    CargoWorkoutStatistics GetLastWorkoutStatistics();

    Task<CargoSleepStatistics> GetLastSleepStatisticsAsync();

    CargoSleepStatistics GetLastSleepStatistics();

    Task<CargoGuidedWorkoutStatistics> GetLastGuidedWorkoutStatisticsAsync();

    CargoGuidedWorkoutStatistics GetLastGuidedWorkoutStatistics();

    Task SensorSubscribeAsync(SensorType subscriptionType);

    void SensorSubscribe(SensorType subscriptionType);

    Task SensorUnsubscribeAsync(SensorType subscriptionType);

    void SensorUnsubscribe(SensorType subscriptionType);

    Task GenerateSensorLogAsync(TimeSpan duration);

    void GenerateSensorLog(TimeSpan duration);

    Task LoggerEnableAsync();

    void LoggerEnable();

    Task LoggerDisableAsync();

    void LoggerDisable();

    Task<ushort> GetLogVersionAsync();

    ushort GetLogVersion();

    void DownloadSensorLog(Stream stream, int chunkRangeSize);

    Task<string> GetProductSerialNumberAsync();

    string GetProductSerialNumber();

    Task<bool> GpsIsEnabledAsync();

    bool GpsIsEnabled();

    Task<bool> UploadFileToCloudAsync(
      Stream fileStream,
      LogFileTypes fileType,
      string uploadId,
      int logVersion,
      LogCompressionAlgorithm compressionAlgorithm,
      string compressedFileCRC,
      CancellationToken cancellationToken);

    bool UploadFileToCloud(
      Stream fileStream,
      LogFileTypes fileType,
      string uploadId,
      int logVersion,
      LogCompressionAlgorithm compressionAlgorithm,
      string compressedFileCRC,
      CancellationToken cancellationToken);

    Task<StartStrip> GetStartStripAsync();

    StartStrip GetStartStrip();

    Task<IList<AdminBandTile>> GetDefaultTilesAsync();

    IList<AdminBandTile> GetDefaultTiles();

    Task<AdminBandTile> GetTileAsync(Guid id);

    AdminBandTile GetTile(Guid id);

    Task<StartStrip> GetStartStripNoImagesAsync();

    StartStrip GetStartStripNoImages();

    Task<IList<AdminBandTile>> GetDefaultTilesNoImagesAsync();

    IList<AdminBandTile> GetDefaultTilesNoImages();

    Task<AdminBandTile> GetTileNoImageAsync(Guid id);

    AdminBandTile GetTileNoImage(Guid id);

    Task SetStartStripAsync(StartStrip tiles);

    void SetStartStrip(StartStrip tiles);

    Task UpdateTileAsync(AdminBandTile tile);

    void UpdateTile(AdminBandTile tile);

    Task<uint> GetMaxTileCountAsync();

    uint GetMaxTileCount();

    Task SetTileIconIndexAsync(Guid id, uint iconIndex);

    void SetTileIconIndex(Guid id, uint iconIndex);

    Task SetTileBadgeIconIndexAsync(Guid id, uint iconIndex);

    void SetTileBadgeIconIndex(Guid id, uint iconIndex);

    Task SetTileNotificationIconIndexAsync(Guid id, uint iconIndex);

    void SetTileNotificationIconIndex(Guid id, uint iconIndex);

    Task<AdminTileSettings> GetTileSettingsAsync(Guid id);

    AdminTileSettings GetTileSettings(Guid id);

    Task SetTileSettingsAsync(Guid id, AdminTileSettings settings);

    void SetTileSettings(Guid id, AdminTileSettings settings);

    Task EnableTileSettingsAsync(Guid id, AdminTileSettings settings);

    void EnableTileSettings(Guid id, AdminTileSettings settings);

    Task DisableTileSettingsAsync(Guid id, AdminTileSettings settings);

    void DisableTileSettings(Guid id, AdminTileSettings settings);

    Task SetDeviceThemeAsync(BandTheme color);

    void SetDeviceTheme(BandTheme color);

    Task SetTileThemesAsync(Dictionary<Guid, BandTheme> customColors);

    void SetTileThemes(Dictionary<Guid, BandTheme> customColors);

    Task SetTileThemeAsync(BandTheme color, Guid id);

    void SetTileTheme(BandTheme color, Guid id);

    Task<BandTheme> GetDeviceThemeAsync();

    BandTheme GetDeviceTheme();

    Task ResetThemeColorsAsync();

    void ResetThemeColors();

    Task<RunningAppType> GetRunningAppAsync();

    RunningAppType GetRunningApp();

    Task SetMeTileImageAsync(BandImage image, uint imageId = 4294967295);

    void SetMeTileImage(BandImage image, uint imageId = 4294967295);

    Task<BandImage> GetMeTileImageAsync();

    BandImage GetMeTileImage();

    Task<uint> GetMeTileIdAsync();

    uint GetMeTileId();

    Task PersonalizeDeviceAsync(
      StartStrip startStrip = null,
      BandImage image = null,
      BandTheme color = null,
      uint imageId = 4294967295,
      IDictionary<Guid, BandTheme> customColors = null);

    void PersonalizeDevice(
      StartStrip startStrip = null,
      BandImage image = null,
      BandTheme color = null,
      uint imageId = 4294967295,
      IDictionary<Guid, BandTheme> customColors = null);

    Task SendSmsNotificationAsync(uint callId, string name, string body, DateTime timestamp);

    void SendSmsNotification(
      uint callID,
      string name,
      string body,
      DateTime timestamp,
      NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings);

    Task SendSmsNotificationAsync(CargoSms sms);

    void SendSmsNotification(CargoSms sms);

    Task SendIncomingCallNotificationAsync(CargoCall incomingCall);

    void SendIncomingCallNotification(CargoCall incomingCall);

    Task SendAnsweredCallNotificationAsync(CargoCall answeredCall);

    void SendAnsweredCallNotification(CargoCall answeredCall);

    Task SendHangupCallNotificationAsync(CargoCall hangupCall);

    void SendHangupCallNotification(CargoCall hangupCall);

    Task SendMissedCallNotificationAsync(CargoCall missedCall);

    void SendMissedCallNotification(CargoCall missedCall);

    Task SendVoiceMailCallNotificationAsync(CargoCall voiceMail);

    void SendVoiceMailCallNotification(CargoCall voiceMail);

    Task SendEmailNotificationAsync(string name, string subject, DateTime timestamp);

    void SendEmailNotification(string name, string subject, DateTime timestamp);

    Task SendTileDialogAsync(
      Guid tileId,
      string lineOne,
      string lineTwo,
      NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings,
      bool forceDialog = false,
      bool throwErrorStatus = false);

    void SendTileDialog(
      Guid tileId,
      string lineOne,
      string lineTwo,
      NotificationFlags flagbits = NotificationFlags.UnmodifiedNotificationSettings,
      bool forceDialog = false,
      bool throwErrorStatus = false);

    Task SendTileMessageAsync(Guid tileId, TileMessage message, bool throwErrorStatus = false);

    void SendTileMessage(Guid tileId, TileMessage message, bool throwErrorStatus = false);

    Task SendPageUpdateAsync(
      Guid tileId,
      Guid pageId,
      ushort pageLayoutIndex,
      IList<ITilePageElement> textFields);

    void SendPageUpdate(
      Guid tileId,
      Guid pageId,
      ushort pageLayoutIndex,
      IList<ITilePageElement> textFields);

    Task ClearTileAsync(Guid tileId);

    void ClearTile(Guid tileId);

    Task ClearPageAsync(Guid tileId, Guid pageId);

    void ClearPage(Guid tileId, Guid pageId);

    Task SendCalendarEventsAsync(CalendarEvent[] events);

    void SendCalendarEvents(CalendarEvent[] events);

    Task VibrateAsync(AdminVibrationType vibrationType);

    void Vibrate(AdminVibrationType vibrationType);

    bool UploadCrashDumpToCloud(
      Stream fileStream,
      FirmwareVersions deviceVersions,
      string uploadId,
      int logVersion,
      CancellationToken cancellationToken);

    Task SyncWebTilesAsync(bool forceSync, CancellationToken cancellationToken);

    Task SyncWebTileAsync(Guid tileId, CancellationToken cancellationToken);

    void EnableRetailDemoMode();

    Task EnableRetailDemoModeAsync();

    void DisableRetailDemoMode();

    Task DisableRetailDemoModeAsync();

    void CargoSystemSettingsFactoryReset();

    Task CargoSystemSettingsFactoryResetAsync();

    Task<IList<WorkoutActivity>> GetWorkoutActivitiesAsync();

    Task SetWorkoutActivitiesAsync(IList<WorkoutActivity> activities);

    Task<SleepNotification> GetSleepNotificationAsync();

    Task SetSleepNotificationAsync(SleepNotification notification);

    Task DisableSleepNotificationAsync();

    Task<LightExposureNotification> GetLightExposureNotificationAsync();

    Task SetLightExposureNotificationAsync(LightExposureNotification notification);

    Task DisableLightExposureNotificationAsync();

    void CloseSession();
  }
}
