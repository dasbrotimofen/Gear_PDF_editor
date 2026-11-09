# Changes

## Obsidian viewer 0.1.0

- Added a separate, minimal AGPL Obsidian plugin under `obsidian-plugin/`, with compiled manual-install files.
- Registered `.gear` files for read-only tabs with all pages, responsive previews and external-save refresh.
- Supports native v1–v3 geometry, images/backgrounds, rotations, styles and hidden layers without writing to source files.
- Verified against GEAR PDF exports and in a disposable Obsidian 1.13.7 vault; see the plugin's validation notes.

## Starter update 1

- Replaced system-menu styling with explicit dark surfaces, light labels and a contrasting hover state.
- Removed global TextBlock foreground styling that affected light system controls; set paper dropdown foreground explicitly.
- Replaced external rectangular tool buttons with eight radial sectors inside an expanded gear outline.
- Added hub click/tap expansion, mouse/touch dragging, viewport bounds and Escape-to-collapse.
- Gear position is session-only. Windows visual and hardware validation remains pending.

## Starter update 2

- Pen 1, Pen 2 and Highlighter sectors show their preset colour in a white-backed swatch.
- Colour previews refresh when tool properties change.
- Tooltips include colour, width and opacity; preview swatches keep the full sector clickable.

## Starter update 3

- E toggles object erasing and restores the previous tool/preset.
- P selects the last gear pen slot. Tablet drivers can map pen buttons to these keys.
- Repeated key-down events are ignored; text inputs do not trigger E/P tools.

## Starter update 4

- Tool properties extend radially from the active tool sector.
- Attached inner property arc: Colour / Width / Opacity; outer arc: choices.
- Tap an already selected primary colour to open its shades; tap Colour to return to families.
- Removed the separate tool-properties panel.

## Starter update 5

- Added a third independently configured pen, with green default and live preview.
- Expanded the gear to nine sectors; property-layer angles and P shortcut include Pen 3.
- Moved favourites into an independent draggable floating bar with horizontal/vertical orientation toggle.
- Bar includes Save preset, colour indicators, tooltips, removal and overflow scrolling.
- Favourites and bar layout remain session-only.

## Starter update 6

- Added clipboard image paste, embedded PNG assets, image selection/move/proportional resize, copying and deletion.
- Added PDFium import of existing PDF pages as backgrounds and PDFsharp flattened export with optional grid.
- Added native format v2 with v1 migration and asset-aware bounded undo.
- Added PDF/image persistence and render/export checks, licence inventory and notices.
- Windows clipboard and UI checks remain pending.

## Starter update 7

- Added image rotation left/right by 90 degrees and a custom-angle dialog.
- Rotation participates in undo/redo, native saves, internal copying and PDF export.
- Selection/hit testing and proportional resizing account for image rotation.

## Starter update 8

- Added per-object layer panel, lock/visibility and front/back ordering. New pasted images start locked; existing images can be locked together.
- Added page selection, deletion (with last-page guard), reordering and undo/redo.
- Added independent page reference preview and resizable reference window; adding a page references the previous working page.
- Added solid/dotted/dashed/dot-dashed strokes across drawing tools, annotation properties, native persistence and PDF export.
- Gear buttons accept direct stylus/touch presses and mouse press activation. Windows input behaviour remains unverified on hardware.
- Disabled WPF flicks on sketch surfaces; added Ctrl+Shift+Z redo.
- Native format v3 preserves layer flags and styles; v1/v2 files remain readable.
- Pinned the PDF dependency's System.Security.Cryptography.Pkcs package to 10.0.13 in response to NuGet vulnerability warnings.

## 0.9.2-alpha — 2026-11-09

- Split the Layers panel into Images and Strokes tabs, with Images selected by default.
- Kept selection, lock, visibility and document-wide stacking controls for both tabs; fixed PDF backgrounds appear under Images.
