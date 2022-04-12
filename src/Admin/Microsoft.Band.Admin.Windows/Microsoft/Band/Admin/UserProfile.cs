using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Band.Admin;

internal sealed class UserProfile : IUserProfile
{
    private static readonly string[] DateTimeFormats = new string[11]
    {
        "M/d/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-ddTHH:mm:ss.fzzz", "yyyy-MM-ddTHH:mm:ss.ffzzz", "yyyy-MM-ddTHH:mm:ss.fffzzz", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.fZ", "yyyy-MM-ddTHH:mm:ss.ffZ",
        "yyyy-MM-ddTHH:mm:ss.fffZ"
    };

    private static readonly int DeserializeDeviceMasteredAppDataFromBand_FastForward1 = CargoFileTime.GetSerializedByteCount() + 4 + 2 + 1 + 32;

    private const int DeserializeDeviceMasteredAppDataFromBand_FastForward2 = 2;

    private static readonly int DeserializeDeviceMasteredAppDataFromBand_FastForward3 = CargoFileTime.GetSerializedByteCount() + 1 + CargoFileTime.GetSerializedByteCount() + 1 + CargoFileTime.GetSerializedByteCount() + 1 + CargoFileTime.GetSerializedByteCount() + 1;

    public UserProfileHeader Header { get; private set; }

    public ushort Version
    {
        get
        {
            return Header.Version;
        }
        set
        {
            Header.Version = value;
        }
    }

    public DateTimeOffset? CreatedOn { get; set; }

    public DateTimeOffset? LastKDKSyncUpdateOn
    {
        get
        {
            return Header.LastKDKSyncUpdateOn;
        }
        set
        {
            Header.LastKDKSyncUpdateOn = value;
        }
    }

    public Guid UserID
    {
        get
        {
            return Header.UserID;
        }
        set
        {
            Header.UserID = value;
        }
    }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string EmailAddress { get; set; }

    public string ZipCode { get; set; }

    public string SmsAddress { get; set; }

    public DateTime Birthdate { get; set; }

    public uint Weight { get; set; }

    public ushort Height { get; set; }

    public Gender Gender { get; set; }

    public bool HasCompletedOOBE { get; set; }

    public byte RestingHeartRate { get; set; }

    public ApplicationSettings ApplicationSettings { get; set; }

    public DeviceSettings DeviceSettings { get; set; }

    public IDictionary<Guid, DeviceSettings> AllDeviceSettings { get; set; }

    internal uint CaloriesFromMotion { get; set; }

    internal uint CaloriesFromNotWorn { get; set; }

    internal uint CaloriesFromHr { get; set; }

    internal uint TotalDistance { get; set; }

    internal uint PedometerDistance { get; set; }

    internal uint GPSDistance { get; set; }

    internal uint TotalSteps { get; set; }

    internal uint CaloriesFromMotionAtReset { get; set; }

    internal uint CaloriesFromNotWornAtReset { get; set; }

    internal uint CaloriesFromHrAtReset { get; set; }

    internal uint TotalDistanceAtReset { get; set; }

    internal uint PedometerDistanceAtReset { get; set; }

    internal uint GPSDistanceAtReset { get; set; }

    internal uint TotalStepsAtReset { get; set; }

    internal byte HeartRateRestingComputed { get; set; }

    internal byte HeartRateGainComputed { get; set; }

    internal ushort HeartRateRecoveryTimeComputed { get; set; }

    internal ushort HeartRateIntensityComputed { get; set; }

    internal ushort HeartRateResponseTimeComputed { get; set; }

    internal ushort StrideLengthWalkingComputed { get; set; }

    internal ushort StrideLengthRunningComputed { get; set; }

    internal ushort StrideLengthJoggingComputed { get; set; }

    internal float[] MotionEstimateTable { get; set; }

    internal byte RecoveryFitnessProfile_MinHr { get; set; }

    internal byte RecoveryFitnessProfile_MaxHr { get; set; }

    internal int RecoveryFitnessProfile_MaxMET { get; set; }

    internal DateTimeOffset LastModifiedOn { get; set; }

