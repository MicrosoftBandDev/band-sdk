// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.VersionExtensions
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

namespace Microsoft.Band.Admin
{
    internal static class VersionExtensions
    {
        public static FirmwareVersions ToFirmwareVersions(this CargoVersions versions) => new FirmwareVersions()
        {
            BootloaderVersion = new FirmwareVersion(versions.BootloaderVersion),
            UpdaterVersion = new FirmwareVersion(versions.UpdaterVersion),
            ApplicationVersion = new FirmwareVersion(versions.ApplicationVersion),
            PcbId = versions.ApplicationVersion.PCBId
        };
    }
}
