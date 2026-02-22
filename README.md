# Spectre

**Spectre** is a minimalist, MIT-licensed replacement for Ghostscript raster filters, written in C# (.NET 8).
It renders PDFs to raster formats (PNG or ZPL for label printers) using Google's PDFium engine — without any AGPL components.

---

## Goals

- Drop-in replacement for Ghostscript's `gstoraster` and `pdftoraster`
- Full PDF rendering via PDFium (text, vector graphics, images, annotations, form fields)
- Output ZPL (`^GFA`) for Zebra / Intermec label printers
- Multi-page support — each page becomes a separate label
- Runs on Linux, Windows, and macOS
- Designed for containerized Linux + CUPS environments
- No AGPL, no GPL, no viral copyleft anywhere in the dependency chain

---

## Usage

### CLI mode

```bash
spectre -i input.pdf -o output.png -r 300
```

| Flag | Description |
|------|-------------|
| `-i` | Input PDF path |
| `-o` | Output image path (PNG). Multi-page PDFs produce `output.png`, `output_p0002.png`, etc. |
| `-r` | Resolution in DPI (default: 300) |

Output paths are printed to `stdout`, one per page.

### CUPS filter mode

```bash
spectre job-id user title copies options input.pdf
```

CUPS calls this automatically. ZPL for all pages is streamed to `stdout`, suitable for RAW print queues.
Diagnostic messages go to `stderr` and appear in the CUPS error log.

---

## Setup (CUPS)

1. Place the compiled binary at:
   ```
   /usr/lib/cups/filter/spectre
   ```

2. Register it in the printer's PPD or `cups-filters.conf`:
   ```ppd
   *cupsFilter: "application/pdf 0 spectre"
   ```

---

## Building

Requires .NET 8 SDK or later. No native toolchain needed — PDFium and SkiaSharp native binaries
are delivered as NuGet packages for Linux, macOS, and Windows.

```bash
dotnet build
dotnet publish -c Release -r linux-x64 --self-contained
```

---

## License

MIT — see [LICENSE](LICENSE).
Third-party library credits — see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
