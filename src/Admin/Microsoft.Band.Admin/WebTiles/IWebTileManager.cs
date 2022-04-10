// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.IWebTileManager
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Band.Admin.WebTiles
{
    public interface IWebTileManager
    {
        Task<IWebTile> GetWebTilePackageAsync(Uri uri);

        Task<IWebTile> GetWebTilePackageAsync(Stream source, string sourceFileName);

        Task InstallWebTileAsync(IWebTile webTile);

        Task UninstallWebTileAsync(Guid tileId);

        Task<IList<IWebTile>> GetInstalledWebTilesAsync(bool loadTileDisplayIcons);

        Task<AdminBandTile> CreateAdminBandTileAsync(
          IWebTile webTile,
          BandClass bandClass);

        IList<Guid> GetInstalledWebTileIds();

        IWebTile GetWebTile(Guid tileId);

        Task DeleteAllStoredResourceCredentialsAsync();
    }
}
