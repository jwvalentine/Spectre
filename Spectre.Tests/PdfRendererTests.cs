using NUnit.Framework;
using SkiaSharp;
using Spectre.Rendering;
using Spectre.Tests.Assets;

namespace Spectre.Tests
{
    [TestFixture]
    public class PdfRendererTests
    {
        // Shared 3-page PDF used by most tests.
        private string _sharedPdf = null!;

        // Every file created during the fixture is registered here for cleanup.
        private readonly List<string> _filesToDelete = [];

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _sharedPdf = TestPdfFactory.CreateTempPdf(pageCount: 3);
            _filesToDelete.Add(_sharedPdf);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            foreach (var f in _filesToDelete.Where(File.Exists))
                File.Delete(f);
        }

        // Renders a PDF and registers the output PNGs for cleanup.
        private IReadOnlyList<string> Render(string pdfPath, int dpi = 72)
        {
            var pages = PdfRenderer.RenderPages(pdfPath, dpi);
            _filesToDelete.AddRange(pages);
            return pages;
        }

        // ── Page count ────────────────────────────────────────────────────────────

        [Test]
        public void RenderPages_ThreePage_ReturnsThreePaths()
        {
            var pages = Render(_sharedPdf);
            Assert.That(pages.Count, Is.EqualTo(3));
        }

        [Test]
        public void RenderPages_SinglePage_ReturnsOnePath()
        {
            var pdf = TestPdfFactory.CreateTempPdf(pageCount: 1);
            _filesToDelete.Add(pdf);

            var pages = Render(pdf);
            Assert.That(pages.Count, Is.EqualTo(1));
        }

        // ── Output files ──────────────────────────────────────────────────────────

        [Test]
        public void RenderPages_AllOutputFilesExistOnDisk()
        {
            var pages = Render(_sharedPdf);
            foreach (var path in pages)
                Assert.That(File.Exists(path), Is.True, $"Missing PNG: {path}");
        }

        [Test]
        public void RenderPages_OutputFilesArePng()
        {
            var pages = Render(_sharedPdf);
            foreach (var path in pages)
                Assert.That(path, Does.EndWith(".png"), $"Expected .png extension: {path}");
        }

        // ── Naming convention ─────────────────────────────────────────────────────

        [Test]
        public void RenderPages_FirstPage_HasNoPageNumberSuffix()
        {
            var pages = Render(_sharedPdf);
            Assert.That(pages[0], Does.Not.Contain("_p0"),
                "First page should use the clean base name with no suffix");
        }

        [Test]
        public void RenderPages_SecondPage_HasP0002Suffix()
        {
            var pages = Render(_sharedPdf);
            Assert.That(pages[1], Does.Contain("_p0002"));
        }

        [Test]
        public void RenderPages_ThirdPage_HasP0003Suffix()
        {
            var pages = Render(_sharedPdf);
            Assert.That(pages[2], Does.Contain("_p0003"));
        }

        // ── Image dimensions ──────────────────────────────────────────────────────

        [Test]
        public void RenderPages_OutputImagesAreNonEmpty()
        {
            var pages = Render(_sharedPdf);
            foreach (var path in pages)
            {
                using var bmp = SKBitmap.Decode(path);
                Assert.That(bmp, Is.Not.Null, $"Could not decode {path}");
                Assert.That(bmp.Width,  Is.GreaterThan(0));
                Assert.That(bmp.Height, Is.GreaterThan(0));
            }
        }

        [Test]
        public void RenderPages_HigherDpi_ProducesLargerImage()
        {
            // Use separate PDFs so the output PNGs are at different paths.
            var pdfLow  = TestPdfFactory.CreateTempPdf(pageCount: 1);
            var pdfHigh = TestPdfFactory.CreateTempPdf(pageCount: 1);
            _filesToDelete.Add(pdfLow);
            _filesToDelete.Add(pdfHigh);

            var pagesLow  = Render(pdfLow,  dpi: 72);
            var pagesHigh = Render(pdfHigh, dpi: 144);

            using var bmpLow  = SKBitmap.Decode(pagesLow[0]);
            using var bmpHigh = SKBitmap.Decode(pagesHigh[0]);

            Assert.That(bmpHigh.Width,  Is.GreaterThan(bmpLow.Width));
            Assert.That(bmpHigh.Height, Is.GreaterThan(bmpLow.Height));
        }

        // ── Error handling ────────────────────────────────────────────────────────

        [Test]
        public void RenderPages_NonPdfFile_Throws()
        {
            var notAPdf = Path.GetTempFileName();
            _filesToDelete.Add(notAPdf);
            File.WriteAllText(notAPdf, "this is not a pdf");

            // Assert.Catch accepts the exception or any derived type (NUnit 4 Assert.Throws is exact-match only).
            Assert.Catch<Exception>(() => PdfRenderer.RenderPages(notAPdf, dpi: 72));
        }

        [Test]
        public void RenderPages_MissingFile_Throws()
        {
            var missing = Path.Combine(Path.GetTempPath(), "does_not_exist.pdf");
            Assert.Catch<FileNotFoundException>(() => PdfRenderer.RenderPages(missing, dpi: 72));
        }
    }
}
