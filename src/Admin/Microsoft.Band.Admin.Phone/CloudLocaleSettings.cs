// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.CloudLocaleSettings
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System.Runtime.Serialization;

namespace Microsoft.Band.Admin
{
  [DataContract]
  internal struct CloudLocaleSettings
  {
    [DataMember]
    internal string LocaleName;
    [DataMember(Name = "Locale")]
    internal ushort LocaleId;
    [DataMember]
    internal ushort Language;
    [DataMember]
    internal char DateSeparator;
    [DataMember]
    internal char NumberSeparator;
    [DataMember]
    internal char DecimalSeparator;
    [DataMember]
    internal byte TimeFormat;
    [DataMember]
    internal byte DateFormat;
    [DataMember(Name = "DisplaySizeUnit")]
    internal byte DistanceShortUnits;
    [DataMember(Name = "DisplayDistanceUnit")]
    internal byte DistanceLongUnits;
    [DataMember(Name = "DisplayWeightUnit")]
    internal byte MassUnits;
    [DataMember(Name = "DisplayVolumeUnit")]
    internal byte VolumeUnits;
    [DataMember(Name = "DisplayCaloriesUnit")]
    internal byte EnergyUnits;
    [DataMember(Name = "DisplayTemperatureUnit")]
    internal byte TemperatureUnits;
  }
}
