using Spectre.Rendering;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Spectre - Minimalist Ghostscript Alternative");

        if (args.Length == 0)
        {
            Console.WriteLine("Usage: spectre -i input.pdf -o output.pgm -r 300");
            return;
        }

        var input = string.Empty;
        var output = string.Empty;
        var dpi = 300;

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

        if (!File.Exists(input))
        {
            Console.WriteLine($"Input file not found: {input}");
            return;
        }

        PdfRenderer.RenderToRaster(input, output, dpi);
    }
}