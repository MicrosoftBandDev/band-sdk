﻿// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.FirmwareVersions
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin
{
  public sealed class FirmwareVersions
  {
    public byte PcbId { get; set; }

    public FirmwareVersion BootloaderVersion { get; set; }

    public FirmwareVersion UpdaterVersion { get; set; }

    public FirmwareVersion ApplicationVersion { get; set; }
  }
}
