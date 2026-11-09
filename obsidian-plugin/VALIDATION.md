# Validation — 2026-11-09

## Automated browser and source checks

- TypeScript strict type-check and esbuild bundle completed.
- GEAR's real C# serializer generated a three-page `.gear` fixture (210 × 297, 297 × 210 and 150 × 150 mm), then its real PDF exporter and PDFium importer generated reference pixels.
- Browser SVG screenshots compared with those reference pixels at matching dimensions. Mean absolute RGB channel error (0–255): **1.859, 2.201, 0.755**; pixels with any channel difference above 40: **0.49%, 3.08%, 0.72%**. Thresholds: mean below 5 and differing pixels below 4%. These are tolerant raster comparisons, not a claim of pixel identity.
- Fixture includes a PDF page background, transparent PNGs, 0/30/90-degree image rotations, overlapping transparent highlighter/freehand strokes, lines, rectangles, ellipses, all four stroke styles, single-point dots, page-edge clipping, varied grids, and hidden image/stroke layers.
- Parser accepts versions 1/2/3 with legacy defaults; rejects malformed files, unknown versions, invalid geometry/layer flags and missing image references.
- View tests with a public-API stub exercised fresh reads on modification, readable errors, cancellation of stale reads, debounced refresh, and close cleanup. The stub rejects all vault APIs except read/event registration/removal. It is not Obsidian.
- Source fixture SHA-256 unchanged after preview rendering.

Reference generator completed with cached packages; NuGet emitted NU1900 because the sandbox could not reach its vulnerability-data endpoint. This did not prevent generation or indicate a failed rendering check.

## Actually tested inside Obsidian

Automated UI test against installed **Obsidian 1.13.7 / Electron 43.3.0, Windows x64**, using a disposable vault and separate profile. Only the file explorer core plugin and GEAR Viewer were enabled; no extension-management plugin or “show all file types” option.

- Accepted the disposable vault's first-run plugin trust prompt.
- `.gear` was visible in the actual file explorer and clicking it opened a `gear-viewer` tab.
- All three pages appeared in order; measured page bounds preserved proportions, fitted the tab, scrolled vertically, and did not overflow horizontally.
- External atomic replacement (temporary file renamed over the destination, like GEAR's saves) and direct writes triggered real vault refresh; replacement page dimensions appeared without reopening.
- Malformed JSON and unsupported-version errors appeared; valid external saves recovered the preview.
- Source hashes matched before and after previewing. Only the test's intentional external writes changed the fixture.
- Closing the tab and disabling the plugin completed without leaving preview elements.
- An actual Obsidian screenshot was visually inspected for the background, grid, rotations, transparent strokes, dash patterns, shapes and basic layout.

The actual-app check covers integration; pixel comparisons use headless Edge and GEAR's exported PDFs. Older Obsidian versions, macOS, Linux, mobile, user-installed themes, multiple pop-out windows and large-document performance were not exercised. No editing/saving API is implemented by the viewer.
