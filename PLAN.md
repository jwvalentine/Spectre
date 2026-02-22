# Spectre — Production Readiness Plan

## Tier 1 — Must fix before real use

---

### 1. CUPS stdin support

**Why**
CUPS does not guarantee a file path. When the job originates from a pipe or a
network queue, CUPS passes `"-"` as the filename or omits it entirely and
streams the document to the filter's `stdin`. The current code unconditionally
reads `args[5]` as a file path and will crash on many real CUPS configurations.
This is a fundamental CUPS filter contract requirement.

**What to do**
- In `CupsFilterRunner`, check whether `args[5]` is `"-"` or absent.
- When stdin is the source, copy it to a temp file before handing it to
  PDFtoImage (PDFium requires a seekable stream or a real file).
- Delete the temp file in a `finally` block alongside the intermediate PNGs.
- Accept both 5-argument and 6-argument invocations (CUPS spec allows both).

**Files:** `Modes/CupsFilterRunner.cs`

---

### 2. DPI from CUPS job options

**Why**
203 DPI is hardcoded in `CupsFilterRunner`. The CUPS options string (`args[4]`)
carries key=value pairs including `Resolution=300dpi`. A 300 DPI printer
receiving 203 DPI raster data will print at the wrong physical size — labels
will be either clipped or scaled incorrectly.

**What to do**
- Parse `args[4]` (space-separated `key=value` pairs) into a dictionary.
- Extract `Resolution` and strip the `dpi` suffix to get an integer.
- Fall back to 203 DPI if the key is absent.
- Pass the resolved DPI through to `PdfRenderer.RenderPages()`.
- Consider also extracting `copies` (`args[3]`) and honoring it by repeating
  ZPL output rather than relying on the printer's internal copy logic.

**Files:** `Modes/CupsFilterRunner.cs`

---

### 3. ZPL run-length encoding (RLE)

**Why**
The current `ZplConverter` emits every pixel as two hex characters with no
compression. A 4" × 6" label at 203 DPI (812 × 1218 px) produces roughly
200 KB of ZPL per label. Many Zebra printers have receive-buffer limits
around 64–100 KB and will silently truncate or reject oversized jobs. Even
where the data fits, transfer time is a real bottleneck over serial and older
network connections.

**What to do**
ZPL supports a compact repeat-byte encoding within `^GFA` data:

| Sequence | Meaning |
|----------|---------|
| `:` | Repeat previous byte to end of row |
| `_` | Row of all `00` bytes (blank line) |
| `G`–`Z` (before hex pair) | Repeat following byte 1–20 times |
| `g`–`z` (before hex pair) | Repeat following byte 21–400 times |

- After building each row's raw hex, apply RLE before appending.
- A row of all zeros becomes `_` (one character vs. `widthBytes × 2`).
- A row of all `FF` becomes the appropriate repeat prefix + `FF`.
- Mixed rows should be compressed run-by-run.
- Add a simple before/after byte-count log line to `stderr` so compression
  ratio is visible during testing.

**Files:** `Zpl/ZplConverter.cs`

---

## Tier 2 — Important for correctness

---

### 4. Label bounds and scaling

**Why**
PDFtoImage renders the PDF at its native dimensions multiplied by DPI. If the
source PDF is A4 (210 × 297 mm) but the physical label is 4" × 6"
(101.6 × 152.4 mm), the rendered image will be much larger than the printable
area. The printer will clip silently, cutting off content.

**What to do**
- Define a target width and height in dots (physical label size × DPI).
- Pass `Width` and `Height` to `RenderOptions` in `PdfRenderer` to constrain
  the output, and set `WithAspectRatio: true` so the image scales uniformly
  within those bounds.
- Source the label dimensions from CUPS options (`PageSize`, `MediaSize`) if
  present, or accept `-w` / `-h` flags in CLI mode.
- If no size hint is available, render at native PDF size and document the
  assumption clearly.

**Files:** `Rendering/PdfRenderer.cs`, `Modes/CupsFilterRunner.cs`,
`Modes/CliRunner.cs`

---

### 5. CUPS log formatting

**Why**
CUPS parses `stderr` output from filters and routes lines into
`/var/log/cups/error_log` based on prefix. Unprefixed lines are treated as
`DEBUG` but may be suppressed in default log-level configurations, making
diagnosis of production failures unnecessarily hard.

