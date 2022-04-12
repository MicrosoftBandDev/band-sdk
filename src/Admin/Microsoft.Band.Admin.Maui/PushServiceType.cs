namespace Microsoft.Band.Admin;

internal enum PushServiceType
{
    WakeApp = 0,
    RemoteSubscription = 1,
    Sms = 100,
    DismissCall = 101,
    DismissCallThenSms = 102,
    MuteCall = 103,
    AnswerCall = 104,
    SnoozeAlarm = 110,
    DismissAlarm = 111,
    VoicePacketBegin = 200,
    VoicePacketData = 201,
    VoicePacketEnd = 202,
    VoicePacketCancel = 203,
    TileEvent = 204,
    TileSyncRequest = 205,
    CortanaContext = 206,
    ActivityEvent = 208,
    Keyboard = 220,
    KeyboardSetContext = 222,
    BandBatteryEvent = 230
}
