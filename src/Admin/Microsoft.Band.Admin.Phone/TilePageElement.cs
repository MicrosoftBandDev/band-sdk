// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.TilePageElement
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

namespace Microsoft.Band.Admin
{
  public abstract class TilePageElement : ITilePageElement
  {
    protected internal TilePageElement(ushort elementId) => this.ElementId = elementId;

    public ushort ElementId { get; set; }

    internal abstract ushort ElementType { get; }
  }
}
