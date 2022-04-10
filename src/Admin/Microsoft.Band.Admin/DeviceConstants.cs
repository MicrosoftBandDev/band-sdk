// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.DeviceConstants
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
  public static class DeviceConstants
  {
    public const int CargoPcbId = 9;
    public const int EnvoyMinimumPcbId = 20;
    public const ushort ProfileIdLength = 16;
    public const ushort DeviceProfileFirmwareBytesSerializedByteCount = 282;
    public const ushort NotificationEmailNameMaxLengthV1 = 80;
    public const ushort NotificationEmailSubjectMaxLengthV1 = 72;
    public const ushort NotificationCallerNameMaxLengthV1 = 40;
    public const ushort NotificationSmsNameMaxLengthV1 = 40;
    public const ushort NotificationSmsBodyMaxLengthV1 = 320;
    public const ushort NotificationCalendarShortStringLengthV1 = 40;
    public const ushort NotificationCalendarLongStringLengthV1 = 320;
    public const ushort NotificationCalendarShortStringLengthV2 = 40;
    public const ushort NotificationCalendarLongStringLengthV2 = 320;
    public const string NotificationZeroAppGuid = "00000000-0000-0000-0000-000000000000";
    public const string NotificationSleepAppGuid = "23e7bc94-f90d-44e0-843f-250910fdf74e";
    public const string NotificationWorkoutAppGuid = "a708f02a-03cd-4da0-bb33-be904e6a2924";
    public const string NotificationWeatherAppGuid = "69a39b4e-084b-4b53-9a1b-581826df9e36";
    public const string NotificationFinanceAppGuid = "5992928a-bd79-4bb5-9678-f08246d03e68";
    public const string NotificationCallAppName = "Phone Calls";
    public const string NotificationTextMessageAppName = "SMS";
    public const string NotificationEmailName = "Emails";
    public const string NotificationCortanaAppName = "Cortana Reminders";
    public const string NotificationCortanaRemindMeAppGuid = "{79ffbd59-d090-4365-aabf-384ee84ee3e5}";
    public const string NotificationLyncAppGuid = "{d85d8a57-0f61-4ff3-a0f4-444e131d8491}";
    public const string NotificationAlarmsAppGuid = "{5b04b775-356b-4aa0-aaf8-6491ffea560a}";
    public const ushort LoggerChunkSize = 4096;
    public const ushort DeviceTimeDeltaThreshold = 0;
    public const byte LocaleNameMaxLength = 6;
    public const int BootIntoUpdateModeConnectDelay = 5000;
    public const int BootIntoUpdateModeConnectRetryDelay = 500;
    public const byte BootIntoUpdateModeConnectRetryLimit = 40;
    public static readonly TimeSpan Firmware2UpUpdateConnectExpectedWaitTime = TimeSpan.FromSeconds(20.0);
    public static readonly TimeSpan FirmwareUpAppUpdateConnectExpectedWaitTime = TimeSpan.FromSeconds(40.0);
    public static readonly TimeSpan FirmwareUpdateConnectMaxWaitTime = TimeSpan.FromMinutes(4.0);
    public static readonly TimeSpan FirmwareUpdateInitialConnectWait = TimeSpan.FromSeconds(5.0);
    public static readonly TimeSpan FirmwareUpdateConnectRetryInterval = TimeSpan.FromSeconds(2.0);
    public const uint MinimumDeviceVersionForFirmwareUpdate = 5100;
    public const int ResponseStringMaxLength = 160;
    public const int MaxDeviceResponses = 8;
    public const int DeviceResponseStringMaxLength = 161;
    public const int CortanaMessageMaxLength = 160;
    public const int MaxLogChunkBatchSize = 524288;
    public static readonly TimeSpan DefaultLoggerFlushBusyRetryDelay = TimeSpan.FromMilliseconds(250.0);
    public const string MinVersionRequiredForIntelligentCannedResponse = "10.2.2422.0";
    public const int MaximumWorkoutActivityTypes = 15;
    public const int MaximumWorkoutActivityNameCharacters = 40;
    public const int WorkoutActivityReservedBytes = 9;
    public const int WorkoutActivityDataReservedBytes = 10;
    public const int MaximumSleepNotificationHeaderCharacters = 80;
    public const int MaximumSleepNotificationBodyCharacters = 40;
    public const int SleepNotificationReservedBytes = 8;
    public const int MaximumLightExposureNotificationHeaderCharacters = 80;
    public const int MaximumLightExposureNotificationBodyCharacters = 40;
    public const int LightExposureNotificationReservedBytes = 10;
    public const int FirmwareValidityCheckTimeoutMilliseconds = 20000;
  }
}
