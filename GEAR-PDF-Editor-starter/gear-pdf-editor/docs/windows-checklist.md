# Windows acceptance checklist

- Build solution; run Core.Checks; launch the application.
- Draw dots and long strokes with mouse and pen, crossing page edges.
- Exercise all shapes, object erase, selection movement, property edits and undo/redo.
- Click/tap the collapsed gear to expand it; check all eight tool sectors lie inside its outline.
- Click/tap its hub again or press Escape to collapse; drag its hub with mouse and touch.
- Drag near each viewport edge; resize the window and check the gear remains reachable.
- Check canvas drawing/panning still works outside the gear bounds.
- Open the canvas and favourite context menus; check normal, hovered and disabled text contrast.
- Check paper-size selected value and all dropdown choices remain readable.
- Tap active tool, choose a primary colour, tap it again and select a shade.
- Change paper size/grid visibility, create pages and switch between them.
- Save/open a multi-page sketch; confirm editable points and grid values survive.
- Cancel New/Open/Close with unsaved work; reject failed save without losing the document.
- Test touch pan/pinch, touch during pen strokes, pen inversion, pointer capture loss and leaving the window.
- Check high-DPI screens and resizing; Fit page should keep the paper visible.
- Pen side buttons remain pending; record the device model and actual Windows events.

Do not treat successful compilation or model checks as validation of pen/touch behaviour.

- Check Pen 1/Pen 2/Highlighter colour swatches; change each tool colour and confirm its swatch updates.
- Check black and white swatches remain distinguishable; swatch clicks should activate the tool sector.

- Map a pen button to custom keyboard E; press once to erase whole strokes and again to restore the exact previous tool.
- Map another button to P; confirm it selects the last used gear pen. Holding a key must not repeatedly toggle tools.

- Reselect Pen 1: check attached property arcs extend upwards. Repeat for Pen 2 and Highlighter at their respective angles.
- Choose Colour/Width/Opacity; check each preset updates the tool and preview.
- Tap the selected primary colour to open shades; tap Colour to return to families.
- Switch tools, collapse and drag the gear; check property arcs follow it and fit near viewport edges.

- Confirm all three pen slots retain independent colours/widths/opacity and aligned property arcs.
- Check P returns to Pen 3 after selecting it and toggling E.
- Drag the favourite-bar header with mouse and touch; flip its orientation near each viewport edge.
- Add/remove favourites; check the scrollbars allow access when the bar has many presets.
- Click/draw outside the bar; verify the transparent overlay does not intercept canvas input.
