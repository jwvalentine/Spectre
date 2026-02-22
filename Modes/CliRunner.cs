using Spectre.Rendering;

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
                    case "-i": input = args[++i]; break;
                    case "-o": output = args[++i]; break;
                    case "-r": dpi = int.Parse(args[++i]); break;
                }
            }

            if (!File.Exists(input))
            {
                Console.Error.WriteLine($"Spectre: input file not found: {input}");
                Environment.Exit(1);
                return;
            }

            var pages = PdfRenderer.RenderPages(input, dpi);

            if (string.IsNullOrEmpty(output))
            {
                foreach (var p in pages)
                    Console.WriteLine(p);
                return;
            }

            if (pages.Count == 1)
            {
                File.Move(pages[0], output, overwrite: true);
                Console.WriteLine(output);
            }
            else
            {
                // Multi-page: insert page number before the extension for pages 2+.
                var ext = Path.GetExtension(output);
                var stem = Path.ChangeExtension(output, null);

                for (int i = 0; i < pages.Count; i++)
                {
                    var dest = i == 0 ? output : $"{stem}_p{i + 1:D4}{ext}";
                    File.Move(pages[i], dest, overwrite: true);
                    Console.WriteLine(dest);
                }
            }
        }
    }
}
