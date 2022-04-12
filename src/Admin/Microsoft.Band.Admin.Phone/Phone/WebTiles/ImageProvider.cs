// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Phone.WebTiles.ImageProvider
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Admin.WebTiles;
using Microsoft.Band.Tiles;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace Microsoft.Band.Admin.Phone.WebTiles
{
  internal class ImageProvider : IImageProvider
  {
    private StorageProvider storageProvider;

    public ImageProvider(StorageProvider storageProvider) => this.storageProvider = storageProvider;

    public async Task<BandIcon> GetBandIconFromFileAsync(string path)
    {
      WriteableBitmap writeableBitmap = new WriteableBitmap(1, 1);
      await ((BitmapSource) writeableBitmap).SetSourceAsync(this.storageProvider.OpenFileRandomAccessStreamForRead(StorageProviderRoot.App, path));
      return writeableBitmap.ToBandIcon();
    }
  }
}
