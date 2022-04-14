using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class CloudProfile
{
    [DataMember(EmitDefaultValue = false)]
    internal string CreatedOn;

    [DataMember(EmitDefaultValue = true)]
    internal string LastKDKSyncUpdateOn;

    [DataMember(EmitDefaultValue = false)]
    internal string LastModifiedOn;

    [DataMember(EmitDefaultValue = false)]
    internal string LastUserUpdateOn;

    [DataMember(EmitDefaultValue = false)]
    internal string EndPoint;

    [DataMember(EmitDefaultValue = false)]
    internal string FUSEndPoint;

    [DataMember(Name = "ODSUserID", EmitDefaultValue = false)]
    internal Guid? UserID;

    [DataMember(EmitDefaultValue = false)]
    internal string FirstName;

    [DataMember(EmitDefaultValue = false)]
    internal string LastName;

    [DataMember(EmitDefaultValue = false)]
    internal string ZipCode;

    [DataMember(EmitDefaultValue = false)]
    internal string EmailAddress;

    [DataMember(EmitDefaultValue = false)]
    internal string SmsAddress;

    [DataMember(Name = "HeightInMM", EmitDefaultValue = false)]
    internal uint? Height;

    [DataMember(Name = "WeightInGrams", EmitDefaultValue = false)]
    internal uint? Weight;

    [DataMember(EmitDefaultValue = false)]
    internal bool? HasCompletedOOBE;

    [DataMember(EmitDefaultValue = true)]
    internal Gender Gender;

    [DataMember(Name = "DateOfBirth", EmitDefaultValue = false)]
    internal string Birthdate;

    [DataMember(EmitDefaultValue = false)]
    internal ulong? TotalCaloriesBurnedFromMotion;

    [DataMember(EmitDefaultValue = false)]
    internal ulong? TotalCaloriesBurnedWhileNotWorn;

    [DataMember(EmitDefaultValue = false)]
    internal ulong? TotalCaloriesBurnedFromHR;

    [DataMember(EmitDefaultValue = false)]
    internal ulong? TotalDistanceTravelledInM;

    [DataMember(EmitDefaultValue = false)]
    internal ulong? TotalDistanceMeasuredByPedometerInM;

    [DataMember(EmitDefaultValue = false)]
    internal ulong? TotalDistanceMeasuredByGPSInM;

    [DataMember(EmitDefaultValue = false)]
    internal ulong? TotalStepsCounted;

    [DataMember(EmitDefaultValue = false)]
    internal uint? HRGain;

    [DataMember(EmitDefaultValue = false)]
    internal uint? HRRecoveryTime;

    [DataMember(EmitDefaultValue = false)]
    internal uint? HRIntensity;

    [DataMember(EmitDefaultValue = false)]
    internal uint? HRResponseTime;

    [DataMember(EmitDefaultValue = false)]
    internal float? StrideLengthWhileWalking;

    [DataMember(EmitDefaultValue = false)]
    internal float? StrideLengthWhileRunning;

    [DataMember(EmitDefaultValue = false)]
    internal float? StrideLengthWhileJogging;

    [DataMember(EmitDefaultValue = false)]
    internal uint? MaxHR;

    [DataMember(EmitDefaultValue = false)]
    internal uint? RestingHR;

    [DataMember(EmitDefaultValue = false)]
    internal bool? RestingHROverride;

    [DataMember(EmitDefaultValue = false)]
    internal bool? MaxHROverride;

    [DataMember(EmitDefaultValue = false)]
    internal float? ActivityClass;

    [DataMember]
    internal CloudApplicationSettings ApplicationSettings { get; set; }

    [DataMember]
    internal CloudDeviceSettings DeviceSettings { get; set; }

    [DataMember]
    internal IDictionary<Guid, CloudDeviceSettings> AllDeviceSettings { get; set; }
}
