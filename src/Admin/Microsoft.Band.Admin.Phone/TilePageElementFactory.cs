// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.TilePageElementFactory
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

namespace Microsoft.Band.Admin
{
  public class TilePageElementFactory : ITilePageElementFactory
  {
    public ITileBarcode CreateTileBarcode(
      ushort elementId,
      BarcodeType codeType,
      string barcodeValue)
    {
      return (ITileBarcode) new TileBarcode(elementId, codeType, barcodeValue);
    }

    public ITileIconbox CreateTileIconbox(ushort elementId, ushort iconIndex) => (ITileIconbox) new TileIconbox(elementId, iconIndex);

    public ITileTextbox CreateTileTextbox(ushort elementId, string textboxValue) => (ITileTextbox) new TileTextbox(elementId, textboxValue);

    public ITileWrappableTextbox CreateTileWrappableTextbox(
      ushort elementId,
      string textboxValue)
    {
      return (ITileWrappableTextbox) new TileWrappableTextbox(elementId, textboxValue);
    }
  }
}
