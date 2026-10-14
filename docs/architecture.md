# Architecture

GearPdf.Core owns millimetre-based document/page/mark geometry, embedded PNG assets, native v2 JSON persistence and bounded undo/redo. V1 files migrate on load.

GearPdf.App is the Windows WPF UI and input layer. Clipboard bitmaps are normalized to PNG/BGRA32; images are selected after pasting and can move/resize. Cached WPF bitmap sources are cleared when a document is replaced.

GearPdf.Pdf owns PDF import/export. Docnet.Core/PDFium renders imported page backgrounds at 2 pixels per PDF point (approximately 144 dpi); original page dimensions are derived from rendered dimensions with rounding. Imports are serialized because PDFium is not thread-safe. Raster output uses a small project-owned PNG encoder. PDFsharp writes the backgrounds and separately stored markup into new PDF page content. No original PDF is embedded or rewritten.

Native documents store assets once in a dictionary; marks and backgrounds refer to asset IDs. Undo snapshots share immutable asset bytes while snapshotting geometry, preventing image data duplication for every stroke. History is capped at 100 snapshots.

PDF export is flattened. It does not round-trip searchable text, forms, signatures, links or standard PDF annotations. Future work should preserve original page objects and test interoperability before promising such capabilities.

Limits: 25 PDF pages / 50 MB source, 40 megapixels per image, 128 MB of embedded asset bytes, 200 MB on native open. All media stay local to the application.
