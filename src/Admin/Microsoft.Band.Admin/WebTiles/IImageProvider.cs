// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.IImageProvider
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Microsoft.Band.Tiles;
using System.Threading.Tasks;

namespace Microsoft.Band.Admin.WebTiles
{
    public interface IImageProvider
    {
        Task<BandIcon> GetBandIconFromFileAsync(string path);
    }
}
