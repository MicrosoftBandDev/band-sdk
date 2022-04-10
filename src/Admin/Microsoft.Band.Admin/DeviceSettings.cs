// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.DeviceSettings
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;

namespace Microsoft.Band.Admin
{
  public sealed class DeviceSettings
  {
    public DeviceSettings()
    {
      this.LocaleSettings = CargoLocaleSettings.Default();
      this.AdditionalSettings = new Dictionary<string, string>();
    }

    public Guid DeviceId { get; set; }

    public string SerialNumber { get; set; }

    public string DeviceName { get; set; }

    public int ProfileDeviceVersion { get; set; }

    public DateTime? LastReset { get; set; }

    public DateTime? LastSuccessfulSync { get; set; }

    public CargoLocaleSettings LocaleSettings { get; set; }

    public RunMeasurementUnitType RunDisplayUnits { get; set; }

    public bool TelemetryEnabled { get; set; }

    public Dictionary<string, string> AdditionalSettings { get; set; }

    public byte[] Reserved { get; set; }

    public byte[] FirmwareByteArray { get; set; }
  }
}
