// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
namespace GearPdf.Core;
public record Vec(double X, double Y);
public enum Tool { Pen, Highlighter, Line, Rectangle, Ellipse, Eraser, Select, Image }
public record ToolPreset(Tool Tool, string Colour, double WidthMm, double Opacity);
public sealed class Mark {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Tool Tool { get; set; }
    public string Colour { get; set; } = "#e34444";
    public double WidthMm { get; set; } = .5;
    public double Opacity { get; set; } = 1;
    public List<Vec> Points { get; set; } = [];
    public string? ImageAssetId { get; set; }
    public double RotationDegrees { get; set; }
}
public sealed class ImageAsset {
    public byte[] Png { get; set; } = [];
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
}
public sealed class Page {
    public double WidthMm { get; set; } = 210;
    public double HeightMm { get; set; } = 297;
    public double GridSpacingMm { get; set; } = 5;
    public double GridOpacity { get; set; } = .25;
    public List<Mark> Marks { get; set; } = [];
    public string? BackgroundImageId { get; set; }
}
public sealed class Document {
    public int Version { get; set; } = 2;
    public List<Page> Pages { get; set; } = [new()];
    public Dictionary<string,ImageAsset> Assets { get; set; } = [];
}
public static class DocumentFile {
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static string Encode(Document doc) => JsonSerializer.Serialize(doc, Options);
    internal static string EncodeHistory(Document doc) => Encode(new(){Version=doc.Version,Pages=doc.Pages});
    internal static Document DecodeHistory(string json,Dictionary<string,ImageAsset> assets) => Decode(json,assets);
    public static Document Decode(string json) => Decode(json,null);
    private static Document Decode(string json,Dictionary<string,ImageAsset>? assets) {
        var doc = JsonSerializer.Deserialize<Document>(json, Options) ?? throw new InvalidDataException("Empty document.");
        if(assets!=null)doc.Assets=new(assets);
        Validate(doc);doc.Version=2;return doc;
    }
    public static void Validate(Document doc){
        if (doc.Version is not (1 or 2) || doc.Pages is null || doc.Pages.Count is < 1 or > 1000 || doc.Assets is null) throw new InvalidDataException("Unsupported document version or page count.");
        long assetBytes=0;
        foreach(var (id,asset) in doc.Assets){
            if(string.IsNullOrWhiteSpace(id) || asset is null || asset.Png is null || asset.Png.Length<24 || !asset.Png.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}) || System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(asset.Png.AsSpan(16,4))!=asset.PixelWidth || System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(asset.Png.AsSpan(20,4))!=asset.PixelHeight || asset.PixelWidth is <1 or >10000 || asset.PixelHeight is <1 or >10000 || (long)asset.PixelWidth*asset.PixelHeight>40000000)throw new InvalidDataException("Invalid embedded image.");
            assetBytes+=asset.Png.Length;if(assetBytes>128L*1024*1024)throw new InvalidDataException("Embedded images exceed the 128 MB limit.");
        }
        foreach (var p in doc.Pages) {
            if (!double.IsFinite(p.WidthMm) || !double.IsFinite(p.HeightMm) || p.WidthMm is < 10 or > 2000 || p.HeightMm is < 10 or > 2000 || p.GridSpacingMm is < 1 or > 100 || !double.IsFinite(p.GridSpacingMm) || !double.IsFinite(p.GridOpacity) || p.GridOpacity is < 0 or > 1 || p.Marks is null) throw new InvalidDataException("Invalid page settings.");
            if(p.BackgroundImageId!=null && !doc.Assets.ContainsKey(p.BackgroundImageId))throw new InvalidDataException("Missing PDF page background.");
            foreach (var m in p.Marks) {
                if (!double.IsFinite(m.RotationDegrees) || m.RotationDegrees is < -360000 or > 360000 || !Enum.IsDefined(m.Tool) || m.Tool is Tool.Select or Tool.Eraser || m.Points is null || m.Points.Count == 0 || m.Points.Count > 1000000 || m.Points.Any(v => v is null || !double.IsFinite(v.X) || !double.IsFinite(v.Y)) || !double.IsFinite(m.WidthMm) || m.WidthMm is <= 0 or > 50 || !double.IsFinite(m.Opacity) || m.Opacity is < 0 or > 1 || m.Colour is null || !System.Text.RegularExpressions.Regex.IsMatch(m.Colour, "^#[0-9a-fA-F]{6}$")) throw new InvalidDataException("Invalid annotation.");
                if(m.Tool==Tool.Image && (m.ImageAssetId is null || !doc.Assets.ContainsKey(m.ImageAssetId) || m.Points.Count!=2 || m.Points[1].X<=m.Points[0].X || m.Points[1].Y<=m.Points[0].Y))throw new InvalidDataException("Invalid image placement.");
            }
        }
    }
    public static void Save(string path, Document doc) {
        Validate(doc);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, Encode(doc)); File.Move(temp, path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
public sealed class History {
    private sealed record Snapshot(string Json,Dictionary<string,ImageAsset> Assets);
    private readonly List<Snapshot> undo=[],redo=[];
    private static Snapshot Capture(Document doc)=>new(DocumentFile.EncodeHistory(doc),new(doc.Assets));
    private static Document Restore(Snapshot snapshot)=>DocumentFile.DecodeHistory(snapshot.Json,snapshot.Assets);
    public void Record(Document doc) { undo.Add(Capture(doc));if(undo.Count>100)undo.RemoveAt(0);redo.Clear(); }
    public Document Undo(Document current) { if (undo.Count == 0) return current; redo.Add(Capture(current));var value=undo[^1];undo.RemoveAt(undo.Count-1);return Restore(value); }
    public Document Redo(Document current) { if (redo.Count == 0) return current; undo.Add(Capture(current));var value=redo[^1];redo.RemoveAt(redo.Count-1);return Restore(value); }
    public void Clear() { undo.Clear(); redo.Clear(); }
}
