// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.WebTileIconCondition
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin.WebTiles
{
  [DataContract]
  public class WebTileIconCondition
  {
    private string condition;
    private string iconName;

    [DataMember(IsRequired = true, Name = "condition")]
    public string Condition
    {
      get => this.condition;
      set => this.condition = value;
    }

    [DataMember(IsRequired = true, Name = "icon")]
    public string IconName
    {
      get => this.iconName;
      set
      {
        if (value == null)
          throw new ArgumentNullException(nameof (value));
        this.iconName = !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException(CommonSR.WTIconNameCannotBeEmpty, nameof (value));
      }
    }
  }
}
