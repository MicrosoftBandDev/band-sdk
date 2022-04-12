using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin;

[DataContract]
internal sealed class EphemerisCloudVersion
{
    private static readonly string[] DateTimeFormats = new string[9] { "o", "yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-ddTHH:mm:ss.fzzz", "yyyy-MM-ddTHH:mm:ss.ffzzz", "yyyy-MM-ddTHH:mm:ss.fffzzz", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.fZ", "yyyy-MM-ddTHH:mm:ss.ffZ", "yyyy-MM-ddTHH:mm:ss.fffZ" };

    private DateTime? lastFileUpdatedTime;

    [DataMember]
    public string EphemerisFileHeaderDataUrl { get; set; }

    [DataMember]
    public string EphemerisProcessedFileDataUrl { get; set; }

    [DataMember(Name = "LastFileUpdatedTime")]
    private string LastFileUpdatedTimeSerialized
    {
        get
        {
            return GetSerializedDateTimeValue(lastFileUpdatedTime);
        }
        set
        {
            SetDeserializedDateTimeValue(value, out lastFileUpdatedTime);
        }
    }

    public DateTime? LastFileUpdatedTime
    {
        get
        {
            return lastFileUpdatedTime;
        }
        set
        {
            lastFileUpdatedTime = value;
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