    internal DateTimeOffset LastUserUpdateOn { get; set; }

    internal string EndPoint { get; set; }

    internal string FUSEndPoint { get; set; }

    internal ulong TotalCaloriesBurnedFromMotion { get; set; }

    internal ulong TotalCaloriesBurnedWhileNotWorn { get; set; }

    internal ulong TotalCaloriesBurnedFromHR { get; set; }

    internal ulong TotalDistanceTravelledInM { get; set; }

    internal ulong TotalDistanceMeasuredByPedometerInM { get; set; }

    internal ulong TotalDistanceMeasuredByGPSInM { get; set; }

    internal ulong TotalStepsCounted { get; set; }

    internal uint HRGain { get; set; }

    internal uint HRRecoveryTime { get; set; }

    internal uint HRIntensity { get; set; }

    internal uint HRResponseTime { get; set; }

    internal float StrideLengthWhileWalking { get; set; }

    internal float StrideLengthWhileRunning { get; set; }

    internal float StrideLengthWhileJogging { get; set; }

    internal byte MaxHR { get; set; }

    internal bool? RestingHROverride { get; set; }

    internal bool? MaxHROverride { get; set; }

    internal float? ActivityClass { get; set; }

    public DateTimeOffset? HwagChangeTime { get; set; }

    public byte HwagChangeAgent { get; set; }

    public DateTimeOffset? DeviceNameChangeTime { get; set; }

    public byte DeviceNameChangeAgent { get; set; }

    public DateTimeOffset? LocaleSettingsChangeTime { get; set; }

    public byte LocaleSettingsChangeAgent { get; set; }

    public DateTimeOffset? LanguageChangeTime { get; set; }

    public byte LanguageChangeAgent { get; set; }

    private UserProfile()
    {
    }

    internal UserProfile(CloudProfile profile, DynamicAdminBandConstants constants)
    {
        if (profile == null)
        {
            throw new ArgumentNullException("profile");
        }
        Header = new UserProfileHeader();
        if (constants != null)
        {
            Header.Version = constants.BandProfileAppDataVersion;
        }
        if (profile.UserID.HasValue)
        {
            Header.UserID = profile.UserID.Value;
        }
        EmailAddress = profile.EmailAddress;
        ZipCode = profile.ZipCode;
        SmsAddress = profile.SmsAddress;
        FirstName = profile.FirstName;
        LastName = profile.LastName;
        if (profile.Height.HasValue)
        {
            Height = (ushort)profile.Height.Value;
        }
        if (profile.Weight.HasValue)
        {
            Weight = profile.Weight.Value;
        }
        Gender = profile.Gender;
        Birthdate = ToDateTime(profile.Birthdate).Date;
        HasCompletedOOBE = profile.HasCompletedOOBE.HasValue && profile.HasCompletedOOBE.Value;
        CreatedOn = ToDateTimeOffset(profile.CreatedOn);
        Header.LastKDKSyncUpdateOn = ToDateTimeOffset(profile.LastKDKSyncUpdateOn);
        ApplicationSettings = profile.ApplicationSettings.ToApplicationSettings();
        DeviceSettings = profile.DeviceSettings.ToDeviceSettings();
        AllDeviceSettings = profile.AllDeviceSettings.ToAllDeviceSettings();
        LastModifiedOn = ToDateTimeOffset(profile.LastModifiedOn) ?? DateTimeOffset.MinValue;
        LastUserUpdateOn = ToDateTimeOffset(profile.LastUserUpdateOn) ?? DateTimeOffset.MinValue;
        EndPoint = profile.EndPoint;
        FUSEndPoint = profile.FUSEndPoint;
        if (profile.TotalCaloriesBurnedFromMotion.HasValue)
        {
            TotalCaloriesBurnedFromMotion = profile.TotalCaloriesBurnedFromMotion.Value;
        }
        if (profile.TotalCaloriesBurnedWhileNotWorn.HasValue)
        {
            TotalCaloriesBurnedWhileNotWorn = profile.TotalCaloriesBurnedWhileNotWorn.Value;
        }
        if (profile.TotalCaloriesBurnedFromHR.HasValue)
        {
            TotalCaloriesBurnedFromHR = profile.TotalCaloriesBurnedFromHR.Value;
        }
        if (profile.TotalDistanceTravelledInM.HasValue)
        {
            TotalDistanceTravelledInM = profile.TotalDistanceTravelledInM.Value;
        }
        if (profile.TotalDistanceMeasuredByPedometerInM.HasValue)
        {
            TotalDistanceMeasuredByPedometerInM = profile.TotalDistanceMeasuredByPedometerInM.Value;
        }
        if (profile.TotalDistanceMeasuredByGPSInM.HasValue)
        {
            TotalDistanceMeasuredByGPSInM = profile.TotalDistanceMeasuredByGPSInM.Value;
        }
        if (profile.TotalStepsCounted.HasValue)
        {
            TotalStepsCounted = profile.TotalStepsCounted.Value;
        }
        if (profile.HRGain.HasValue)
        {
            HRGain = profile.HRGain.Value;
        }
        if (profile.HRRecoveryTime.HasValue)
        {
            HRRecoveryTime = profile.HRRecoveryTime.Value;
        }
        if (profile.HRIntensity.HasValue)
        {
            HRIntensity = profile.HRIntensity.Value;
        }
        if (profile.HRResponseTime.HasValue)
        {
            HRResponseTime = profile.HRResponseTime.Value;
        }
        if (profile.StrideLengthWhileWalking.HasValue)
        {
            StrideLengthWhileWalking = profile.StrideLengthWhileWalking.Value;
        }
        if (profile.StrideLengthWhileRunning.HasValue)
        {
            StrideLengthWhileRunning = profile.StrideLengthWhileRunning.Value;
        }
        if (profile.StrideLengthWhileJogging.HasValue)
        {
            StrideLengthWhileJogging = profile.StrideLengthWhileJogging.Value;
        }
        if (profile.RestingHR.HasValue && profile.RestingHR.Value <= 255)
        {
            RestingHeartRate = (byte)profile.RestingHR.Value;
        }
        if (profile.MaxHR.HasValue && profile.MaxHR.Value <= 255)
        {
            MaxHR = (byte)profile.MaxHR.Value;
        }
        RestingHROverride = profile.RestingHROverride;
        MaxHROverride = profile.MaxHROverride;
        ActivityClass = profile.ActivityClass;
    }

