// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.WebTileIconBinding
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin.WebTiles
{
  [DataContract]
  public class WebTileIconBinding
  {
    private short elementId;
    private WebTileIconCondition[] conditions;
    private WebTilePropertyValidator validator = new WebTilePropertyValidator();

    public WebTilePropertyValidator Validator => this.validator;

    public bool AllowInvalidValues
    {
      get => this.validator.AllowInvalidValues;
      set => this.validator.AllowInvalidValues = value;
    }

    public Dictionary<string, string> PropertyErrors => this.validator.PropertyErrors;

    [DataMember(IsRequired = true, Name = "elementId")]
    public short ElementId
    {
      get => this.elementId;
      set => this.elementId = value;
    }

    [DataMember(IsRequired = true, Name = "conditions")]
    public WebTileIconCondition[] Conditions
    {
      get => this.conditions;
      set => this.conditions = value;
    }
  }
}
