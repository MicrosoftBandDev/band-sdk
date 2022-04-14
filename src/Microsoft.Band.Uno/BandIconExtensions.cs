using Microsoft.Band.Tiles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using SLImage = SixLabors.ImageSharp.Image;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Microsoft.Band
{
    public static class BandIconExtensions
    {
        public static BandIcon BandIconFromMauiImage(this ImageSource source)
        {
            SLImage agnosticImage = null;
            if (source is BitmapImage bitmap)
            {
                agnosticImage = SLImage.Load(bitmap.UriSource.AbsolutePath);
            }
            else if (source is RenderTargetBitmap softwareBitmap)
            {
                IBuffer buffer = softwareBitmap.GetPixelsAsync().AsTask().Result;
                agnosticImage = SLImage.LoadPixelData<Bgra32>(buffer.ToArray(), softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);
            }

            if (agnosticImage == null)
                throw new Exception("Failed to retrieve image data from ImageSource.");
            Image<Bgr565> convertedImage = agnosticImage.CloneAs<Bgr565>();
            Span<byte> bgr565Bytes = null;
            Rgba32[] pixelArray;
            //if (convertedImage.TryGetSinglePixelSpan(out var pixelSpan))
            //    pixelArray = pixelSpan.ToArray();
            //else
            //    throw new Exception("Failed to convert image to BGR565.");


            int bpp = convertedImage.PixelType.BitsPerPixel / 8;
            convertedImage.CopyPixelDataTo(bgr565Bytes);
            return new(convertedImage.Width * bpp, convertedImage.Height * bpp, bgr565Bytes.ToArray());

            bgr565Bytes = new byte[pixelArray.Length * 2];
            int i = 0, w = 0;
            while (i < pixelArray.Length)
            {
                Rgba32 pixel = pixelArray[i++];
                ushort packed = 0;

                // Convert from 1 byte per channel to 5 or 6 as appropriate
                byte B = (byte)((double)pixel.B / 255 * 0b011111);
                byte G = (byte)((double)pixel.G / 255 * 0b111111);
                byte R = (byte)((double)pixel.R / 255 * 0b011111);

                packed = (ushort)((pixel.B << 11) | (pixel.G << 5) | pixel.R);

                bgr565Bytes[w++] = (byte)(packed >> sizeof(byte));      // Upper byte
                bgr565Bytes[w++] = (byte)(packed & byte.MaxValue);      // Lower byte
            }

            const int bytesPerPixel = 2;
            //return new(convertedImage.Width * bytesPerPixel, convertedImage.Height * bytesPerPixel, bgr565Bytes);
        }
    }
}
