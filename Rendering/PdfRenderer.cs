using PDFtoImage;
using SkiaSharp;

namespace Spectre.Rendering
{
    public static class PdfRenderer
    {
        /// <summary>
        /// Renders every page of a PDF to grayscale PNG files on disk.
        /// Returns a list of output paths, one per page.
        /// </summary>
        public static IReadOnlyList<string> RenderPages(string inputPdfPath, int dpi = 203)
        {
            var options = new RenderOptions(
                Dpi: dpi,
                WithAnnotations: true,
                WithFormFill: true,
                Grayscale: true,
                BackgroundColor: SKColors.White);

            var basePath = Path.ChangeExtension(inputPdfPath, null);
            var pngPaths = new List<string>();

            using var stream = File.OpenRead(inputPdfPath);

            foreach (var bitmap in Conversion.ToImages(stream, leaveOpen: true, password: null, options: options))
            {
                using (bitmap)
                {
                    // First page gets a clean name; subsequent pages are numbered.
                    var outputPath = pngPaths.Count == 0
                        ? $"{basePath}.png"
                        : $"{basePath}_p{pngPaths.Count + 1:D4}.png";

                    using var image = SKImage.FromBitmap(bitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    File.WriteAllBytes(outputPath, data.ToArray());

                    Console.Error.WriteLine($"Spectre: page {pngPaths.Count + 1} rendered ({bitmap.Width}x{bitmap.Height}) → {outputPath}");
                    pngPaths.Add(outputPath);
                }
            }

            if (pngPaths.Count == 0)
                throw new InvalidOperationException($"No pages rendered from: {inputPdfPath}");

            return pngPaths;
        }
    }
}
