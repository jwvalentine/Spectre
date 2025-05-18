using SkiaSharp;
using System;

namespace Spectre.Rendering
{
    public static class PdfRenderer
    {
        public static string RenderToMonochromePng(string inputPdfPath, int dpi = 203)
        {
            var outputPath = System.IO.Path.ChangeExtension(inputPdfPath, ".png");

            int width = 800, height = 1200; // typical label size at 203dpi
            using var bitmap = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Opaque);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            var paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true
            };

            var font = new SKFont
            {
                Size = 24
            };

            canvas.DrawText("Sample Label PDF Rendered", 100, 100, SKTextAlign.Left, font, paint);
            canvas.DrawText(DateTime.Now.ToString("g"), 100, 140, SKTextAlign.Left, font, paint);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            System.IO.File.WriteAllBytes(outputPath, data.ToArray());

            Console.WriteLine($"> PNG written: {outputPath}");
            return outputPath;
        }
    }
}