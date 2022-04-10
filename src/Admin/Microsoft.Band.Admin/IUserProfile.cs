// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.IUserProfile
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;

namespace Microsoft.Band.Admin
{
    public interface IUserProfile
    {
        ushort Version { get; }

        DateTimeOffset? CreatedOn { get; set; }

        DateTimeOffset? LastKDKSyncUpdateOn { get; set; }

        Guid UserID { get; set; }

        string FirstName { get; set; }

        string LastName { get; set; }

        string EmailAddress { get; set; }

        string ZipCode { get; set; }

        string SmsAddress { get; set; }

        DateTime Birthdate { get; set; }

        uint Weight { get; set; }

        ushort Height { get; set; }

        Gender Gender { get; set; }

        bool HasCompletedOOBE { get; set; }

        byte RestingHeartRate { get; set; }

        ApplicationSettings ApplicationSettings { get; set; }

        DeviceSettings DeviceSettings { get; set; }

        IDictionary<Guid, DeviceSettings> AllDeviceSettings { get; set; }
    }
}
