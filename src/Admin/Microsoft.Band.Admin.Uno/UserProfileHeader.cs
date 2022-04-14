using System;

namespace Microsoft.Band.Admin;

internal sealed class UserProfileHeader
{
    private static readonly int serializedByteCount = 2 + CargoFileTime.GetSerializedByteCount() + 16;

    public ushort Version { get; set; }

    public DateTimeOffset? LastKDKSyncUpdateOn { get; set; }

    public Guid UserID { get; set; }

    public static int GetSerializedByteCount()
    {
        return serializedByteCount;
    }

    public static UserProfileHeader DeserializeFromBand(ICargoReader reader)
    {
        return new UserProfileHeader
        {
            Version = reader.ReadUInt16(),
            LastKDKSyncUpdateOn = CargoFileTime.DeserializeFromBandAsDateTimeOffset(reader),
            UserID = reader.ReadGuid()
        };
    }

    public void SerializeToBand(ICargoWriter writer, DynamicAdminBandConstants constants)
    {
        writer.WriteUInt16(constants.BandProfileAppDataVersion);
        if (LastKDKSyncUpdateOn.HasValue)
        {
            CargoFileTime.SerializeToBandFromDateTimeOffset(writer, LastKDKSyncUpdateOn.Value);
        }
        else
        {
            CargoFileTime.SerializeToBandFromDateTimeOffset(writer, DateTimeOffset.MinValue);
        }
        writer.WriteGuid(UserID);
    }
}
