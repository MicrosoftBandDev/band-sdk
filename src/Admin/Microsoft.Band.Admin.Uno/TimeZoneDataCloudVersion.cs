using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class TimeZoneDataCloudVersion
{
    private static readonly string[] DateTimeFormats = new string[9] { "o", "yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-ddTHH:mm:ss.fzzz", "yyyy-MM-ddTHH:mm:ss.ffzzz", "yyyy-MM-ddTHH:mm:ss.fffzzz", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.fZ", "yyyy-MM-ddTHH:mm:ss.ffZ", "yyyy-MM-ddTHH:mm:ss.fffZ" };

    private DateTime? lastModifiedDateTime;

    private DateTime? lastModifiedDateTimeDevice;

    private DateTime? lastCloudCheckDateTime;

    [DataMember]
    internal string Url { get; set; }

    [DataMember(Name = "LastModifiedDateTime")]
    private string LastModifiedDateTimeSerialized
    {
        get
        {
            return GetSerializedDateTimeValue(lastModifiedDateTime);
        }
        set
        {
            SetDeserializedDateTimeValue(value, out lastModifiedDateTime);
        }
    }

    public DateTime? LastModifiedDateTime
    {
        get
        {
            return lastModifiedDateTime;
        }
        set
        {
            lastModifiedDateTime = value;
        }
    }

    [DataMember(Name = "LastModifiedDateTimeDevice")]
    private string LastModifiedDateTimeDeviceSerialized
    {
        get
        {
            return GetSerializedDateTimeValue(lastModifiedDateTimeDevice);
        }
        set
        {
            SetDeserializedDateTimeValue(value, out lastModifiedDateTimeDevice);
        }
    }

    public DateTime? LastModifiedDateTimeDevice
    {
        get
        {
            return lastModifiedDateTimeDevice;
        }
        set
        {
            lastModifiedDateTimeDevice = value;
        }
    }

    [DataMember(Name = "LastCloudCheckDateTime")]
    private string LastCloudCheckDateTimeSerialized
    {
        get
        {
            return GetSerializedDateTimeValue(lastCloudCheckDateTime);
        }
        set
        {
            SetDeserializedDateTimeValue(value, out lastCloudCheckDateTime);
        }
    }

    public DateTime? LastCloudCheckDateTime
    {
        get
        {
            return lastCloudCheckDateTime;
        }
        set
        {
            lastCloudCheckDateTime = value;
        }
    }

    [DataMember(Name = "Language")]
    public LocaleLanguage Language { get; set; }

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
