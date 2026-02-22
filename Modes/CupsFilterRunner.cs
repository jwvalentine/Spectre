using Spectre.Rendering;
using Spectre.Zpl;

namespace Spectre.Modes
{
    public static class CupsFilterRunner
    {
        public static void Run(string[] args)
        {
            // CUPS filter argv: job-id user title copies options [filename]
            string inputPdfPath = args[5];

            try
            {
                var pages = PdfRenderer.RenderPages(inputPdfPath, dpi: 203);

                foreach (var pngPath in pages)
                {
                    try
                    {
                        var zpl = ZplConverter.ConvertPngToZpl(pngPath);
                        Console.Write(zpl);
                    }
                    finally
                    {
                        // Clean up intermediate PNG; don't leave debris in the CUPS temp dir.
                        if (File.Exists(pngPath))
                            File.Delete(pngPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Spectre filter failed: " + ex.Message);
                Environment.Exit(1);
            }
        }
    }
}
