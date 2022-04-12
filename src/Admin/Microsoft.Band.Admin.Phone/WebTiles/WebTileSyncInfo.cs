// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.WebTileSyncInfo
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin.WebTiles
{
  [DataContract]
  internal class WebTileSyncInfo
  {
    private const string KeyValueSeparator = "__*$$/_KSeparatorV/_$$*__";
    private const string MappingsSeparator = "__*$$/_MSeparatorS/_$$*__";
    private DateTimeOffset lastSyncTime;
    private List<Dictionary<string, string>> lastSyncMappings = new List<Dictionary<string, string>>();

    public DateTimeOffset LastSyncTime
    {
      get => this.lastSyncTime;
      set => this.lastSyncTime = value;
    }

    public List<Dictionary<string, string>> LastSyncMappings
    {
      get => this.lastSyncMappings;
      set => this.lastSyncMappings = value ?? new List<Dictionary<string, string>>();
    }

    public bool HasSameLastSyncMappings(List<Dictionary<string, string>> other) => this.lastSyncMappings.SelectMany<Dictionary<string, string>, string>((Func<Dictionary<string, string>, IEnumerable<string>>) (m => WebTileSyncInfo.MappingAsStrings(m))).SequenceEqual<string>(other.SelectMany<Dictionary<string, string>, string>((Func<Dictionary<string, string>, IEnumerable<string>>) (m => WebTileSyncInfo.MappingAsStrings(m))));

    [DataMember]
    private string LastSyncTimeSerialized
    {
      get => this.lastSyncTime.ToString("o");
      set
      {
        DateTimeOffset result;
        if (DateTimeOffset.TryParse(value, out result))
          this.lastSyncTime = result;
        else
          this.lastSyncTime = DateTimeOffset.MinValue;
      }
    }

    [DataMember]
    private string LastSyncMappingsSerialized
    {
      get => string.Join("__*$$/_MSeparatorS/_$$*__", this.lastSyncMappings.Select<Dictionary<string, string>, string>((Func<Dictionary<string, string>, string>) (m => string.Join("__*$$/_KSeparatorV/_$$*__", WebTileSyncInfo.MappingAsStrings(m)))));
      set
      {
        this.lastSyncMappings.Clear();
        if (string.IsNullOrEmpty(value))
          return;
        string str1 = value;
        string[] separator1 = new string[1]
        {
          "__*$$/_MSeparatorS/_$$*__"
        };
        foreach (string str2 in str1.Split(separator1, StringSplitOptions.None))
        {
          string[] separator2 = new string[1]
          {
            "__*$$/_KSeparatorV/_$$*__"
          };
          string[] strArray = str2.Split(separator2, StringSplitOptions.None);
          if (strArray.Length % 2 != 0)
          {
            this.lastSyncMappings.Clear();
            break;
          }
          Dictionary<string, string> dictionary = new Dictionary<string, string>(strArray.Length / 2);
          for (int index = 0; index < strArray.Length; index += 2)
            dictionary.Add(strArray[index], strArray[index + 1]);
          this.lastSyncMappings.Add(dictionary);
        }
      }
    }

    private static IEnumerable<string> MappingAsStrings(
      Dictionary<string, string> mapping)
    {
      foreach (KeyValuePair<string, string> keyValuePair in mapping)
      {
        KeyValuePair<string, string> pair = keyValuePair;
        yield return pair.Key;
        yield return pair.Value;
        pair = new KeyValuePair<string, string>();
      }
    }
  }
}
