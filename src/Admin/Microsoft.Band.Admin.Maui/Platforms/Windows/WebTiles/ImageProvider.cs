using System.Threading.Tasks;
using Microsoft.Band.Admin.WebTiles;
using Microsoft.Band.Admin.Windows;
using Microsoft.Band.Tiles;
using Microsoft.Maui.Controls;

namespace Microsoft.Band.Admin.Phone.WebTiles;

internal class ImageProvider : IImageProvider
{
    private StorageProvider storageProvider;

    public ImageProvider(StorageProvider storageProvider)
    {
        this.storageProvider = storageProvider;
    }

    public async Task<BandIcon> GetBandIconFromFileAsync(string path)
    {
        FileImageSource imageSource = new()
        {
            File = path
        };
        return imageSource.BandIconFromMauiImage();
    }
}
