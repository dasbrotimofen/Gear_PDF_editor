// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GearPdf.Core;
namespace GearPdf.App;
public sealed class SketchCanvas : FrameworkElement {
    public Document Document { get; set; } = new();
    public History History { get; } = new();
    public int PageIndex { get; set; }
    public Page Page => Document.Pages[PageIndex];
    public ToolPreset Preset { get; set; } = new(Tool.Pen, "#e34444", .5, 1);
    public event Action? Changed;
    public Guid? Selected { get; set; }
    private double scale = 2.5;
    private Vector offset = new(50, 30);
    private Mark? active;
    private Point last;
    private bool panning, dragging, editing, erasing;
    private readonly Dictionary<int, Point> touches = [];
    public SketchCanvas() { Focusable = true; ClipToBounds = true; }
    private Point Screen(Vec p) => new(p.X * scale + offset.X, p.Y * scale + offset.Y);
    private Vec World(Point p) => new((p.X-offset.X)/scale, (p.Y-offset.Y)/scale);
    private bool Inside(Vec v) => v.X >= 0 && v.Y >= 0 && v.X <= Page.WidthMm && v.Y <= Page.HeightMm;
    public void Fit() { scale = Math.Max(.1, Math.Min((ActualWidth-80)/Page.WidthMm,(ActualHeight-60)/Page.HeightMm)); offset = new((ActualWidth-Page.WidthMm*scale)/2,(ActualHeight-Page.HeightMm*scale)/2); InvalidateVisual(); }
    private static Brush BrushFor(string colour, double opacity) { var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colour)); b.Opacity = opacity; return b; }
    private Geometry GeometryFor(Mark m) {
        var a=Screen(m.Points[0]); var b=Screen(m.Points[^1]);
        if (m.Tool == Tool.Rectangle) return new RectangleGeometry(new Rect(a,b));
        if (m.Tool == Tool.Ellipse) return new EllipseGeometry(new Rect(a,b));
        var g=new StreamGeometry(); using (var c=g.Open()) { c.BeginFigure(a,false,false); foreach(var p in m.Points.Skip(1)) c.LineTo(Screen(p),true,false); if(m.Points.Count==1)c.LineTo(new(a.X+.01,a.Y),true,false); } return g;
    }
    protected override void OnRender(DrawingContext dc) {
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(18,27,38)),null,new Rect(RenderSize));
        var paper=new Rect(offset.X,offset.Y,Page.WidthMm*scale,Page.HeightMm*scale);
        dc.DrawRectangle(Brushes.White,null,paper); dc.PushClip(new RectangleGeometry(paper));
        if(Page.GridOpacity>0) {
            for(int axis=0;axis<2;axis++) {
                double bound=axis==0?Page.WidthMm:Page.HeightMm;
                for(int i=1;i*Page.GridSpacingMm<bound;i++) {
                    double v=i*Page.GridSpacingMm*scale;
                    var pen=new Pen(BrushFor("#6d91ad",Math.Min(1,Page.GridOpacity*(i%5==0?1.5:1))),i%5==0?1:.6);
                    dc.DrawLine(pen,axis==0?new(paper.X+v,paper.Y):new(paper.X,paper.Y+v),axis==0?new(paper.X+v,paper.Bottom):new(paper.Right,paper.Y+v));
                }
            }
        }
        foreach(var m in Page.Marks.Concat(active is null?Array.Empty<Mark>():new[]{active})) {
            var pen=new Pen(BrushFor(m.Colour,m.Opacity),m.WidthMm*scale) { StartLineCap=PenLineCap.Round, EndLineCap=PenLineCap.Round, LineJoin=PenLineJoin.Round };
            var geometry=GeometryFor(m); dc.DrawGeometry(null,pen,geometry);
            if(m.Id==Selected) { var bounds=geometry.Bounds; bounds.Inflate(5,5); dc.DrawRectangle(null,new Pen(Brushes.DodgerBlue,1){DashStyle=DashStyles.Dash},bounds); }
        }
        dc.Pop();
    }
    private Mark? Hit(Point screen) => Page.Marks.AsEnumerable().Reverse().FirstOrDefault(m => GeometryFor(m).StrokeContains(new Pen(Brushes.Black,Math.Max(10,m.WidthMm*scale+6)), screen));
    private void Begin(Point p, bool erase=false) {
        Focus(); last=p; var v=World(p); if(!Inside(v)) return;
        erasing=erase || Preset.Tool==Tool.Eraser;
        if(erasing) { editing=true; History.Record(Document); Erase(p); return; }
        if(Preset.Tool==Tool.Select) { var hit=Hit(p); Selected=hit?.Id; dragging=hit!=null; if(dragging){ History.Record(Document); editing=true; } InvalidateVisual(); return; }
        History.Record(Document); editing=true;
        active=new(){Tool=Preset.Tool,Colour=Preset.Colour,WidthMm=Preset.WidthMm,Opacity=Preset.Opacity,Points=[v]}; InvalidateVisual();
    }
    private void Erase(Point p) { var m=Hit(p); if(m!=null) Page.Marks.Remove(m); InvalidateVisual(); }
    private void Move(Point p) {
        if(erasing) { Erase(p); return; }
        if(dragging && Selected is Guid id) { var mark=Page.Marks.FirstOrDefault(m=>m.Id==id); if(mark!=null) { var delta=(p-last)/scale; mark.Points=mark.Points.Select(v=>new Vec(v.X+delta.X,v.Y+delta.Y)).ToList(); } last=p; }
        if(active!=null) { var v=World(p); v=new(Math.Clamp(v.X,0,Page.WidthMm),Math.Clamp(v.Y,0,Page.HeightMm)); if(active.Tool is Tool.Pen or Tool.Highlighter) active.Points.Add(v); else { if(active.Points.Count==1) active.Points.Add(v); else active.Points[^1]=v; } }
        InvalidateVisual();
    }
    private void End() { if(active!=null) {Page.Marks.Add(active);active=null;} dragging=false;erasing=false; if(editing){ editing=false;Changed?.Invoke(); } InvalidateVisual(); }
    protected override void OnMouseDown(MouseButtonEventArgs e) {
        base.OnMouseDown(e); if(e.StylusDevice!=null)return;
        if(e.ChangedButton==MouseButton.Middle) {panning=true;last=e.GetPosition(this);CaptureMouse();}
        if(e.ChangedButton==MouseButton.Left) {Begin(e.GetPosition(this));CaptureMouse();} e.Handled=e.ChangedButton!=MouseButton.Right;
    }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e);if(e.StylusDevice!=null)return;var p=e.GetPosition(this);if(panning){offset+=p-last;last=p;InvalidateVisual();}else if(IsMouseCaptured)Move(p); }
    protected override void OnMouseUp(MouseButtonEventArgs e) { base.OnMouseUp(e);if(e.StylusDevice!=null)return;if(e.ChangedButton==MouseButton.Right)return;End();panning=false;ReleaseMouseCapture(); }
    protected override void OnLostMouseCapture(MouseEventArgs e) { End();panning=false;base.OnLostMouseCapture(e); }
    protected override void OnStylusDown(StylusDownEventArgs e) { base.OnStylusDown(e);if(e.StylusDevice.TabletDevice.Type==TabletDeviceType.Touch)return;Begin(e.GetPosition(this),e.Inverted);CaptureStylus();e.Handled=true; }
    protected override void OnStylusMove(StylusEventArgs e) { base.OnStylusMove(e);if(IsStylusCaptured){Move(e.GetPosition(this));e.Handled=true;} }
    protected override void OnStylusUp(StylusEventArgs e) { base.OnStylusUp(e);if(IsStylusCaptured){End();ReleaseStylusCapture();e.Handled=true;} }
    protected override void OnLostStylusCapture(StylusEventArgs e) {End();base.OnLostStylusCapture(e);}
    protected override void OnTouchDown(TouchEventArgs e) {base.OnTouchDown(e);if(editing){e.Handled=true;return;}touches[e.TouchDevice.Id]=e.GetTouchPoint(this).Position;e.TouchDevice.Capture(this);e.Handled=true;}
    protected override void OnTouchMove(TouchEventArgs e) {
        base.OnTouchMove(e);if(!touches.ContainsKey(e.TouchDevice.Id))return;
        var before=touches.Values.ToArray();var oldCenter=new Point(before.Average(p=>p.X),before.Average(p=>p.Y));
        touches[e.TouchDevice.Id]=e.GetTouchPoint(this).Position;var after=touches.Values.ToArray();var center=new Point(after.Average(p=>p.X),after.Average(p=>p.Y));
        double ratio=1; if(before.Length==2) {double d=(before[0]-before[1]).Length;if(d>5)ratio=(after[0]-after[1]).Length/d;}
        var anchor=World(oldCenter);scale=Math.Clamp(scale*ratio,.2,20);offset=new(center.X-anchor.X*scale,center.Y-anchor.Y*scale);InvalidateVisual();e.Handled=true;
    }
    protected override void OnTouchUp(TouchEventArgs e) {touches.Remove(e.TouchDevice.Id);e.TouchDevice.Capture(null);e.Handled=true;base.OnTouchUp(e);}
    protected override void OnLostTouchCapture(TouchEventArgs e) {touches.Remove(e.TouchDevice.Id);base.OnLostTouchCapture(e);}
    protected override void OnMouseWheel(MouseWheelEventArgs e) { if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){var p=e.GetPosition(this);var v=World(p);scale=Math.Clamp(scale*(e.Delta>0?1.1:1/1.1),.2,20);offset=new(p.X-v.X*scale,p.Y-v.Y*scale);}else offset.Y+=e.Delta*.5;InvalidateVisual();e.Handled=true; }
    public void Refresh() {Selected=null;InvalidateVisual();}
    public void DeleteSelection() { if(Selected is Guid id && Page.Marks.Any(m=>m.Id==id)) {History.Record(Document);Page.Marks.RemoveAll(m=>m.Id==id);Selected=null;Changed?.Invoke();InvalidateVisual();} }
}
