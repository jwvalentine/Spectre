using NUnit.Framework;
using SkiaSharp;
using Spectre.Zpl;

namespace Spectre.Tests
{
    [TestFixture]
    public class ZplConverterTests
    {
        private readonly List<string> _filesToDelete = [];

        [TearDown]
        public void TearDown()
        {
            foreach (var f in _filesToDelete.Where(File.Exists))
                File.Delete(f);
            _filesToDelete.Clear();
        }

        // Creates a solid-colour PNG at the given pixel dimensions and returns its path.
        private string CreateSolidPng(int width, int height, SKColor colour)
        {
            var path = Path.Combine(Path.GetTempPath(), $"spectre_zpl_{Guid.NewGuid():N}.png");
            _filesToDelete.Add(path);

            using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(colour);

            using var image = SKImage.FromBitmap(bitmap);
            using var data  = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, data.ToArray());

            return path;
        }

        // ── Structure ─────────────────────────────────────────────────────────────

        [Test]
        public void ConvertPngToZpl_StartsWithXA()
        {
            var png = CreateSolidPng(8, 4, SKColors.White);
            var zpl = ZplConverter.ConvertPngToZpl(png);
            Assert.That(zpl.TrimStart(), Does.StartWith("^XA"));
        }

        [Test]
        public void ConvertPngToZpl_EndsWithXZ()
        {
            var png = CreateSolidPng(8, 4, SKColors.White);
            var zpl = ZplConverter.ConvertPngToZpl(png);
            Assert.That(zpl.TrimEnd(), Does.EndWith("^XZ"));
        }

        [Test]
        public void ConvertPngToZpl_ContainsGfaCommand()
        {
            var png = CreateSolidPng(8, 4, SKColors.White);
            var zpl = ZplConverter.ConvertPngToZpl(png);
            Assert.That(zpl, Does.Contain("^GFA"));
        }

        // ── Byte-count header ─────────────────────────────────────────────────────

        [Test]
        public void ConvertPngToZpl_GfaHeader_ReflectsCorrectDimensions()
        {
            // 16 px wide → widthBytes = 2; 8 rows → totalBytes = 16
            const int width = 16, height = 8;
            int widthBytes = (width + 7) / 8;
            int totalBytes = widthBytes * height;

            var png = CreateSolidPng(width, height, SKColors.White);
            var zpl = ZplConverter.ConvertPngToZpl(png);

            Assert.That(zpl, Does.Contain($"^GFA,{totalBytes},{totalBytes},{widthBytes},"),
                "GFA header must encode totalBytes, totalBytes, widthBytes in order");
        }

        // ── Pixel data correctness ────────────────────────────────────────────────

        [Test]
        public void ConvertPngToZpl_AllWhiteImage_ProducesNoSetBits()
        {
            // 8 px wide → widthBytes = 1; each row should be "00"
            var png = CreateSolidPng(8, 4, SKColors.White);
            var zpl = ZplConverter.ConvertPngToZpl(png);

            var hexLines = ExtractHexDataLines(zpl);
            Assert.That(hexLines, Is.Not.Empty);
            Assert.That(hexLines.All(l => l == new string('0', l.Length)), Is.True,
                "A fully white image must produce all-zero hex data");
        }

        [Test]
        public void ConvertPngToZpl_AllBlackImage_ProducesAllSetBits()
        {
            // 8 px wide → widthBytes = 1; each row should be "FF"
            var png = CreateSolidPng(8, 4, SKColors.Black);
            var zpl = ZplConverter.ConvertPngToZpl(png);

            var hexLines = ExtractHexDataLines(zpl);
            Assert.That(hexLines, Is.Not.Empty);
            Assert.That(hexLines.All(l => l.ToUpperInvariant() == new string('F', l.Length)), Is.True,
                "A fully black image must produce all-FF hex data");
        }

        [Test]
        public void ConvertPngToZpl_HexDataRowCount_MatchesImageHeight()
        {
            const int height = 6;
            var png = CreateSolidPng(8, height, SKColors.White);
            var zpl = ZplConverter.ConvertPngToZpl(png);

            var hexLines = ExtractHexDataLines(zpl);
            Assert.That(hexLines.Count, Is.EqualTo(height),
                "There must be exactly one hex data row per image row");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        // Returns only the lines that contain pure hex data (not ZPL command lines).
        // ZPL command lines always contain non-hex characters (^, G in GFA, commas, etc.).
        private static IReadOnlyList<string> ExtractHexDataLines(string zpl)
        {
            static bool IsHexChar(char c) =>
                (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');

            return zpl
                .Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && l.All(IsHexChar))
                .ToList();
        }
    }
}
