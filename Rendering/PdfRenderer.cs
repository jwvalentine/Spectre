using SkiaSharp;
using System;

namespace Spectre.Rendering
{
    public static class PdfRenderer
    {
        public static void RenderToRaster(string inputPdfPath, string outputRasterPath, int dpi)
        {
            Console.WriteLine($"> Rendering PDF '{inputPdfPath}' at {dpi} DPI...");
            // TODO: Use PdfPig to parse PDF and render with SkiaSharp (mocked for now)
            int width = 595, height = 842; // A4 at 72dpi base, scale for target dpi

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            var paint = new SKPaint { Color = SKColors.Black };
            var font = new SKFont { Size = 32 };
            canvas.DrawText("Spectre Raster Output", 100, 400, SKTextAlign.Left, font, paint);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(outputRasterPath, data.ToArray());

            Console.WriteLine($"> Output written to {outputRasterPath}");
        }
    }
}