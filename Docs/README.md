# Spectre

**Spectre** is a minimalist MIT-licensed replacement for Ghostscript filters, written in C# (.NET 8). It renders PDFs to raster formats (e.g. PNG, CUPS Raster) without any AGPL-licensed components.

## Goals

- Drop-in replacement for Ghostscript raster filters
- Use SkiaSharp for rendering and PdfPig for parsing
- Fully compatible with modern containerized Linux print environments

## Usage

```
spectre -i input.pdf -o output.png -r 300
```

## Status

🚧 Work in progress: current output is mocked raster. Real PDF rendering coming next.

---
MIT License.