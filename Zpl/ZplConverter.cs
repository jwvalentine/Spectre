using SkiaSharp;
using System;
using System.IO;
using System.Text;

namespace Spectre.Zpl
{
    public static class ZplConverter
    {
        public static string ConvertPngToZpl(string pngPath)
        {
            using var stream = File.OpenRead(pngPath);
            using var skBitmap = SKBitmap.Decode(stream);
            var sb = new StringBuilder();

            int width = skBitmap.Width;
            int height = skBitmap.Height;
            int widthBytes = (width + 7) / 8;
            int totalBytes = widthBytes * height;

            sb.AppendLine("^XA");
            sb.AppendLine($"^FO0,0^GFA,{totalBytes},{totalBytes},{widthBytes},");

            for (int y = 0; y < height; y++)
            {
                for (int xByte = 0; xByte < widthBytes; xByte++)
                {
                    byte b = 0;
                    for (int bit = 0; bit < 8; bit++)
                    {
                        int x = xByte * 8 + bit;
                        if (x >= width) continue;

                        var color = skBitmap.GetPixel(x, y);
                        // Use brightness to determine whether to print a pixel
                        if (color.Red < 128 && color.Green < 128 && color.Blue < 128)
                        {
                            b |= (byte)(1 << (7 - bit));
                        }
                    }
                    sb.Append(b.ToString("X2"));
                }
                sb.AppendLine();
            }

            sb.AppendLine("^FS^XZ");
            return sb.ToString();
        }
    }
}