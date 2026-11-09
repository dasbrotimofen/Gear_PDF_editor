# GEAR Viewer for Obsidian

A small, read-only `.gear` document preview. No GEAR process, PDF export, extension-management plugin, or "show all file types" setting is needed.

## Install

1. Create `<vault>/.obsidian/plugins/gear-viewer/`.
2. Copy **`main.js`, `manifest.json`, and `styles.css`** from this directory into that folder. These compiled files are included in the repository. Keep `LICENSE` with redistributed copies.
3. Reload Obsidian. Under **Settings → Community plugins**, enable community plugins and **GEAR Viewer**.
4. Put a `.gear` file anywhere in the vault and click it in the file explorer.

Pages appear vertically in document order, fitted to the tab width. Scroll normally. Saving in GEAR or another external application refreshes the open preview when Obsidian reports the vault change. Scroll position is retained where possible. Closing the preview or disabling the plugin releases listeners, pending loads and image URLs. The viewer never writes to the document.

Supports GEAR native JSON versions **1, 2 and 3**, using the application's defaults for absent fields. Invalid JSON, unsupported versions, missing/invalid PNG assets and invalid geometry produce an error in the tab; a subsequent valid save recovers automatically. GEAR's 200 MB file, 128 MB embedded-image and 40-megapixel-per-image limits apply. Large documents can still be expensive to load.

Rendering follows `GearPdf.Core/Document.cs`, `GearPdf.App/SketchCanvas.cs`, and `GearPdf.Pdf/PdfExporter.cs`: page dimensions, PDF backgrounds, PNG transparency and rotation, freehand/line/rectangle/ellipse geometry, colours, opacity, dash styles, stacking and visibility. Locked objects remain visible. Selection and resize UI is omitted. Like GEAR itself, image mark opacity is ignored; PNG alpha is preserved. Grid thickness follows PDF export's physical units because the WPF canvas uses fixed screen-pixel thickness. Browser/PDF rasterizers differ slightly in antialiasing, opacity rounding and dash caps. Imported PDF backgrounds have the same raster quality as the original `.gear` file.

## Build

Node.js 22+ and npm are sufficient; the C# application is not part of the plugin build.

```sh
cd obsidian-plugin
npm ci
npm run build
npm run package
```

`npm run build` type-checks and bundles `src/main.ts` into `main.js` with Obsidian externalized. `npm run dev` watches source changes. `npm run package` prepares `release/gear-viewer/` for copying into a vault. Dependencies are development-only; the installed plugin has no third-party runtime dependencies. `manifest.json` is the plugin's independent version (0.1.0), not the Windows application's release number.

## Check

Generate comparison documents and reference pixels with the actual GEAR serializer, PDFsharp exporter and PDFium importer (.NET 10 SDK needed **only for this test**):

```sh
# From repository root
dotnet run --project obsidian-plugin/tests/reference/Generate.csproj -- . obsidian-plugin/test-output
cd obsidian-plugin
npm test
```

Browser tests use installed Microsoft Edge by default. Set `GEAR_BROWSER` to another Chromium executable if needed. Results and screenshots go to ignored `test-output/`. Tests cover parser/defaults, responsive rendering against GEAR PDF exports, source hashes, and view refresh/cleanup using an API stub.

The optional real-app check runs installed Obsidian with a **separate disposable vault and profile** under `test-output/`, accepts that test vault's trust prompt, and closes that instance afterward:

```sh
node tests/obsidian.mjs
```

On Windows it finds `%LOCALAPPDATA%/Programs/obsidian/Obsidian.exe`; elsewhere set `OBSIDIAN_EXE`. It uses local debug port 19383; close any previous test instance before rerunning. Developer-console access in this test is not used by the shipped plugin. See [VALIDATION.md](VALIDATION.md) for checks actually performed.

## Licence

AGPL-3.0-or-later, matching GEAR PDF Editor; see [LICENSE](LICENSE). Source and reproducible build scripts are included here. The plugin uses Obsidian's [public API](https://github.com/obsidianmd/obsidian-api), notably `registerExtensions`, `registerView`, `FileView` and vault events. No toolbar, settings, editor, Markdown embeds or network calls are added.
