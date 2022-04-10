// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.NotificationID
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin
{
  internal enum NotificationID : ushort
  {
    Sms = 1,
    Email = 2,
    IncomingCall = 11, // 0x000B
    AnsweredCall = 12, // 0x000C
    MissedCall = 13, // 0x000D
    HangupCall = 14, // 0x000E
    Voicemail = 15, // 0x000F
    CalendarEventAdd = 16, // 0x0010
    CalendarClear = 17, // 0x0011
    Messaging = 18, // 0x0012
    GenericDialog = 100, // 0x0064
    GenericUpdate = 101, // 0x0065
    GenericClearTile = 102, // 0x0066
    GenericClearPage = 103, // 0x0067
  }
}
