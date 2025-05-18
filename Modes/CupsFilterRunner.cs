using Spectre.Rendering;
using Spectre.Zpl;
using System;

namespace Spectre.Modes
{
    public static class CupsFilterRunner
    {
        public static void Run(string[] args)
        {
            string inputPdfPath = args[5];

            try
            {
                var pngPath = PdfRenderer.RenderToMonochromePng(inputPdfPath, 203);
                var zpl = ZplConverter.ConvertPngToZpl(pngPath);
                Console.Write(zpl);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Spectre filter failed: " + ex.Message);
                Environment.Exit(1);
            }
        }
    }
}