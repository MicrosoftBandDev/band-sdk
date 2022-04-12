using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class DeviceFileSyncTimeInfo
{
    private static readonly string[] DateTimeFormats = new string[9] { "o", "yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-ddTHH:mm:ss.fzzz", "yyyy-MM-ddTHH:mm:ss.ffzzz", "yyyy-MM-ddTHH:mm:ss.fffzzz", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.fZ", "yyyy-MM-ddTHH:mm:ss.ffZ", "yyyy-MM-ddTHH:mm:ss.fffZ" };

    private DateTime? lastDeviceFileDownloadAttemptTime;

    [DataMember(Name = "LastDeviceFileDownloadAttemptTime")]
    private string LastDeviceFileDownloadAttemptTimeSerialized
    {
        get
        {
            return GetSerializedDateTimeValue(lastDeviceFileDownloadAttemptTime);
        }
        set
        {
            SetDeserializedDateTimeValue(value, out lastDeviceFileDownloadAttemptTime);
        }
    }

    public DateTime? LastDeviceFileDownloadAttemptTime
    {
        get
        {
            return lastDeviceFileDownloadAttemptTime;
        }
        set
        {
            lastDeviceFileDownloadAttemptTime = value;
        }
    }

    private string GetSerializedDateTimeValue(DateTime? deserialized)
    {
        if (!deserialized.HasValue)
        {
            return null;
        }
        return deserialized.Value.ToString(DateTimeFormats[0]);
    }

    private void SetDeserializedDateTimeValue(string serialized, out DateTime? deserialized)
    {
        if (DateTime.TryParseExact(serialized, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var result))
        {
            deserialized = result;
        }
        else
        {
            deserialized = null;
        }
    }
}
