# GEAR PDF Editor

Great everthing appears Wo(r)king PDF Editor

A Windows-first sketching and document-markup prototype with a movable gear menu. Project source is AGPL-3.0-or-later. Keep the repository private during initial development.

## Start on Windows

Latest alpha: **0.9.2-alpha**, packaged as [GEAR-Windows_alpha_0.9.2.zip](Release/GEAR-Windows_alpha_0.9.2.zip). Extract the ZIP before launching.

The compiled `GEAR-Windows` folder requires the **.NET 10 Desktop Runtime x64**. Run `GearPdf.App.exe`. For source development, install the .NET 10 SDK and run:

```powershell
dotnet run --project src/GearPdf.App/GearPdf.App.csproj
```

## Images and PDFs

- Copy a screenshot or image, focus GEAR, and press **Ctrl+V**, or use **Paste image**.
- The image is embedded in the sketch and selected after insertion. New pasted images start locked. Untick **Lock** in the Layers panel, then drag the selected image to move it; drag its bottom-right square to resize while preserving its aspect ratio.
- To rotate a placed image, select it and right-click: **Rotate left 90°**, **Rotate right 90°**, or **Rotate image…** for a custom clockwise angle. Rotation is retained in native saves, internal copies and PDF export.
- Select a pen to draw over the image. Undo/redo covers insertion, movement, resizing and deletion.
- **Import PDF** opens an existing PDF as a new GEAR document, replacing the current sketch after the unsaved-work prompt. It imports all pages as rendered backgrounds with the grid off. Use the page arrows to navigate and annotate.
- **Save / Save As** writes an editable `.gear` file, including embedded images, PDF page backgrounds and your separate markup. Older v1/v2 sketches remain readable; saves use format v3. New saves require this update or later.
- **Export PDF** creates a portable, flattened copy of all pages. Choose whether to include the visible grid. Save a `.gear` copy as well to retain editable markup.

**PDF limitation:** imported pages are raster backgrounds rendered at approximately 144 dpi, not live PDF objects. Original PDF text/searchability, form fields, links, signatures and editable existing annotations are not preserved in the exported copy. GEAR markup is flattened into page content, not standard editable PDF annotations. The original PDF is not modified by importing; exporting to the currently imported source path is blocked.

## Drawing and navigation

- A4/A3 portrait/landscape and Letter; multiple blank pages.
- 5 mm grid with 0–100% visibility; major lines every 25 mm.
- Three independently configured pen slots, highlighter, line, rectangle, ellipse, whole-object eraser and selection.
- Nine-sector gear with colour previews; click its hub to expand/collapse, drag the hub to move it.
- Click an active drawing tool again for attached Colour / Width / Opacity / Style arcs and presets. Tap the selected primary colour again for its shades.
- Floating favourites bar with header dragging and horizontal/vertical flip. Save a preset or right-click a favourite to remove it.
- Mouse draws; middle drag pans; wheel pans vertically; Ctrl+wheel zooms.
- Stylus draws; inverted tip erases. Finger pan and two-finger zoom are implemented in source.
- **E** toggles object erasing and restores the previous tool on a second press. **P** selects the last gear pen. Map tablet-driver buttons to these keys.
- **Ctrl+Z** undo, **Ctrl+Y** or **Ctrl+Shift+Z** redo. Right-click: Cut / Copy / Paste / Delete / Properties. Delete removes selection.

## Layers, pages and references

- The right Layers panel separates images and drawing objects into **Images** and **Strokes** tabs, each frontmost first. **Images** is selected by default. Select a row to select that object, including locked images.
- **Lock** protects an object from erasing, moving, resizing, rotation, property edits and deletion. **Lock all images** protects images in existing sketches. Unlock an image before positioning it, then lock it again to draw over it.
- The left checkbox controls visibility, including PDF export. **Forward / Backward / To front / To back** change stacking order across both tabs, including for locked objects. PDF backgrounds remain fixed beneath all objects.
- Layers are individual objects, not named groups or paint layers. Locking keeps images editable later; no destructive image flattening is applied.
- In the Pages list, select, add, delete or move a page up/down. One page must remain. Page deletion and reordering support undo/redo.
- **Page reference** shows a chosen page independently of the working page. Adding a page sets the previous working page as reference. **Enlarge reference** opens a resizable reference window that can be placed beside the editor; use the main reference selector to change it.
- Gear **Style** choices: Solid, Dotted, Dashed, Dot-dash. Applies to pen, highlighter, straight lines, rectangles and ellipses; saved in favourites for the session. Existing unlocked strokes can be changed through **Properties**.
- Lock, visibility, order and stroke style are saved in `.gear` and participate in undo/redo. PDF export preserves order and stroke patterns and omits hidden objects.

## Limits

- Windows clipboard, canvas rendering, pen and touch input still need hardware testing.
- Import: at most 25 PDF pages, source file at most 50 MB. Each image/rendered page at most 40 megapixels; embedded image bytes at most 128 MB. Opening `.gear`: at most 200 MB.
- Native storage is JSON with embedded PNG bytes, not a compressed ZIP archive. Imports and large images increase file size. Undo holds up to 100 metadata snapshots and shares immutable image data.
- Favourites, bar orientation/position and gear position are session-only. No pressure, ink smoothing, autosave or custom colour picker yet.
- Image resizing is supported. Shape resize handles are not yet implemented. Image opacity property editing is not implemented.
- Object erasing removes entire unlocked strokes, shapes or placed images. Locked and hidden layers are skipped. Imported PDF backgrounds are not erasable objects.
- Paper-size changes clip content outside the page without deleting it; the paper combo is a size action, not a current-size indicator.
- Grid visibility changes are saved but do not have a separate undo entry.
- Geometry copy/paste stays inside the running GEAR instance. Images can also be copied to the Windows clipboard. Native PNG assets are embedded.

## Build and checks

```powershell
dotnet build GearPdf.slnx
dotnet run --project tests/GearPdf.Core.Checks
dotnet run --project tests/GearPdf.Pdf.Checks
dotnet publish src/GearPdf.App/GearPdf.App.csproj -c Release -r win-x64 --self-contained false -o dist/win-x64
```

PDF export uses PDFsharp; import uses PDFium through Docnet.Core. Exact package versions and bundled notices are in `DEPENDENCIES.md` and `THIRD_PARTY_NOTICES.md`. Windows x64 is the packaged application target. Domain and PDF checks also run on Linux.

Validation includes native image/background persistence, v1 migration, asset-aware undo/redo, missing-asset rejection, PDF export page counts/sizes, PDFium import and rotated-page dimensions. Exported sample pages and their re-imported render were visually inspected. See `docs/windows-checklist.md` for outstanding UI checks.
