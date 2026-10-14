# Validation recorded

- .NET SDK 10.0.401, Linux cross-build: zero warnings/errors.
- Windows x64 framework-dependent Release publish: completed successfully.
- Core checks: passed geometry/grid round trip, undo/redo, invalid-file rejection, save/overwrite.
- Source XAML/project XML: parsed successfully.
- Windows UI, visual layout and pen/touch hardware: not tested in this environment.

The Windows launch folder requires .NET 10 Desktop Runtime x64.

## Starter update 1

The updated WPF solution cross-compiled with zero warnings/errors, and Windows x64 Release publish completed. Context menu colours, radial geometry and pointer handling were inspected in source. The supplied screenshot was used to identify the original contrast issue. No Windows UI session was available to visually validate the update. Core persistence was unchanged.

## Starter update 2

Pen/highlighter colour swatches and live refresh added. Windows x64 Release publish completed without reported warnings or errors. No Windows visual session was available. Core code was unchanged.

## Starter update 3

E/P keyboard tool switching compiled and Windows x64 Release publish completed without reported errors/warnings. Physical XP-Pen shortcut delivery has not been tested here.

## Starter update 4

Attached radial property arcs compiled and Windows x64 Release publish completed without reported warnings/errors. XAML parsed successfully. Windows layout and pointer validation remains pending.

## Starter update 5

Three-pen gear and draggable orientation-switching favourites bar compiled and Windows x64 Release publish completed without reported warnings/errors. XAML parsed successfully. Windows UI and input behaviour remain pending hardware validation. Core document persistence was unchanged.

## Starter update 6

- Full solution compiled with zero warnings/errors. Windows x64 Release publish completed.
- Existing geometry/grid/undo/save checks passed.
- New checks passed: embedded PNG/background round trip, asset-aware undo/redo, missing-asset rejection and v1 migration.
- PDF export passed page-count and physical-page-size checks. PDFium re-import/render and rotated-page dimension checks passed.
- Two exported pages were rendered with Poppler and inspected; the PDFium-imported first page was also inspected. Images, transparency, grids and markup rendered visibly in the expected positions.
- Windows clipboard delivery, canvas selection/resizing and pen/touch input have not been executed here.

## Starter update 7

Image-angle persistence, rotation undo/redo and rotated resize-anchor/aspect checks passed. The solution built with zero warnings/errors; Windows x64 Release publish completed. A PDF sample containing a 30-degree rotated image was rendered and visually inspected. Windows context-menu and pointer interaction remain pending.
