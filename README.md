# GEAR PDF Editor

Great everthing appears Wo(r)king PDF Editor

A Windows-first sketching application with a gear menu. This is an early **source prototype**, not a released PDF editor. Blank-page sketching comes first; PDF import/export is deferred.

## Validation

The solution cross-compiled with .NET 10.0.401 with zero warnings/errors. Core checks passed for geometry/grid round trips, undo/redo, invalid-file rejection and disk save/overwrite. The Windows UI and hardware input have not been run in this environment.

## Run on Windows

Install the .NET 10 SDK, then from this directory:

```powershell
dotnet run --project src/GearPdf.App/GearPdf.App.csproj
```

Or open `GearPdf.slnx` in a compatible Visual Studio version. To publish a self-contained Windows x64 folder:

```powershell
dotnet publish src/GearPdf.App/GearPdf.App.csproj -c Release -r win-x64 --self-contained true -o dist/win-x64
```

## Implemented in source

- A4/A3 portrait and landscape, Letter portrait; multiple pages.
- 5 mm grid with visibility from 0–100%, stronger lines every 25 mm.
- Three independent pen slots with colour previews.
- Pen, freehand highlighter, line, rectangle, ellipse, whole-object eraser.
- Selection, moving, Delete, internal Cut/Copy/Paste, annotation properties.
- Undo/redo for annotation changes, page addition and paper resizing.
- Gear hub expands into eight tool sectors inside a toothed gear; click the hub to collapse.
- Drag the hub to reposition the gear; Escape collapses it. Click an active tool again for attached outer Colour/Width/Opacity arcs.
- Pen/highlighter gear sectors show live colour swatches, with width and opacity in their tooltips.
- Explicit high-contrast context-menu colours and readable paper-size dropdown.
- Five colour families; select the primary colour, then tap it again for finer colours.
- Width/opacity presets; session-only favourites, with temporary edits kept separate.
- Floating favourites bar: drag the header, flip horizontal/vertical, save or remove presets.
- Mouse drawing; middle-button pan; Ctrl+wheel zoom; wheel vertical pan.
- Driver-mappable shortcuts: E toggles object eraser/previous tool; P activates the most recently selected Pen 1/Pen 2/Pen 3.
- Stylus drawing and inverted-tip erasing; touch pan and two-finger zoom.
- Editable JSON `.gear` files with format versioning, Save/Save As and unsaved-change prompts.

## Current limits

- No PDF engine, PDF import, export or PDF annotation writing yet.
- Pen side buttons can be assigned to E/P keyboard shortcuts in the tablet driver. Raw device-specific button mapping is not implemented; hardware testing remains pending.
- Pressure and low-latency ink smoothing are deferred. This custom drawing surface uses the UI thread.
- Favourites are not yet saved between sessions. No custom colour picker or favourite reorder/edit dialog.
- Gear position is session-only; expansion stays inside the available canvas and may shift near edges.
- Grid visibility changes are saved but are not recorded in undo history.
- No resize handles. Object erasing tests stroke outlines, not filled interiors.
- Cut/copy/paste works inside this application only, not the Windows clipboard.
- Page-size changes clip marks outside the paper without deleting them.
- The paper combo is a size action; it does not reflect sizes when switching/opening pages.
- File limit on open: 50 MB. JSON is a v1 starter format; embedded assets/ZIP come later.
- Windows UI, touch and pen behaviour require testing on Windows hardware.

## Checks

```powershell
dotnet build GearPdf.slnx
dotnet run --project tests/GearPdf.Core.Checks
```

See `docs/windows-checklist.md` before calling this a usable alpha.

## Repository

No public repository or deployment is part of this starter. Keep the remote repository private during development. The package includes a Windows build/check workflow for when it is added to GitHub.

## Licence

Project source: **AGPL-3.0-or-later**. Full licence in `LICENSE`. Third-party/runtime inventory in `THIRD_PARTY_NOTICES.md`. PDF dependency selection and its licence review remain open.
