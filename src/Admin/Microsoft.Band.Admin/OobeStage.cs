// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.OobeStage
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin
{
  public enum OobeStage : ushort
  {
    AskPhoneType = 0,
    DownloadMessage = 1,
    WaitingOnPhoneToEnterCode = 2,
    WaitingOnPhoneToAcceptPairing = 3,
    PairingSuccess = 4,
    CheckingForUpdate = 5,
    StartingUpdate = 6,
    UpdateComplete = 7,
    WaitingOnPhoneToCompleteOobe = 8,
    PressActionButton = 9,
    ErrorState = 10, // 0x000A
    PairMessage = 11, // 0x000B
    PreStateCharging = 100, // 0x0064
    PreStateLanguageSelect = 101, // 0x0065
  }
}
