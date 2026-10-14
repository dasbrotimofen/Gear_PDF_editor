# Changes

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
