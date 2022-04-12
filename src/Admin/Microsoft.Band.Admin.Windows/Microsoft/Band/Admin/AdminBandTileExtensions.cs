using Microsoft.Band.Tiles;

namespace Microsoft.Band.Admin;

internal static class AdminBandTileExtensions
{
    internal static TileData ToTileData(this AdminBandTile tile, uint startStripOrder = 0u)
    {
        TileData tileData = new TileData();
        tileData.AppID = tile.Id;
        tileData.StartStripOrder = startStripOrder;
        tileData.ThemeColor = 0u;
        tileData.SettingsMask = (TileSettings)tile.SettingsMask;
        tileData.SetNameAndOwnerId(tile.Name, tile.OwnerId);
        return tileData;
    }
}