**What to do**
- Replace bare `Console.Error.WriteLine(...)` calls with a small static helper,
  e.g. `CupsLog.Info(...)`, `CupsLog.Error(...)`, that prepends the correct
  CUPS prefix:

  ```
  DEBUG: Spectre: page 1 rendered (812x1218) → /tmp/job001.png
  ERROR: Spectre filter failed: No pages rendered from: /tmp/job001.pdf
  ```

- Use `DEBUG:` for per-page progress, `INFO:` for job-level summaries,
  `WARNING:` for recoverable issues, `ERROR:` for failures.

**Files:** new `Logging/CupsLog.cs`, all callers

---

### 6. Input validation

**Why**
Passing a non-PDF or a zero-byte file directly to PDFium produces an unhelpful
exception stack trace on `stderr`. In CUPS this surfaces as a generic filter
failure with no actionable message for the administrator.

**What to do**
- Before calling `PdfRenderer.RenderPages()`, verify the file exists and is
  non-empty.
- Read the first 5 bytes and confirm the `%PDF-` magic header.
- Emit a clear `ERROR:` message and exit with code `1` if either check fails.

**Files:** `Modes/CupsFilterRunner.cs`, `Modes/CliRunner.cs`

---

## Tier 3 — Packaging and confidence

---

### 7. Automated tests

**Why**
There are currently zero automated tests. Any change to `ZplConverter`,
`PdfRenderer`, or the runners is unverified. A CI-blocking test suite is the
minimum bar for a shared or production codebase.

**What to do**
- Add an `xUnit` or `NUnit` test project (`Spectre.Tests`).
- Create a small, committed test PDF (`Tests/Assets/multipage.pdf`) with known
  content — at least 3 pages, mix of text and a simple vector shape.
- Write tests covering:
  - `PdfRenderer.RenderPages()` returns the correct page count.
  - Each rendered PNG has the expected dimensions for the given DPI.
  - `ZplConverter.ConvertPngToZpl()` produces output starting with `^XA` and
    ending with `^XZ`.
  - After RLE is implemented: compressed output is smaller than uncompressed
    for a representative label.
  - `CupsFilterRunner` handles a stdin source (`"-"`) without crashing.
- Run tests in CI on Linux, macOS, and Windows.

**Files:** new `Spectre.Tests/` project

---

### 8. Self-contained publish and packaging

**Why**
Deploying to a CUPS server currently requires .NET 8 runtime to be installed
separately. A self-contained single-file binary removes that dependency and
makes the filter a simple file copy.

**What to do**
- Add publish profiles in `Properties/PublishProfiles/` for:
  - `linux-x64` (primary CUPS target)
  - `linux-arm64` (Raspberry Pi / ARM servers)
  - `win-x64` (Windows testing)
  - `osx-x64` and `osx-arm64` (macOS testing)
- Set `<PublishSingleFile>true</PublishSingleFile>` and
  `<SelfContained>true</SelfContained>` in each profile.
- Write an `install.sh` that copies the binary to `/usr/lib/cups/filter/spectre`
  and sets the correct permissions (`chmod 755`).
- Add a `Dockerfile` based on `mcr.microsoft.com/dotnet/runtime:8.0` for
  containerized CUPS deployments.

**Files:** `Spectre.csproj`, new `Properties/PublishProfiles/`, `install.sh`,
`Dockerfile`

---

### 9. PPD / CUPS filter registration

**Why**
Currently, integrating Spectre into a CUPS printer requires manually editing
PPD files or `cups-filters.conf`. A minimal PPD and a registration snippet
make setup a documented, repeatable one-step operation.

**What to do**
- Write a minimal `spectre.ppd` declaring:
  - Supported MIME type: `application/pdf`
  - Filter cost and path: `*cupsFilter: "application/pdf 0 spectre"`
  - Common `Resolution` options: `203dpi`, `300dpi`, `600dpi`
  - Common `PageSize` options for standard label stock (4×6, 2×1, etc.)
- Document how to register the PPD with `lpadmin`.
- Include a sample `docker-compose.yml` for a CUPS + Spectre container.

**Files:** new `packaging/spectre.ppd`, `packaging/docker-compose.yml`,
updated `README.md`
