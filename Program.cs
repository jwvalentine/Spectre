using Spectre.Modes;
using System;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage();
            return;
        }

        if (args[0] == "-i")
        {
            CliRunner.Run(args);
        }
        else if (args.Length == 6)
        {
            CupsFilterRunner.Run(args);
        }
        else
        {
            ShowUsage();
        }
    }

    static void ShowUsage()
    {
        Console.WriteLine("Spectre - Minimalist Ghostscript Alternative");
        Console.WriteLine("CLI Mode:   spectre -i input.pdf -o output.png -r 300");
        Console.WriteLine("CUPS Mode:  spectre job-id user title copies options input.pdf");
    }
}