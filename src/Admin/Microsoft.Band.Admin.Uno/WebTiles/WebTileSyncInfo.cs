using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin.WebTiles;

[DataContract]
internal class WebTileSyncInfo
{
    private const string KeyValueSeparator = "__*$$/_KSeparatorV/_$$*__";

    private const string MappingsSeparator = "__*$$/_MSeparatorS/_$$*__";

    private DateTimeOffset lastSyncTime;

    private List<Dictionary<string, string>> lastSyncMappings = new List<Dictionary<string, string>>();

    public DateTimeOffset LastSyncTime
    {
        get
        {
            return lastSyncTime;
        }
        set
        {
            lastSyncTime = value;
        }
    }

    public List<Dictionary<string, string>> LastSyncMappings
    {
        get
        {
            return lastSyncMappings;
        }
        set
        {
            lastSyncMappings = value ?? new List<Dictionary<string, string>>();
        }
    }

    [DataMember]
    private string LastSyncTimeSerialized
    {
        get
        {
            return lastSyncTime.ToString("o");
        }
        set
        {
            if (DateTimeOffset.TryParse(value, out var result))
            {
                lastSyncTime = result;
            }
            else
            {
                lastSyncTime = DateTimeOffset.MinValue;
            }
        }
    }

    [DataMember]
    private string LastSyncMappingsSerialized
    {
        get
        {
            return string.Join("__*$$/_MSeparatorS/_$$*__", lastSyncMappings.Select((Dictionary<string, string> m) => string.Join("__*$$/_KSeparatorV/_$$*__", MappingAsStrings(m))));
        }
        set
        {
            lastSyncMappings.Clear();
            if (string.IsNullOrEmpty(value))
            {
                return;
            }
            string[] array = value.Split(new string[1] { "__*$$/_MSeparatorS/_$$*__" }, StringSplitOptions.None);
            for (int i = 0; i < array.Length; i++)
            {
                string[] array2 = array[i].Split(new string[1] { "__*$$/_KSeparatorV/_$$*__" }, StringSplitOptions.None);
                if (array2.Length % 2 != 0)
                {
                    lastSyncMappings.Clear();
                    break;
                }
                Dictionary<string, string> dictionary = new Dictionary<string, string>(array2.Length / 2);
                for (int j = 0; j < array2.Length; j += 2)
                {
                    dictionary.Add(array2[j], array2[j + 1]);
                }
                lastSyncMappings.Add(dictionary);
            }
        }
    }

    public bool HasSameLastSyncMappings(List<Dictionary<string, string>> other)
    {
        return lastSyncMappings.SelectMany((Dictionary<string, string> m) => MappingAsStrings(m)).SequenceEqual(other.SelectMany((Dictionary<string, string> m) => MappingAsStrings(m)));
    }

    private static IEnumerable<string> MappingAsStrings(Dictionary<string, string> mapping)
    {
        foreach (KeyValuePair<string, string> pair in mapping)
        {
            yield return pair.Key;
            yield return pair.Value;
        }
    }
}
