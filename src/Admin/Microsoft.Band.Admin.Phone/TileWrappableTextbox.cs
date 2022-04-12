// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.TileWrappableTextbox
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  internal sealed class TileWrappableTextbox : 
    TilePageElement,
    ITileWrappableTextbox,
    ITilePageElement
  {
    private string textboxValue;

    internal TileWrappableTextbox(ushort elementId, string textboxValue)
      : base(elementId)
    {
      this.TextboxValue = textboxValue;
    }

    public string TextboxValue
    {
      get => this.textboxValue;
      set => this.textboxValue = value != null ? value : throw new ArgumentNullException(nameof (value));
    }

    internal override ushort ElementType => 3002;
  }
}
