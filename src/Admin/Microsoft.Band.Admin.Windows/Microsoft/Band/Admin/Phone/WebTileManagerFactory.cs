using Microsoft.Band.Admin.Phone.WebTiles;
using Microsoft.Band.Admin.WebTiles;

namespace Microsoft.Band.Admin.Phone;

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
                        StorageProvider storageProvider = StorageProvider.Create();
                        instance = new WebTileManager(storageProvider, new ImageProvider(storageProvider));
                    }
                }
            }
            return instance;
        }
    }
}
