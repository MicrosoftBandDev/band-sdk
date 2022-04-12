// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.AdminBandTileExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Tiles;

namespace Microsoft.Band.Admin
{
  internal static class AdminBandTileExtensions
  {
    internal static TileData ToTileData(this AdminBandTile tile, uint startStripOrder = 0)
    {
      TileData tileData = new TileData();
      tileData.AppID = tile.Id;
      tileData.StartStripOrder = startStripOrder;
      tileData.ThemeColor = 0U;
      tileData.SettingsMask = (TileSettings) tile.SettingsMask;
      tileData.SetNameAndOwnerId(tile.Name, tile.OwnerId);
      return tileData;
    }
  }
}
