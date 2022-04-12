using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Band.Admin.WebTiles;
using Microsoft.Band.Tiles;
using Windows.UI.Xaml.Media.Imaging;

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
        WriteableBitmap writeableBitmap = new WriteableBitmap(1, 1);
        TaskAwaiter taskAwaiter = WindowsRuntimeSystemExtensions.GetAwaiter(((BitmapSource)writeableBitmap).SetSourceAsync(storageProvider.OpenFileRandomAccessStreamForRead(StorageProviderRoot.App, path)));
        if (!taskAwaiter.IsCompleted)
        {
            await taskAwaiter;
            TaskAwaiter taskAwaiter2 = default(TaskAwaiter);
            taskAwaiter = taskAwaiter2;
        }
        taskAwaiter.GetResult();
        return writeableBitmap.ToBandIcon();
    }
}
