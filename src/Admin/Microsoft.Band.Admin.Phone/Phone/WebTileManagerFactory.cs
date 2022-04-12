// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Phone.WebTileManagerFactory
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Admin.Phone.WebTiles;
using Microsoft.Band.Admin.WebTiles;

namespace Microsoft.Band.Admin.Phone
{
  public class WebTileManagerFactory : IWebTileManagerFactory
  {
    private static IWebTileManager instance;
    private static object lockingObject = new object();

    public static IWebTileManager Instance
    {
      get
      {
        if (WebTileManagerFactory.instance == null)
        {
          lock (WebTileManagerFactory.lockingObject)
          {
            if (WebTileManagerFactory.instance == null)
            {
              StorageProvider storageProvider = StorageProvider.Create();
              WebTileManagerFactory.instance = (IWebTileManager) new WebTileManager((IStorageProvider) storageProvider, (IImageProvider) new ImageProvider(storageProvider));
            }
          }
        }
        return WebTileManagerFactory.instance;
      }
    }
  }
}
