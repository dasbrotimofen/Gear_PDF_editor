# Architecture

The first milestone uses C#/.NET 10 and WPF. WPF was chosen for this implementation; WinUI was discussed previously but not committed to. Windows is the first target.

`GearPdf.Core` owns document/page/mark data, millimetre coordinates, JSON validation and snapshot undo history. It has no WPF dependency. `GearPdf.App` owns the window, gear controls, property menus, file dialogs and pointer handling.

Keeping two projects for the first slice avoids empty assemblies. Extract `Input`, `UI`, and `Annotations` when there is enough implementation to justify them. A future `GearPdf.Pdf` project will own PDF adapters.

Document format v1: plain JSON, one document with pages, background settings and ordered marks. Widths and geometry are in millimetres. Each mark has an ID, tool, colour, width, opacity and points. Save validates and writes a temporary sibling before replacement. No PDF objects are required for blank pages.

The renderer maps millimetres to display coordinates through scale and translation; screen DPI is not used to claim exact physical display size. Grid spacing remains 5 mm in document space.

The first input pipeline renders on the UI thread. Device testing and performance profiling must precede any latency claim. Side-button bindings and pressure are intentionally pending.

Current undo uses whole-document JSON snapshots, suitable for initial testing but potentially expensive for large sketches. Replace with bounded commands before production use. Page grid-opacity changes currently bypass history.

PDF work starts with a separate licence/API investigation and an annotation round-trip spike. No engine has been selected or bundled. Render only visible pages; own live ink independently of the PDF backend.
