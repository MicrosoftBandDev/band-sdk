// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.ColorExtensions
// Assembly: Microsoft.Band.Store, Version=1.3.20628.2, Culture=neutral, PublicKeyToken=608d7da3159f502b
// MVID: 91750BE8-70C6-4542-841C-664EE611AF0B
// Assembly location: .\netcore451\Microsoft.Band.Store.dll

using DrawingColor = System.Drawing.Color;
using WinUIColor = Windows.UI.Color;

namespace Microsoft.Band
{
    public static partial class ColorExtensions
    {
        public static BandColor ToBandColor(this DrawingColor color) => new(color.R, color.G, color.B);

        public static BandColor ToBandColor(this WinUIColor color) => new(color.R, color.G, color.B);

        public static DrawingColor ToColor(this BandColor color) => DrawingColor.FromArgb(byte.MaxValue, color.R, color.G, color.B);

        public static WinUIColor ToWinUIColor(this BandColor color) => WinUIColor.FromArgb(byte.MaxValue, color.R, color.G, color.B);
    }
}
