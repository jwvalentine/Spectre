using Spectre.Rendering;
using System;

namespace Spectre.Modes
{
    public static class CliRunner
    {
        public static void Run(string[] args)
        {
            string input = string.Empty;
            string output = string.Empty;
            int dpi = 300;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-i":
                        input = args[++i];
                        break;
                    case "-o":
                        output = args[++i];
                        break;
                    case "-r":
                        dpi = int.Parse(args[++i]);
                        break;
                }
            }

            if (!System.IO.File.Exists(input))
            {
                Console.WriteLine($"Input file not found: {input}");
                return;
            }

            var png = PdfRenderer.RenderToMonochromePng(input, dpi);
            Console.WriteLine($"> Rendered: {png}");

            if (!string.IsNullOrEmpty(output) && output != png)
            {
                System.IO.File.Move(png, output, overwrite: true);
                Console.WriteLine($"> Moved to: {output}");
            }
        }
    }
}