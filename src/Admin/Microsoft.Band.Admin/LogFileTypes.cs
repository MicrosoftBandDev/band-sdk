// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.LogFileTypes
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Runtime.Serialization;

namespace Microsoft.Band.Admin
{
    [DataContract]
    public enum LogFileTypes
    {
        [EnumMember] Unknown = 0,
        [EnumMember] Sensor = 5,
        CrashDump = 6,
        [EnumMember] KAppLogs = 7,
        Telemetry = 8,
    }
}
