# Spectre

**Spectre** is a minimalist, MIT-licensed replacement for Ghostscript raster filters, written in C# (.NET 8).  
It renders PDFs to raster formats (e.g. PNG or ZPL for label printers) **without using any AGPL components**.

---

## ✨ Goals

- 🧱 Drop-in replacement for Ghostscript's `gstoraster` and `pdftoraster`
- 🖼️ Use **SkiaSharp** for raster rendering (no System.Drawing)
- 💬 Output **ZPL** for Zebra/Intermec label printers
- 🐳 Designed for **containerized Linux + CUPS** environments
- ✅ Fully testable in Visual Studio

---

## 🚀 Usage

### CLI Mode (local testing / debugging)
```bash
spectre -i input.pdf -o output.png -r 300
```

### CUPS Filter Mode (auto-called by CUPS)
```bash
spectre job-id user title copies options input.pdf
```

Output is streamed to `stdout` (e.g., ZPL), suitable for RAW queues.

---

## 🔧 Setup

1. Place the compiled binary in:
   ```
   /usr/lib/cups/filter/spectre
   ```

2. Patch your printer's PPD or use `cups-filters.conf`:
   ```ppd
   *cupsFilter: "application/pdf 0 spectre"
   ```

---

## 🧪 Status

✅ Working:
- PDF → monochrome PNG (SkiaSharp)
- PNG → ZPL (`^GFA`) output
- CLI + CUPS support
- Ghostscript-free

🧱 Next:
- True PDF content parsing/rendering
- Multi-page support
- Optional CUPS Raster output for broader printer support

---

## 🪪 License

MIT License.
