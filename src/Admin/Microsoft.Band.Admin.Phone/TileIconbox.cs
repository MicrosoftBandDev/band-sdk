// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.TileIconbox
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using System;

namespace Microsoft.Band.Admin
{
  internal sealed class TileIconbox : TilePageElement, ITileIconbox, ITilePageElement
  {
    private ushort iconIndex;

    internal TileIconbox(ushort elementId, ushort iconIndex)
      : base(elementId)
    {
      this.IconIndex = iconIndex;
    }

    public ushort IconIndex
    {
      get => this.iconIndex;
      set => this.iconIndex = value < (ushort) 10 ? value : throw new ArgumentOutOfRangeException(nameof (IconIndex));
    }

    internal override ushort ElementType => 3101;
  }
}
