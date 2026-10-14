# Interaction decisions and implementation status

| Input | Target behaviour | Starter |
|---|---|---|
| Mouse left drag | Current tool | Implemented |
| Mouse right click | Cut/copy/paste/delete/properties | Implemented; select an object first |
| Middle drag | Pan | Implemented |
| Ctrl + wheel | Zoom at pointer | Implemented |
| Pen tip | Current tool | Implemented in source; hardware test pending |
| Pen button 1 | Drawing mode | Map driver keyboard shortcut P |
| Pen button 2 | Whole-object erase | Map driver keyboard shortcut E (toggle) |
| Inverted pen tip | Whole-object erase | Implemented in source; hardware test pending |
| One finger | Pan | Implemented in source; hardware test pending |
| Two fingers | Pinch zoom | Implemented in source; hardware test pending |
| Active tool tap | Properties | Implemented for drawing tools |
| Colour tap | Primary colour | Implemented |
| Active primary colour tap | Colour family | Implemented |
| Favourite tap | Exact saved preset | Implemented; session only |

The gear starts collapsed. Click/tap its hub to expand nine tool sectors inside a toothed gear outline. Click/tap the hub again or press Escape to collapse. Drag the hub to move the gear with mouse, pen-promoted mouse events or touch. A five-DIP movement threshold separates dragging from clicking. The expanded outline stays inside the available canvas; near an edge it may shift inward. Position is retained only for the current session. Clicking the already active drawing sector opens its properties. Paper controls sit outside the tool menu. Default page: A4 portrait, 5 mm grid, 25% visibility. These defaults are implementation choices available to change, not additional user requirements.

Selection uses stroke proximity. Drag moves the whole object. Context Properties changes the selected mark. Tool properties affect subsequent marks only. Favourite changes never overwrite the stored session preset; Save preset creates a new favourite.

Touch is ignored while a drawing/edit gesture is active. No pressure-dependent width or partial stroke erasing. Native pen side-button events vary by hardware and must be investigated before assigning the requested two-button scheme.

Keyboard E toggles whole-object erase and restores the prior tool/preset on a second press. P selects the most recently selected gear Pen 1/Pen 2/Pen 3. Key repeat is ignored for these shortcuts. Driver mapping is toggle-based, not hold-to-erase. Keyboard focus must be in GEAR; text inputs ignore these shortcuts.

Tool properties are attached radial layers aligned to the tool angle (Pen 1 at twelve o'clock). The first outer arc offers Colour, Width, Opacity; the next offers presets. Tapping the selected primary colour enters its shade family; tapping Colour returns to primary families. Choices retain the open layers for further editing. Changing tools closes the property layers. The gear shifts inward near edges to fit its layers.

The nine-sector gear includes three independently configured pens (red, blue and green defaults), highlighter, eraser, line, rectangle, ellipse and selection. The favourites bar floats independently of the gear. Drag its header to move it; use its orientation button to flip its preset strip between horizontal and vertical. Add presets with Save preset; remove via a favourite's context menu. Position, orientation and presets are session-only in this starter.

Ctrl+V pastes a clipboard image when no internal GEAR clipboard object is active. Pasted images switch to Select; body drag moves them and the bottom-right square resizes proportionally. Copying an image also places its bitmap on the Windows clipboard. Import PDF replaces the document after an unsaved-work prompt; page arrows navigate imported pages. Export PDF asks whether to include the visible grid and creates flattened page content. Native Save retains editable GEAR marks and embedded page/image assets.
