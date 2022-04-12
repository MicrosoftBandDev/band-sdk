// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.TileDataExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Tiles;

namespace Microsoft.Band.Admin
{
  internal static class TileDataExtensions
  {
    internal static AdminBandTile ToAdminBandTile(this TileData data)
    {
      string name = "(DEFAULT NAME)";
      if (data.FriendlyName != null && data.FriendlyNameLength > (ushort) 0)
        name = data.FriendlyName;
      return new AdminBandTile(data.AppID, name, (AdminTileSettings) data.SettingsMask, data.Icon)
      {
        OwnerId = data.OwnerId
      };
    }
  }
}
