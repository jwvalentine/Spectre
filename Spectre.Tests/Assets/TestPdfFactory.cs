using System.Text;

namespace Spectre.Tests.Assets
{
    /// <summary>
    /// Builds minimal but fully valid PDF documents in memory.
    /// No external PDF library required — the structure is written by hand
    /// and produces output that PDFium renders correctly.
    /// </summary>
    public static class TestPdfFactory
    {
        // One paragraph of Lorem Ipsum per page (cycled for N > 3 pages).
        private static readonly string[] Paragraphs =
        [
            "Lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor " +
            "incididunt ut labore et dolore magna aliqua ut enim ad minim veniam",

            "Quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat " +
            "duis aute irure dolor in reprehenderit in voluptate velit esse cillum",

            "Excepteur sint occaecat cupidatat non proident sunt in culpa qui officia deserunt " +
            "mollit anim id est laborum sed ut perspiciatis unde omnis iste natus error",
        ];

        /// <summary>
        /// Writes a temp PDF file and returns its path. The caller is responsible for deletion.
        /// </summary>
        public static string CreateTempPdf(int pageCount = 3)
        {
            var path = Path.Combine(Path.GetTempPath(), $"spectre_test_{Guid.NewGuid():N}.pdf");
            File.WriteAllBytes(path, BuildPdf(pageCount));
            return path;
        }

        /// <summary>
        /// Builds and returns the raw bytes of a valid PDF document with <paramref name="pageCount"/> pages.
        /// </summary>
        public static byte[] BuildPdf(int pageCount = 3)
        {
            if (pageCount < 1) throw new ArgumentOutOfRangeException(nameof(pageCount));

            // Object layout
            //   1          Catalog
            //   2          Pages
            //   3          Font (Helvetica Type1 — built into PDFium, no embedding required)
            //   4..3+N     Page objects
            //   4+N..3+2N  Content streams

            int firstPage    = 4;
            int firstContent = firstPage + pageCount;
            int totalObjects = 3 + pageCount * 2;

            var offsets = new long[totalObjects + 1]; // 1-indexed; [0] unused

            using var ms = new MemoryStream();

            void Emit(string s)
            {
                var bytes = Encoding.Latin1.GetBytes(s);
                ms.Write(bytes, 0, bytes.Length);
            }

            void BeginObject(int n)
            {
                offsets[n] = ms.Position;
                Emit($"{n} 0 obj\n");
            }

            // ── Header ────────────────────────────────────────────────────────────
            Emit("%PDF-1.4\n");

            // ── Object 1: Catalog ─────────────────────────────────────────────────
            BeginObject(1);
            Emit("<< /Type /Catalog /Pages 2 0 R >>\n");
            Emit("endobj\n\n");

            // ── Object 2: Pages ───────────────────────────────────────────────────
            BeginObject(2);
            var kids = string.Join(" ", Enumerable.Range(firstPage, pageCount).Select(i => $"{i} 0 R"));
            Emit($"<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>\n");
            Emit("endobj\n\n");

            // ── Object 3: Font ────────────────────────────────────────────────────
            BeginObject(3);
            Emit("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\n");
            Emit("endobj\n\n");

            // ── Page objects ──────────────────────────────────────────────────────
            for (int i = 0; i < pageCount; i++)
            {
                BeginObject(firstPage + i);
                Emit($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792]\n");
                Emit($"   /Contents {firstContent + i} 0 R\n");
                Emit( "   /Resources << /Font << /F1 3 0 R >> >> >>\n");
                Emit("endobj\n\n");
            }

            // ── Content streams ───────────────────────────────────────────────────
            for (int i = 0; i < pageCount; i++)
            {
                string stream = BuildPageStream(i + 1, Paragraphs[i % Paragraphs.Length]);
                int length = Encoding.Latin1.GetByteCount(stream);

                BeginObject(firstContent + i);
                Emit($"<< /Length {length} >>\n");
                Emit("stream\n");
                Emit(stream);
                Emit("endstream\n");
                Emit("endobj\n\n");
            }

            // ── Cross-reference table ─────────────────────────────────────────────
            // Each entry is exactly 20 bytes: 10-digit offset + space + 5-digit gen
            // + space + f/n + \r\n  (PDF spec §7.5.4).
            long xrefOffset = ms.Position;
            Emit($"xref\n0 {totalObjects + 1}\n");
            Emit("0000000000 65535 f\r\n");
            for (int i = 1; i <= totalObjects; i++)
                Emit($"{offsets[i]:D10} 00000 n\r\n");

            // ── Trailer ───────────────────────────────────────────────────────────
            Emit($"trailer\n<< /Size {totalObjects + 1} /Root 1 0 R >>\n");
            Emit($"startxref\n{xrefOffset}\n");
            Emit("%%EOF\n");

            return ms.ToArray();
        }

        // Builds a simple PDF content stream: a page heading + wrapped body text.
        // Only ASCII characters are used; no ( ) \ appear in the Lorem Ipsum paragraphs,
        // so no PDF string escaping is required.
        private static string BuildPageStream(int pageNumber, string paragraph)
        {
            var sb = new StringBuilder();
            sb.Append("BT\n");
            sb.Append("/F1 18 Tf\n");
            sb.Append("72 720 Td\n");
            sb.Append($"(Page {pageNumber}) Tj\n");
            sb.Append("0 -28 Td\n");
            sb.Append("/F1 11 Tf\n");

            // Naive word-wrap at 65 characters per line.
            var words = paragraph.Split(' ');
            var line  = new StringBuilder();
            foreach (var word in words)
            {
                if (line.Length > 0 && line.Length + word.Length + 1 > 65)
                {
                    sb.Append($"({line.ToString().TrimEnd()}) Tj\n");
                    sb.Append("0 -16 Td\n");
                    line.Clear();
                }
                line.Append(word).Append(' ');
            }
            if (line.Length > 0)
                sb.Append($"({line.ToString().TrimEnd()}) Tj\n");

            sb.Append("ET\n");
            return sb.ToString();
        }
    }
}
