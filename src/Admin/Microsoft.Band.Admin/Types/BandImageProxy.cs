// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.Types.BandImageProxy
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using Microsoft.Band.Personalization;

namespace Microsoft.Band.Admin.Types
{
  public class BandImageProxy : BandImage
  {
    public BandImageProxy(int width, int height, byte[] pixelData)
      : base(width, height, pixelData)
    {
    }

    public BandImageProxy(BandImage bandImage)
      : base(bandImage.Width, bandImage.Height, bandImage.PixelData)
    {
    }

    public new byte[] PixelData => base.PixelData;
  }
}
