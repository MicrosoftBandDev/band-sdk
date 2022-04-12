// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.UserProfile
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Band.Admin
{
  internal sealed class UserProfile : IUserProfile
  {
    private static readonly string[] DateTimeFormats = new string[11]
    {
      "M/d/yyyy",
      "MM/dd/yyyy",
      "yyyy-MM-dd",
      "yyyy-MM-ddTHH:mm:sszzz",
      "yyyy-MM-ddTHH:mm:ss.fzzz",
      "yyyy-MM-ddTHH:mm:ss.ffzzz",
      "yyyy-MM-ddTHH:mm:ss.fffzzz",
      "yyyy-MM-ddTHH:mm:ssZ",
      "yyyy-MM-ddTHH:mm:ss.fZ",
      "yyyy-MM-ddTHH:mm:ss.ffZ",
      "yyyy-MM-ddTHH:mm:ss.fffZ"
    };
    private static readonly int DeserializeDeviceMasteredAppDataFromBand_FastForward1 = CargoFileTime.GetSerializedByteCount() + 4 + 2 + 1 + 32;
    private const int DeserializeDeviceMasteredAppDataFromBand_FastForward2 = 2;
    private static readonly int DeserializeDeviceMasteredAppDataFromBand_FastForward3 = CargoFileTime.GetSerializedByteCount() + 1 + CargoFileTime.GetSerializedByteCount() + 1 + CargoFileTime.GetSerializedByteCount() + 1 + CargoFileTime.GetSerializedByteCount() + 1;

    private UserProfile()
    {
    }

    internal UserProfile(CloudProfile profile, DynamicAdminBandConstants constants)
    {
      if (profile == null)
        throw new ArgumentNullException(nameof (profile));
      this.Header = new UserProfileHeader();
      if (constants != null)
        this.Header.Version = constants.BandProfileAppDataVersion;
      if (profile.UserID.HasValue)
        this.Header.UserID = profile.UserID.Value;
      this.EmailAddress = profile.EmailAddress;
      this.ZipCode = profile.ZipCode;
      this.SmsAddress = profile.SmsAddress;
      this.FirstName = profile.FirstName;
      this.LastName = profile.LastName;
      if (profile.Height.HasValue)
        this.Height = (ushort) profile.Height.Value;
      if (profile.Weight.HasValue)
        this.Weight = profile.Weight.Value;
      this.Gender = profile.Gender;
      this.Birthdate = this.ToDateTime(profile.Birthdate).Date;
      this.HasCompletedOOBE = profile.HasCompletedOOBE.HasValue && profile.HasCompletedOOBE.Value;
      this.CreatedOn = this.ToDateTimeOffset(profile.CreatedOn);
      this.Header.LastKDKSyncUpdateOn = this.ToDateTimeOffset(profile.LastKDKSyncUpdateOn);
      this.ApplicationSettings = profile.ApplicationSettings.ToApplicationSettings();
      this.DeviceSettings = profile.DeviceSettings.ToDeviceSettings();
      this.AllDeviceSettings = profile.AllDeviceSettings.ToAllDeviceSettings();
      DateTimeOffset? dateTimeOffset = this.ToDateTimeOffset(profile.LastModifiedOn);
      this.LastModifiedOn = dateTimeOffset ?? DateTimeOffset.MinValue;
      dateTimeOffset = this.ToDateTimeOffset(profile.LastUserUpdateOn);
      this.LastUserUpdateOn = dateTimeOffset ?? DateTimeOffset.MinValue;
      this.EndPoint = profile.EndPoint;
      this.FUSEndPoint = profile.FUSEndPoint;
      if (profile.TotalCaloriesBurnedFromMotion.HasValue)
        this.TotalCaloriesBurnedFromMotion = profile.TotalCaloriesBurnedFromMotion.Value;
      if (profile.TotalCaloriesBurnedWhileNotWorn.HasValue)
        this.TotalCaloriesBurnedWhileNotWorn = profile.TotalCaloriesBurnedWhileNotWorn.Value;
      if (profile.TotalCaloriesBurnedFromHR.HasValue)
        this.TotalCaloriesBurnedFromHR = profile.TotalCaloriesBurnedFromHR.Value;
      if (profile.TotalDistanceTravelledInM.HasValue)
        this.TotalDistanceTravelledInM = profile.TotalDistanceTravelledInM.Value;
      if (profile.TotalDistanceMeasuredByPedometerInM.HasValue)
        this.TotalDistanceMeasuredByPedometerInM = profile.TotalDistanceMeasuredByPedometerInM.Value;
      if (profile.TotalDistanceMeasuredByGPSInM.HasValue)
        this.TotalDistanceMeasuredByGPSInM = profile.TotalDistanceMeasuredByGPSInM.Value;
      if (profile.TotalStepsCounted.HasValue)
        this.TotalStepsCounted = profile.TotalStepsCounted.Value;
      if (profile.HRGain.HasValue)
        this.HRGain = profile.HRGain.Value;
      if (profile.HRRecoveryTime.HasValue)
        this.HRRecoveryTime = profile.HRRecoveryTime.Value;
      if (profile.HRIntensity.HasValue)
        this.HRIntensity = profile.HRIntensity.Value;
      if (profile.HRResponseTime.HasValue)
        this.HRResponseTime = profile.HRResponseTime.Value;
      if (profile.StrideLengthWhileWalking.HasValue)
        this.StrideLengthWhileWalking = profile.StrideLengthWhileWalking.Value;
      if (profile.StrideLengthWhileRunning.HasValue)
        this.StrideLengthWhileRunning = profile.StrideLengthWhileRunning.Value;
      if (profile.StrideLengthWhileJogging.HasValue)
        this.StrideLengthWhileJogging = profile.StrideLengthWhileJogging.Value;
      if (profile.RestingHR.HasValue && profile.RestingHR.Value <= (uint) byte.MaxValue)
        this.RestingHeartRate = (byte) profile.RestingHR.Value;
      if (profile.MaxHR.HasValue && profile.MaxHR.Value <= (uint) byte.MaxValue)
        this.MaxHR = (byte) profile.MaxHR.Value;
      this.RestingHROverride = profile.RestingHROverride;
      this.MaxHROverride = profile.MaxHROverride;
      this.ActivityClass = profile.ActivityClass;
    }

    public UserProfileHeader Header { get; private set; }

    public ushort Version
    {
      get => this.Header.Version;
      set => this.Header.Version = value;
    }

    public DateTimeOffset? CreatedOn { get; set; }

    public DateTimeOffset? LastKDKSyncUpdateOn
    {
      get => this.Header.LastKDKSyncUpdateOn;
      set => this.Header.LastKDKSyncUpdateOn = value;
    }

    public Guid UserID
    {
      get => this.Header.UserID;
      set => this.Header.UserID = value;
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

    internal CloudProfileDeviceLink ToCloudProfileDeviceLink()
    {
      CloudProfileDeviceLink profileDeviceLink1 = new CloudProfileDeviceLink();
      if (this.Header.LastKDKSyncUpdateOn.HasValue)
      {
        CloudProfileDeviceLink profileDeviceLink2 = profileDeviceLink1;
        DateTimeOffset universalTime = this.Header.LastKDKSyncUpdateOn.Value;
        universalTime = universalTime.ToUniversalTime();
        string str = universalTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        profileDeviceLink2.LastKDKSyncUpdateOn = str;
      }
      profileDeviceLink1.ApplicationSettings.ApplicationId = Guid.Empty;
      profileDeviceLink1.ApplicationSettings.PairedDeviceId = new Guid?(this.ApplicationSettings.PairedDeviceId);
      profileDeviceLink1.DeviceSettings.DeviceId = new Guid?(this.ApplicationSettings.PairedDeviceId);
      profileDeviceLink1.DeviceSettings.SerialNumber = this.DeviceSettings.SerialNumber;
      return profileDeviceLink1;
    }

    internal CloudProfile ToCloudProfile()
    {
      CloudProfile cloudProfile = new CloudProfile();
      cloudProfile.FirstName = this.FirstName;
      cloudProfile.LastName = this.LastName;
      cloudProfile.Height = new uint?((uint) this.Height);
      cloudProfile.Weight = new uint?(this.Weight);
      cloudProfile.Gender = this.Gender;
      cloudProfile.Birthdate = this.Birthdate.ToString("yyyy-MM-dd");
      cloudProfile.EmailAddress = this.EmailAddress;
      cloudProfile.SmsAddress = this.SmsAddress;
      cloudProfile.ZipCode = this.ZipCode;
      cloudProfile.UserID = new Guid?(this.Header.UserID);
      cloudProfile.HasCompletedOOBE = new bool?(this.HasCompletedOOBE);
      cloudProfile.RestingHR = new uint?((uint) Convert.ToInt32(this.RestingHeartRate));
      cloudProfile.RestingHROverride = this.RestingHROverride;
      cloudProfile.MaxHROverride = this.MaxHROverride;
      cloudProfile.ActivityClass = this.ActivityClass;
      if (this.Header.LastKDKSyncUpdateOn.HasValue)
        cloudProfile.LastKDKSyncUpdateOn = this.Header.LastKDKSyncUpdateOn.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
      if (this.ApplicationSettings != null)
        cloudProfile.ApplicationSettings = this.ApplicationSettings.ToCloudApplicationSettings();
      if (this.DeviceSettings == null)
        cloudProfile.DeviceSettings = new CloudDeviceSettings()
        {
          DeviceProfileVersion = (int) this.Version
        };
      else
        cloudProfile.DeviceSettings = this.DeviceSettings.ToCloudDeviceSettings((int) this.Version);
      return cloudProfile;
    }

    private DateTime ToDateTime(string dateTime)
    {
      DateTime dateTime1 = DateTime.MinValue;
      DateTime result;
      if (DateTime.TryParseExact(dateTime, UserProfile.DateTimeFormats, (IFormatProvider) CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out result))
        dateTime1 = result;
      return dateTime1;
    }

    private DateTimeOffset? ToDateTimeOffset(string dateTime)
    {
      DateTimeOffset result;
      return DateTimeOffset.TryParseExact(dateTime, UserProfile.DateTimeFormats, (IFormatProvider) CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out result) ? new DateTimeOffset?(result) : new DateTimeOffset?();
    }

    public static int GetAppDataSerializedByteCount(DynamicAdminBandConstants constants) => (int) constants.BandProfileAppDataByteCount;

    public static int GetFirmwareBytesSerializedByteCount() => 282;

    public static UserProfile DeserializeAppDataFromBand(
      ICargoReader reader,
      DynamicAdminBandConstants constants)
    {
      UserProfile userProfile = new UserProfile();
      userProfile.Header = UserProfileHeader.DeserializeFromBand(reader);
      userProfile.Birthdate = CargoFileTime.DeserializeFromBandAsDateTime(reader).Date;
      userProfile.Weight = reader.ReadUInt32();
      userProfile.Height = reader.ReadUInt16();
      userProfile.Gender = UserProfile.FWGenderByteToGender(reader.ReadByte());
      userProfile.DeviceSettings = new DeviceSettings()
      {
        DeviceName = reader.ReadString(16),
        LocaleSettings = CargoLocaleSettings.DeserializeFromBand(reader),
        RunDisplayUnits = (RunMeasurementUnitType) reader.ReadByte(),
        TelemetryEnabled = Convert.ToBoolean(reader.ReadByte())
      };
      if (userProfile.Version > (ushort) 1)
      {
        userProfile.HwagChangeTime = new DateTimeOffset?(CargoFileTime.DeserializeFromBandAsDateTimeOffset(reader));
        userProfile.HwagChangeAgent = reader.ReadByte();
        userProfile.DeviceNameChangeTime = new DateTimeOffset?(CargoFileTime.DeserializeFromBandAsDateTimeOffset(reader));
        userProfile.DeviceNameChangeAgent = reader.ReadByte();
        userProfile.LocaleSettingsChangeTime = new DateTimeOffset?(CargoFileTime.DeserializeFromBandAsDateTimeOffset(reader));
        userProfile.LocaleSettingsChangeAgent = reader.ReadByte();
        userProfile.LanguageChangeTime = new DateTimeOffset?(CargoFileTime.DeserializeFromBandAsDateTimeOffset(reader));
        userProfile.LanguageChangeAgent = reader.ReadByte();
        userProfile.MaxHR = reader.ReadByte();
      }
      userProfile.DeviceSettings.Reserved = reader.ReadExact((int) constants.BandProfileDeviceReservedBytes);
      return userProfile;
    }

    public static UserProfile DeserializeDeviceMasteredAppDataFromBand(
      ICargoReader reader,
      DynamicAdminBandConstants constants)
    {
      UserProfile userProfile = new UserProfile();
      userProfile.DeviceSettings = new DeviceSettings();
      userProfile.Header = UserProfileHeader.DeserializeFromBand(reader);
      reader.ReadExactAndDiscard(UserProfile.DeserializeDeviceMasteredAppDataFromBand_FastForward1);
      userProfile.DeviceSettings.LocaleSettings.DeserializeDeviceMasteredFieldsFromBand(reader, false);
      reader.ReadExactAndDiscard(2);
      if (userProfile.Header.Version > (ushort) 1)
      {
        reader.ReadExactAndDiscard(UserProfile.DeserializeDeviceMasteredAppDataFromBand_FastForward3);
        reader.ReadExactAndDiscard(1);
      }
      reader.ReadExactAndDiscard((int) constants.BandProfileDeviceReservedBytes);
      return userProfile;
    }

    public bool DeserializeAndOverwriteDeviceMasteredAppDataFromBand(
      ICargoReader reader,
      DynamicAdminBandConstants constants,
      bool forExplicitSave)
    {
      bool flag = false;
      UserProfileHeader userProfileHeader = UserProfileHeader.DeserializeFromBand(reader);
      reader.ReadExactAndDiscard(UserProfile.DeserializeDeviceMasteredAppDataFromBand_FastForward1);
      this.DeviceSettings.LocaleSettings.DeserializeDeviceMasteredFieldsFromBand(reader, forExplicitSave);
      reader.ReadExactAndDiscard(2);
      if (userProfileHeader.Version > (ushort) 1)
      {
        reader.ReadExactAndDiscard(UserProfile.DeserializeDeviceMasteredAppDataFromBand_FastForward3);
        byte num = reader.ReadByte();
        if (userProfileHeader.Version > (ushort) 2 && (int) num != (int) this.MaxHR)
          flag = true;
      }
      reader.ReadExactAndDiscard((int) constants.BandProfileDeviceReservedBytes);
      return flag;
    }

    public void SerializeAppDataToBand(ICargoWriter writer, DynamicAdminBandConstants constants)
    {
      this.Header.SerializeToBand(writer, constants);
      CargoFileTime.SerializeToBandFromDateTime(writer, this.Birthdate);
      writer.WriteUInt32(this.Weight);
      writer.WriteUInt16(this.Height);
      writer.WriteByte(UserProfile.GenderToFWGenderByte(this.Gender));
      writer.WriteStringWithPadding(this.DeviceSettings.DeviceName, 16);
      this.DeviceSettings.LocaleSettings.SerializeToBand(writer);
      writer.WriteByte(Convert.ToByte((object) this.DeviceSettings.RunDisplayUnits));
      writer.WriteByte(Convert.ToByte(this.DeviceSettings.TelemetryEnabled));
      if (constants.BandProfileAppDataVersion == (ushort) 2)
      {
        CargoFileTime.SerializeToBandFromDateTimeOffset(writer, this.HwagChangeTime);
        writer.WriteByte(this.HwagChangeAgent);
        CargoFileTime.SerializeToBandFromDateTimeOffset(writer, this.DeviceNameChangeTime);
        writer.WriteByte(this.DeviceNameChangeAgent);
        CargoFileTime.SerializeToBandFromDateTimeOffset(writer, this.LocaleSettingsChangeTime);
        writer.WriteByte(this.LocaleSettingsChangeAgent);
        CargoFileTime.SerializeToBandFromDateTimeOffset(writer, this.LanguageChangeTime);
        writer.WriteByte(this.LanguageChangeAgent);
        writer.WriteByte(this.MaxHR);
      }
      int num = 0;
      int deviceReservedBytes = (int) constants.BandProfileDeviceReservedBytes;
      if (this.DeviceSettings.Reserved != null)
      {
        writer.Write(this.DeviceSettings.Reserved);
        num += this.DeviceSettings.Reserved.Length;
      }
      if (num >= deviceReservedBytes)
        return;
      writer.WriteByte((byte) 0, deviceReservedBytes - num);
    }

    public void SerializeFirmwareBytesToBand(
      ICargoWriter writer,
      DynamicAdminBandConstants constants)
    {
      this.Header.SerializeToBand(writer, constants);
      int num1 = 0;
      int num2 = 256;
      if (this.DeviceSettings.FirmwareByteArray != null)
      {
        writer.Write(this.DeviceSettings.FirmwareByteArray);
        num1 += this.DeviceSettings.FirmwareByteArray.Length;
      }
      if (num1 >= num2)
        return;
      writer.WriteByte((byte) 0, num2 - num1);
    }

    private static Gender FWGenderByteToGender(byte genderByte)
    {
      if (genderByte == (byte) 0)
        return Gender.Male;
      if (genderByte == (byte) 1)
        ;
      return Gender.Female;
    }

    private static byte GenderToFWGenderByte(Gender gender)
    {
      if (gender == Gender.Male)
        return 0;
      if (gender == Gender.Female)
        ;
      return 1;
    }
  }
}
