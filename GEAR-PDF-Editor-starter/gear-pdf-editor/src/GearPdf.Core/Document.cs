// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
namespace GearPdf.Core;
public record Vec(double X, double Y);
public enum Tool { Pen, Highlighter, Line, Rectangle, Ellipse, Eraser, Select }
public record ToolPreset(Tool Tool, string Colour, double WidthMm, double Opacity);
public sealed class Mark {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Tool Tool { get; set; }
    public string Colour { get; set; } = "#e34444";
    public double WidthMm { get; set; } = .5;
    public double Opacity { get; set; } = 1;
    public List<Vec> Points { get; set; } = [];
}
public sealed class Page {
    public double WidthMm { get; set; } = 210;
    public double HeightMm { get; set; } = 297;
    public double GridSpacingMm { get; set; } = 5;
    public double GridOpacity { get; set; } = .25;
    public List<Mark> Marks { get; set; } = [];
}
public sealed class Document {
    public int Version { get; set; } = 1;
    public List<Page> Pages { get; set; } = [new()];
}
public static class DocumentFile {
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static string Encode(Document doc) => JsonSerializer.Serialize(doc, Options);
    public static Document Decode(string json) {
        var doc = JsonSerializer.Deserialize<Document>(json, Options) ?? throw new InvalidDataException("Empty document.");
        if (doc.Version != 1 || doc.Pages is null || doc.Pages.Count is < 1 or > 1000) throw new InvalidDataException("Unsupported document version or page count.");
        foreach (var p in doc.Pages) {
            if (!double.IsFinite(p.WidthMm) || !double.IsFinite(p.HeightMm) || p.WidthMm is < 10 or > 2000 || p.HeightMm is < 10 or > 2000 || p.GridSpacingMm is < 1 or > 100 || !double.IsFinite(p.GridSpacingMm) || !double.IsFinite(p.GridOpacity) || p.GridOpacity is < 0 or > 1 || p.Marks is null) throw new InvalidDataException("Invalid page settings.");
            foreach (var m in p.Marks) {
                if (!Enum.IsDefined(m.Tool) || m.Tool is Tool.Select or Tool.Eraser || m.Points is null || m.Points.Count == 0 || m.Points.Count > 1000000 || m.Points.Any(v => v is null || !double.IsFinite(v.X) || !double.IsFinite(v.Y)) || !double.IsFinite(m.WidthMm) || m.WidthMm is <= 0 or > 50 || !double.IsFinite(m.Opacity) || m.Opacity is < 0 or > 1 || m.Colour is null || !System.Text.RegularExpressions.Regex.IsMatch(m.Colour, "^#[0-9a-fA-F]{6}$")) throw new InvalidDataException("Invalid annotation.");
            }
        }
        return doc;
    }
    public static void Save(string path, Document doc) {
        Decode(Encode(doc));
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, Encode(doc)); File.Move(temp, path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
public sealed class History {
    private readonly Stack<string> undo = new(), redo = new();
    public void Record(Document doc) { undo.Push(DocumentFile.Encode(doc)); redo.Clear(); }
    public Document Undo(Document current) { if (undo.Count == 0) return current; redo.Push(DocumentFile.Encode(current)); return DocumentFile.Decode(undo.Pop()); }
    public Document Redo(Document current) { if (redo.Count == 0) return current; undo.Push(DocumentFile.Encode(current)); return DocumentFile.Decode(redo.Pop()); }
    public void Clear() { undo.Clear(); redo.Clear(); }
}
