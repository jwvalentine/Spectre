using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Spectre.Pdf
{
    public class PdfPageLetter
    {
        public string Value { get; set; } = string.Empty;
        public float X { get; set; } = 100; // placeholder
        public float Y { get; set; } = 500; // placeholder
        public float FontSize { get; set; } = 18; // default font size
    }

    public static class MinimalPdfReader
    {
        public static List<PdfPageLetter> ExtractText(string pdfPath)
        {
            var letters = new List<PdfPageLetter>();

            byte[] data = File.ReadAllBytes(pdfPath);
            string content = Encoding.ASCII.GetString(data);

            // Find all streams
            var streamRegex = new Regex(@"stream\r?\n(.*?)\r?\nendstream", RegexOptions.Singleline);
            var matches = streamRegex.Matches(content);

            foreach (Match match in matches)
            {
                string streamContent = match.Groups[1].Value;

                // Decode any literal strings inside parentheses: (text)
                var textRegex = new Regex(@"\(([^)]+)\)");
                var textMatches = textRegex.Matches(streamContent);

                foreach (Match textMatch in textMatches)
                {
                    letters.Add(new PdfPageLetter
                    {
                        Value = textMatch.Groups[1].Value
                    });
                }

                if (letters.Count > 0)
                    break; // only extract first stream for now
            }

            // Spread out the letters vertically
            for (int i = 0; i < letters.Count; i++)
            {
                letters[i].X = 100;
                letters[i].Y = 600 - (i * 30);
            }

            return letters;
        }
    }
}