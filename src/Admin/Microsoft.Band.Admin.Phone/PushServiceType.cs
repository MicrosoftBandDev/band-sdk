// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.PushServiceType
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

namespace Microsoft.Band.Admin
{
  internal enum PushServiceType
  {
    WakeApp = 0,
    RemoteSubscription = 1,
    Sms = 100, // 0x00000064
    DismissCall = 101, // 0x00000065
    DismissCallThenSms = 102, // 0x00000066
    MuteCall = 103, // 0x00000067
    AnswerCall = 104, // 0x00000068
    SnoozeAlarm = 110, // 0x0000006E
    DismissAlarm = 111, // 0x0000006F
    VoicePacketBegin = 200, // 0x000000C8
    VoicePacketData = 201, // 0x000000C9
    VoicePacketEnd = 202, // 0x000000CA
    VoicePacketCancel = 203, // 0x000000CB
    TileEvent = 204, // 0x000000CC
    TileSyncRequest = 205, // 0x000000CD
    CortanaContext = 206, // 0x000000CE
    ActivityEvent = 208, // 0x000000D0
    Keyboard = 220, // 0x000000DC
    KeyboardSetContext = 222, // 0x000000DE
    BandBatteryEvent = 230, // 0x000000E6
  }
}
