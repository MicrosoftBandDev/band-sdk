// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.UserProfileHeader
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  internal sealed class UserProfileHeader
  {
    private static readonly int serializedByteCount = 2 + CargoFileTime.GetSerializedByteCount() + 16;

    public ushort Version { get; set; }

    public DateTimeOffset? LastKDKSyncUpdateOn { get; set; }

    public Guid UserID { get; set; }

    public static int GetSerializedByteCount() => UserProfileHeader.serializedByteCount;

    public static UserProfileHeader DeserializeFromBand(ICargoReader reader) => new UserProfileHeader()
    {
      Version = reader.ReadUInt16(),
      LastKDKSyncUpdateOn = new DateTimeOffset?(CargoFileTime.DeserializeFromBandAsDateTimeOffset(reader)),
      UserID = reader.ReadGuid()
    };

    public void SerializeToBand(ICargoWriter writer, DynamicAdminBandConstants constants)
    {
      writer.WriteUInt16(constants.BandProfileAppDataVersion);
      if (this.LastKDKSyncUpdateOn.HasValue)
        CargoFileTime.SerializeToBandFromDateTimeOffset(writer, this.LastKDKSyncUpdateOn.Value);
      else
        CargoFileTime.SerializeToBandFromDateTimeOffset(writer, DateTimeOffset.MinValue);
      writer.WriteGuid(this.UserID);
    }
  }
}