    internal CloudProfileDeviceLink ToCloudProfileDeviceLink()
    {
        CloudProfileDeviceLink cloudProfileDeviceLink = new CloudProfileDeviceLink();
        if (Header.LastKDKSyncUpdateOn.HasValue)
        {
            cloudProfileDeviceLink.LastKDKSyncUpdateOn = Header.LastKDKSyncUpdateOn.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
        cloudProfileDeviceLink.ApplicationSettings.ApplicationId = Guid.Empty;
        cloudProfileDeviceLink.ApplicationSettings.PairedDeviceId = ApplicationSettings.PairedDeviceId;
        cloudProfileDeviceLink.DeviceSettings.DeviceId = ApplicationSettings.PairedDeviceId;
        cloudProfileDeviceLink.DeviceSettings.SerialNumber = DeviceSettings.SerialNumber;
        return cloudProfileDeviceLink;
    }

    internal CloudProfile ToCloudProfile()
    {
        CloudProfile cloudProfile = new CloudProfile();
        cloudProfile.FirstName = FirstName;
        cloudProfile.LastName = LastName;
        cloudProfile.Height = Height;
        cloudProfile.Weight = Weight;
        cloudProfile.Gender = Gender;
        cloudProfile.Birthdate = Birthdate.ToString("yyyy-MM-dd");
        cloudProfile.EmailAddress = EmailAddress;
        cloudProfile.SmsAddress = SmsAddress;
        cloudProfile.ZipCode = ZipCode;
        cloudProfile.UserID = Header.UserID;
        cloudProfile.HasCompletedOOBE = HasCompletedOOBE;
        cloudProfile.RestingHR = (uint)Convert.ToInt32(RestingHeartRate);
        cloudProfile.RestingHROverride = RestingHROverride;
        cloudProfile.MaxHROverride = MaxHROverride;
        cloudProfile.ActivityClass = ActivityClass;
        if (Header.LastKDKSyncUpdateOn.HasValue)
        {
            cloudProfile.LastKDKSyncUpdateOn = Header.LastKDKSyncUpdateOn.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
        if (ApplicationSettings != null)
        {
            cloudProfile.ApplicationSettings = ApplicationSettings.ToCloudApplicationSettings();
        }
        if (DeviceSettings == null)
        {
            cloudProfile.DeviceSettings = new CloudDeviceSettings
            {
                DeviceProfileVersion = Version
            };
        }
        else
        {
            cloudProfile.DeviceSettings = DeviceSettings.ToCloudDeviceSettings(Version);
        }
        return cloudProfile;
    }

    private DateTime ToDateTime(string dateTime)
    {
        DateTime result = DateTime.MinValue;
        if (DateTime.TryParseExact(dateTime, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var result2))
        {
            result = result2;
        }
        return result;
    }

    private DateTimeOffset? ToDateTimeOffset(string dateTime)
    {
        if (DateTimeOffset.TryParseExact(dateTime, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var result))
        {
            return result;
        }
        return null;
    }

    public static int GetAppDataSerializedByteCount(DynamicAdminBandConstants constants)
    {
        return constants.BandProfileAppDataByteCount;
    }

    public static int GetFirmwareBytesSerializedByteCount()
    {
        return 282;
    }

    public static UserProfile DeserializeAppDataFromBand(ICargoReader reader, DynamicAdminBandConstants constants)
    {
        UserProfile userProfile = new UserProfile();
        userProfile.Header = UserProfileHeader.DeserializeFromBand(reader);
        userProfile.Birthdate = CargoFileTime.DeserializeFromBandAsDateTime(reader).Date;
        userProfile.Weight = reader.ReadUInt32();
        userProfile.Height = reader.ReadUInt16();
        userProfile.Gender = FWGenderByteToGender(reader.ReadByte());
        userProfile.DeviceSettings = new DeviceSettings
        {
            DeviceName = reader.ReadString(16),
            LocaleSettings = CargoLocaleSettings.DeserializeFromBand(reader),
            RunDisplayUnits = (RunMeasurementUnitType)reader.ReadByte(),
            TelemetryEnabled = Convert.ToBoolean(reader.ReadByte())
        };
        if (userProfile.Version > 1)
        {
            userProfile.HwagChangeTime = CargoFileTime.DeserializeFromBandAsDateTimeOffset(reader);
            userProfile.HwagChangeAgent = reader.ReadByte();
            userProfile.DeviceNameChangeTime = CargoFileTime.DeserializeFromBandAsDateTimeOffset(reader);
            userProfile.DeviceNameChangeAgent = reader.ReadByte();
            userProfile.LocaleSettingsChangeTime = CargoFileTime.DeserializeFromBandAsDateTimeOffset(reader);
            userProfile.LocaleSettingsChangeAgent = reader.ReadByte();
            userProfile.LanguageChangeTime = CargoFileTime.DeserializeFromBandAsDateTimeOffset(reader);
            userProfile.LanguageChangeAgent = reader.ReadByte();
            userProfile.MaxHR = reader.ReadByte();
        }
        userProfile.DeviceSettings.Reserved = reader.ReadExact(constants.BandProfileDeviceReservedBytes);
        return userProfile;
    }

    public static UserProfile DeserializeDeviceMasteredAppDataFromBand(ICargoReader reader, DynamicAdminBandConstants constants)
    {
        UserProfile obj = new UserProfile
        {
            DeviceSettings = new DeviceSettings(),
            Header = UserProfileHeader.DeserializeFromBand(reader)
        };
        reader.ReadExactAndDiscard(DeserializeDeviceMasteredAppDataFromBand_FastForward1);
        obj.DeviceSettings.LocaleSettings.DeserializeDeviceMasteredFieldsFromBand(reader, forExplicitSave: false);
        reader.ReadExactAndDiscard(2);
        if (obj.Header.Version > 1)
        {
            reader.ReadExactAndDiscard(DeserializeDeviceMasteredAppDataFromBand_FastForward3);
            reader.ReadExactAndDiscard(1);
        }
        reader.ReadExactAndDiscard(constants.BandProfileDeviceReservedBytes);
        return obj;
    }

    public bool DeserializeAndOverwriteDeviceMasteredAppDataFromBand(ICargoReader reader, DynamicAdminBandConstants constants, bool forExplicitSave)
    {
        bool result = false;
        UserProfileHeader userProfileHeader = UserProfileHeader.DeserializeFromBand(reader);
        reader.ReadExactAndDiscard(DeserializeDeviceMasteredAppDataFromBand_FastForward1);
        DeviceSettings.LocaleSettings.DeserializeDeviceMasteredFieldsFromBand(reader, forExplicitSave);
        reader.ReadExactAndDiscard(2);
        if (userProfileHeader.Version > 1)
        {
            reader.ReadExactAndDiscard(DeserializeDeviceMasteredAppDataFromBand_FastForward3);
            byte b = reader.ReadByte();
            if (userProfileHeader.Version > 2 && b != MaxHR)
            {
                result = true;
            }
        }
        reader.ReadExactAndDiscard(constants.BandProfileDeviceReservedBytes);
        return result;
    }

    public void SerializeAppDataToBand(ICargoWriter writer, DynamicAdminBandConstants constants)
    {
        Header.SerializeToBand(writer, constants);
        CargoFileTime.SerializeToBandFromDateTime(writer, Birthdate);
        writer.WriteUInt32(Weight);
        writer.WriteUInt16(Height);
        writer.WriteByte(GenderToFWGenderByte(Gender));
        writer.WriteStringWithPadding(DeviceSettings.DeviceName, 16);
        DeviceSettings.LocaleSettings.SerializeToBand(writer);
        writer.WriteByte(Convert.ToByte(DeviceSettings.RunDisplayUnits));
        writer.WriteByte(Convert.ToByte(DeviceSettings.TelemetryEnabled));
        if (constants.BandProfileAppDataVersion == 2)
        {
            CargoFileTime.SerializeToBandFromDateTimeOffset(writer, HwagChangeTime);
            writer.WriteByte(HwagChangeAgent);
            CargoFileTime.SerializeToBandFromDateTimeOffset(writer, DeviceNameChangeTime);
            writer.WriteByte(DeviceNameChangeAgent);
            CargoFileTime.SerializeToBandFromDateTimeOffset(writer, LocaleSettingsChangeTime);
            writer.WriteByte(LocaleSettingsChangeAgent);
            CargoFileTime.SerializeToBandFromDateTimeOffset(writer, LanguageChangeTime);
            writer.WriteByte(LanguageChangeAgent);
            writer.WriteByte(MaxHR);
        }
        int num = 0;
        int bandProfileDeviceReservedBytes = constants.BandProfileDeviceReservedBytes;
        if (DeviceSettings.Reserved != null)
        {
            writer.Write(DeviceSettings.Reserved);
            num += DeviceSettings.Reserved.Length;
        }
        if (num < bandProfileDeviceReservedBytes)
        {
            writer.WriteByte(0, bandProfileDeviceReservedBytes - num);
        }
    }

    public void SerializeFirmwareBytesToBand(ICargoWriter writer, DynamicAdminBandConstants constants)
    {
        Header.SerializeToBand(writer, constants);
        int num = 0;
        int num2 = 256;
        if (DeviceSettings.FirmwareByteArray != null)
        {
            writer.Write(DeviceSettings.FirmwareByteArray);
            num += DeviceSettings.FirmwareByteArray.Length;
        }
        if (num < num2)
        {
            writer.WriteByte(0, num2 - num);
        }
    }

    private static Gender FWGenderByteToGender(byte genderByte)
    {
        return genderByte switch
        {
            0 => Gender.Male, 
            _ => Gender.Female, 
        };
    }

    private static byte GenderToFWGenderByte(Gender gender)
    {
        return gender switch
        {
            Gender.Male => 0, 
            _ => 1, 
        };
    }
}
