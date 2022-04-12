using Microsoft.Band.Admin.WebTiles;
#if WINDOWS
using Microsoft.Band.Admin.Windows;
using Microsoft.Band.Admin.Phone.WebTiles;
#endif

namespace Microsoft.Band.Admin;

public class WebTileManagerFactory : IWebTileManagerFactory
{
    private static IWebTileManager instance;

    private static object lockingObject = new object();

    public static IWebTileManager Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lockingObject)
                {
                    if (instance == null)
                    {
#if WINDOWS
                        StorageProvider storageProvider = StorageProvider.Create();
                        instance = new WebTileManager(storageProvider, new ImageProvider(storageProvider));
#else
                        throw new System.NotImplementedException();
#endif
                    }
                }
            }
            return instance;
        }
    }
}
