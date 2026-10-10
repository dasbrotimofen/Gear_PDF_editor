// SPDX-License-Identifier: AGPL-3.0-or-later
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using GearPdf.Core;
using Microsoft.Win32;
namespace GearPdf.App;
public partial class MainWindow : Window {
    private string? path;
    private bool dirty, syncing;
    private Mark? clipboard;
    private static readonly string[] GearTools=["Pen 1","Pen 2","Pen 3","Highlight","Eraser","Line","Rectangle","Ellipse","Select"];
    private static double ToolAngleStep => Math.PI*2/GearTools.Length;
    private bool favouritesHorizontal, favouriteDragging;
    private Point favouritePosition=new(24,24), favouriteDragStart, favouriteDragOrigin;
    private string activeSlot="Pen 1";
    private string lastPenSlot="Pen 1", beforeEraseSlot="Pen 1";
    private ToolPreset beforeErasePreset=new(Tool.Pen,"#e34444",.5,1);
    private bool gearExpanded, gearDragging, gearMoved, gearPositioned;
    private Point gearCentre, gearDragStart, gearDragOrigin;
    private const double GearCanvasCentre = 330;
    private bool gearPropertiesOpen;
    private string propertyCategory="Colour";
    private string? colourFamily;
    private double PropertyAngle => Array.IndexOf(GearTools,activeSlot) is int i && i>=0 ? i*ToolAngleStep-Math.PI/2 : -Math.PI/2;
    private readonly Dictionary<string,ToolPreset> slots=new() {
        ["Pen 1"]=new(Tool.Pen,"#e34444",.5,1),["Pen 2"]=new(Tool.Pen,"#2463cf",.5,1),["Pen 3"]=new(Tool.Pen,"#2a9d60",.75,1),
        ["Highlight"]=new(Tool.Highlighter,"#f7cd38",5,.35),["Line"]=new(Tool.Line,"#202020",.5,1),
        ["Rectangle"]=new(Tool.Rectangle,"#202020",.5,1),["Ellipse"]=new(Tool.Ellipse,"#202020",.5,1),
        ["Eraser"]=new(Tool.Eraser,"#202020",1,1),["Select"]=new(Tool.Select,"#202020",1,1)
    };
    private readonly List<ToolPreset> favourites=[new(Tool.Pen,"#e34444",1,1),new(Tool.Pen,"#2463cf",.5,1),new(Tool.Pen,"#2a9d60",.75,1),new(Tool.Highlighter,"#f7cd38",5,.35)];
    private readonly Dictionary<string,string[]> families=new() {
        ["#202020"]=["#000000","#202020","#555555","#999999","#dddddd","#ffffff"],
        ["#e34444"]=["#780d24","#b51d35","#e34444","#ef6544","#f28c28","#f6a9a9"],
        ["#2a9d60"]=["#14532d","#2a9d60","#84cc16","#6b7c32","#008080","#b5e3c4"],
        ["#2463cf"]=["#142451","#1947a3","#2463cf","#009dcc","#80c5ef","#29b5ac"],
        ["#f7cd38"]=["#f7cd38","#e4a817","#f28c28","#fff1a1","#c8a52d","#fffbd6"]
    };
    public MainWindow() {
        InitializeComponent(); Surface.Changed+=Changed; BuildGear(); BuildFavourites();
        var menu=new ContextMenu(); foreach(var label in new[]{"Cut","Copy","Paste","Delete","Properties"}) {var item=new MenuItem{Header=label};item.Click+=(_,_)=>ContextAction(label);menu.Items.Add(item);} Surface.ContextMenu=menu;
        Surface.Preset=slots[activeSlot]; Loaded+=(_,_)=>{Surface.Fit();Sync();};
    }
    private void Changed(){dirty=true;Sync();}
    private void Sync(){syncing=true;GridOpacity.Value=Surface.Page.GridOpacity*100;PageLabel.Text=$"{Surface.PageIndex+1}/{Surface.Document.Pages.Count}";Status.Text=$"{activeSlot} · {Surface.Preset.WidthMm:0.##} mm · {Surface.Preset.Opacity:P0}    |    {Surface.Page.WidthMm} × {Surface.Page.HeightMm} mm    |    {Surface.Page.Marks.Count} objects";Title=$"{(dirty?"* ":"")}GEAR PDF Editor — {(path is null?"Untitled":System.IO.Path.GetFileName(path))}";syncing=false;Surface.InvalidateVisual();}
    private Button MakeButton(string text,Action action){var b=new Button{Content=text};b.Click+=(_,_)=>action();return b;}
    private static Point Polar(double radius, double angle) => new(GearCanvasCentre + radius*Math.Cos(angle), GearCanvasCentre + radius*Math.Sin(angle));
    private static Geometry GearOutline(double outer, double root) {
        var geometry=new StreamGeometry();
        using(var context=geometry.Open()) {
            for(int i=0;i<96;i++) {
                double angle=i*Math.PI/48-Math.PI/2;
                var point=Polar(i%8 is 1 or 2 or 3 or 4?outer:root,angle);
                if(i==0)context.BeginFigure(point,true,true);else context.LineTo(point,true,false);
            }
        }
        geometry.Freeze();return geometry;
    }
    private static Geometry ToolSector(int index) {
        double start=index*ToolAngleStep-Math.PI/2-ToolAngleStep/2+.012;
        double end=start+ToolAngleStep-.024;
        var geometry=new StreamGeometry();using(var context=geometry.Open()) {
            context.BeginFigure(Polar(48,start),true,true);
            context.LineTo(Polar(160,start),true,false);
            context.ArcTo(Polar(160,end),new Size(160,160),0,false,SweepDirection.Clockwise,true,false);
            context.LineTo(Polar(48,end),true,false);
            context.ArcTo(Polar(48,start),new Size(48,48),0,false,SweepDirection.Counterclockwise,true,false);
        }
        geometry.Freeze();return geometry;
    }
    private void BuildGear(){
        Gear.Children.Clear();
        if(gearExpanded) {
            var outline=GearOutline(160,147);
            Gear.Children.Add(new System.Windows.Shapes.Path {Data=outline,Fill=new SolidColorBrush(Color.FromRgb(29,42,59)),Stroke=new SolidColorBrush(Color.FromRgb(238,166,63)),StrokeThickness=2,IsHitTestVisible=false});
            var ring=new Canvas{Width=660,Height=660,Clip=outline};
            var names=GearTools;
            for(int i=0;i<names.Length;i++) {
                string name=names[i];var sector=new System.Windows.Shapes.Path{Data=ToolSector(i),Fill=new SolidColorBrush(name==activeSlot?Color.FromRgb(87,68,39):Color.FromRgb(37,52,72)),Stroke=new SolidColorBrush(Color.FromRgb(83,103,127)),StrokeThickness=1};
                var button=new Button{Width=660,Height=660,Margin=new Thickness(0),Padding=new Thickness(0),Background=Brushes.Transparent,ToolTip=name,Cursor=Cursors.Hand};
                var template=new ControlTemplate(typeof(Button));
                var presenter=new FrameworkElementFactory(typeof(ContentPresenter));template.VisualTree=presenter;button.Template=template;
                var visual=new Canvas{Width=660,Height=660};visual.Children.Add(sector);
                var labelPosition=Polar(99,i*ToolAngleStep-Math.PI/2);
                var label=new TextBlock{Text=name,Foreground=Brushes.White,FontSize=12,FontWeight=name==activeSlot?FontWeights.Bold:FontWeights.Normal,Width=76,TextAlignment=TextAlignment.Center,IsHitTestVisible=false};
                Canvas.SetLeft(label,labelPosition.X-38);Canvas.SetTop(label,labelPosition.Y-(slots[name].Tool is Tool.Pen or Tool.Highlighter?16:9));visual.Children.Add(label);
                if(slots[name].Tool is Tool.Pen or Tool.Highlighter) {
                    var preset=slots[name];
                    // White-backed swatches keep black and pale colours visible on the dark gear.
                    var preview=new Border{Width=44,Height=12,CornerRadius=new CornerRadius(6),Background=Brushes.White,BorderBrush=new SolidColorBrush(Color.FromRgb(175,188,204)),BorderThickness=new Thickness(1),Padding=new Thickness(2),IsHitTestVisible=false};
                    preview.Child=new Border{Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(preset.Colour)),CornerRadius=new CornerRadius(4)};
                    Canvas.SetLeft(preview,labelPosition.X-22);Canvas.SetTop(preview,labelPosition.Y+5);visual.Children.Add(preview);
                    button.ToolTip=$"{name} · {preset.Colour} · {preset.WidthMm:0.##} mm · {preset.Opacity:P0}";
                }
                button.Content=visual;
                // A canvas with no background only hits its sector; the transparent button must be clipped too.
                button.Clip=ToolSector(i);button.Click+=(_,_)=>{Activate(name);BuildGear();};
                ring.Children.Add(button);
            }
            Gear.Children.Add(ring);
        }
        if(gearExpanded && gearPropertiesOpen)BuildPropertyLayers();
        var hub=new Grid{Width=86,Height=86,Cursor=Cursors.Hand,ToolTip="Click: expand/collapse · Drag: move gear",Background=Brushes.Transparent};
        var hubShape=new Polygon{Fill=new SolidColorBrush(Color.FromRgb(238,166,63)),Stroke=Brushes.Black,StrokeThickness=1};
        var points=new PointCollection();for(int i=0;i<64;i++){double angle=i*Math.PI/32-Math.PI/2;double radius=i%8 is 1 or 2 or 3 or 4?42:34;points.Add(new(43+radius*Math.Cos(angle),43+radius*Math.Sin(angle)));}hubShape.Points=points;
        hub.Children.Add(hubShape);hub.Children.Add(new TextBlock{Text=gearExpanded?"CLOSE":"GEAR",Foreground=Brushes.Black,FontWeight=FontWeights.Bold,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false});
        hub.MouseLeftButtonDown+=(_,e)=>{StartGearDrag(e.GetPosition(GearHost));hub.CaptureMouse();e.Handled=true;};
        hub.MouseMove+=(_,e)=>{if(gearDragging && hub.IsMouseCaptured){MoveGearDrag(e.GetPosition(GearHost));e.Handled=true;}};
        hub.MouseLeftButtonUp+=(_,e)=>{if(!hub.IsMouseCaptured)return;bool click=FinishGearDrag();hub.ReleaseMouseCapture();if(click)ToggleGear();e.Handled=true;};
        hub.LostMouseCapture+=(_,_)=>gearDragging=false;
        hub.TouchDown+=(_,e)=>{if(gearDragging){e.Handled=true;return;}StartGearDrag(e.GetTouchPoint(GearHost).Position);e.TouchDevice.Capture(hub);e.Handled=true;};
        hub.TouchMove+=(_,e)=>{if(e.TouchDevice.Captured==hub)MoveGearDrag(e.GetTouchPoint(GearHost).Position);e.Handled=true;};
        hub.TouchUp+=(_,e)=>{if(e.TouchDevice.Captured==hub){bool click=FinishGearDrag();e.TouchDevice.Capture(null);if(click)ToggleGear();}e.Handled=true;};
        hub.LostTouchCapture+=(_,_)=>gearDragging=false;
        Canvas.SetLeft(hub,GearCanvasCentre-43);Canvas.SetTop(hub,GearCanvasCentre-43);Gear.Children.Add(hub);
        PositionGear();
    }
    private void StartGearDrag(Point pointer){gearDragging=true;gearMoved=false;gearDragStart=pointer;gearDragOrigin=gearCentre;}
    private void MoveGearDrag(Point pointer){var delta=pointer-gearDragStart;if(delta.Length>5)gearMoved=true;if(gearMoved){gearCentre=gearDragOrigin+delta;PositionGear();}}
    private bool FinishGearDrag(){bool click=gearDragging&&!gearMoved;gearDragging=false;return click;}
    private void ToggleGear(){gearExpanded=!gearExpanded;if(!gearExpanded)gearPropertiesOpen=false;BuildGear();}
    private void PositionGear(){
        if(GearHost is null || GearHost.ActualWidth<=0 || GearHost.ActualHeight<=0)return;
        double radius=gearExpanded?(gearPropertiesOpen?296:164):47;
        if(!gearPositioned){gearCentre=new(GearHost.ActualWidth-72,GearHost.ActualHeight-72);gearPositioned=true;}
        gearCentre=new(ClampGearAxis(gearCentre.X,GearHost.ActualWidth,radius),ClampGearAxis(gearCentre.Y,GearHost.ActualHeight,radius));
        Canvas.SetLeft(Gear,gearCentre.X-GearCanvasCentre);Canvas.SetTop(Gear,gearCentre.Y-GearCanvasCentre);
    }
    private static double ClampGearAxis(double value,double extent,double radius) => extent<radius*2?extent/2:Math.Clamp(value,radius,extent-radius);
    private void GearHostSizeChanged(object sender,SizeChangedEventArgs e){PositionGear();PositionFavouriteBar();}
    private void Activate(string name){if(activeSlot==name){ShowProperties();return;}if(name=="Eraser" && Surface.Preset.Tool!=Tool.Eraser){beforeEraseSlot=activeSlot;beforeErasePreset=Surface.Preset;}if(name is "Pen 1" or "Pen 2" or "Pen 3")lastPenSlot=name;activeSlot=name;Surface.Preset=slots[name];gearPropertiesOpen=false;Sync();}
    private void ShowProperties(){if(Surface.Preset.Tool is Tool.Eraser or Tool.Select)return;gearExpanded=true;gearPropertiesOpen=true;propertyCategory="Colour";colourFamily=null;BuildGear();}
    private static Geometry PropertySector(double inner,double outer,double start,double end){
        var geometry=new StreamGeometry();using(var c=geometry.Open()){
            c.BeginFigure(Polar(inner,start),true,true);c.LineTo(Polar(outer,start),true,false);
            c.ArcTo(Polar(outer,end),new Size(outer,outer),0,false,SweepDirection.Clockwise,true,false);
            c.LineTo(Polar(inner,end),true,false);c.ArcTo(Polar(inner,start),new Size(inner,inner),0,false,SweepDirection.Counterclockwise,true,false);
        }geometry.Freeze();return geometry;
    }
    private void PropertyChoice(string label,double inner,double outer,double start,double end,Action action,string? colour=null,bool selected=false){
        var shape=PropertySector(inner,outer,start+.006,end-.006);
        var visual=new Canvas{Width=660,Height=660};
        visual.Children.Add(new System.Windows.Shapes.Path{Data=shape,Fill=new SolidColorBrush(selected?Color.FromRgb(87,68,39):Color.FromRgb(37,52,72)),Stroke=new SolidColorBrush(Color.FromRgb(133,151,174)),StrokeThickness=1});
        var pos=Polar((inner+outer)/2,(start+end)/2);
        if(colour!=null){var swatch=new Border{Width=30,Height=24,Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(colour)),BorderBrush=Brushes.White,BorderThickness=new Thickness(2),CornerRadius=new CornerRadius(4),IsHitTestVisible=false};Canvas.SetLeft(swatch,pos.X-15);Canvas.SetTop(swatch,pos.Y-12);visual.Children.Add(swatch);}
        else {var text=new TextBlock{Text=label,Foreground=Brushes.White,FontSize=12,Width=72,TextAlignment=TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false};Canvas.SetLeft(text,pos.X-36);Canvas.SetTop(text,pos.Y-8);visual.Children.Add(text);}
        var button=new Button{Width=660,Height=660,Margin=new Thickness(0),Padding=new Thickness(0),Content=visual,Clip=shape,ToolTip=label,Background=Brushes.Transparent,Cursor=Cursors.Hand};
        var template=new ControlTemplate(typeof(Button)){VisualTree=new FrameworkElementFactory(typeof(ContentPresenter))};button.Template=template;button.Click+=(_,_)=>action();Gear.Children.Add(button);
    }
    private void BuildPropertyLayers(){
        var categories=new[]{"Colour","Width","Opacity"};double span=Math.PI*2/3;
        for(int i=0;i<categories.Length;i++){string category=categories[i];double start=PropertyAngle-span/2+i*span/3;PropertyChoice(category,166,218,start,start+span/3,()=>{propertyCategory=category;colourFamily=null;BuildGear();},selected:propertyCategory==category);}
        var choices=new List<(string Label,string? Colour,Action Apply,bool Selected)>();
        if(propertyCategory=="Colour"){
            var values=colourFamily is null?families.Keys.ToArray():families[colourFamily];
            foreach(var value in values){string colour=value;choices.Add((colour,colour,()=>{if(colourFamily is null && Surface.Preset.Colour==colour){colourFamily=colour;BuildGear();}else SetPreset(Surface.Preset with{Colour=colour});},Surface.Preset.Colour==colour));}
        }else if(propertyCategory=="Width"){
            foreach(double value in new[]{.25,.5,.75,1,1.5,2,3,5}){double width=value;choices.Add(($"{width:0.##} mm",null,()=>SetPreset(Surface.Preset with{WidthMm=width}),Surface.Preset.WidthMm==width));}
        }else{
            foreach(double value in new[]{.25,.5,.75,1}){double opacity=value;choices.Add(($"{opacity:P0}",null,()=>SetPreset(Surface.Preset with{Opacity=opacity}),Surface.Preset.Opacity==opacity));}
        }
        double choiceSpan=Math.PI*8/9;for(int i=0;i<choices.Count;i++){var choice=choices[i];double start=PropertyAngle-choiceSpan/2+i*choiceSpan/choices.Count;PropertyChoice(choice.Label,224,290,start,start+choiceSpan/choices.Count,choice.Apply,choice.Colour,choice.Selected);}
    }
    private void SetPreset(ToolPreset p){Surface.Preset=p;if(slots.ContainsKey(activeSlot))slots[activeSlot]=p;Sync();BuildGear();}
    private void BuildFavourites(){
        Favourites.Children.Clear();Favourites.Orientation=favouritesHorizontal?Orientation.Horizontal:Orientation.Vertical;
        foreach(var preset in favourites.ToArray()){
            var b=MakeButton($"{preset.Tool} · {preset.WidthMm:0.##} mm",()=>{
                if(activeSlot=="Favourite" && Surface.Preset==preset){ShowProperties();return;}
                activeSlot="Favourite";Surface.Preset=preset;gearPropertiesOpen=false;BuildGear();Sync();
            });
            b.Width=128;b.Height=40;b.ToolTip=$"{preset.Colour} · {preset.WidthMm:0.##} mm · {preset.Opacity:P0}";
            b.BorderBrush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(preset.Colour));b.BorderThickness=new(6,0,0,0);
            var menu=new ContextMenu();var remove=new MenuItem{Header="Remove favourite"};remove.Click+=(_,_)=>{favourites.Remove(preset);BuildFavourites();};menu.Items.Add(remove);b.ContextMenu=menu;Favourites.Children.Add(b);
        }
        var add=MakeButton("＋ Save preset",()=>FavouriteClick(this,new RoutedEventArgs()));add.Width=128;add.Height=40;Favourites.Children.Add(add);
        if(FavouriteOrientation!=null){FavouriteOrientation.Content=favouritesHorizontal?"↕":"↔";FavouriteOrientation.ToolTip=favouritesHorizontal?"Switch to vertical bar":"Switch to horizontal bar";}
        Dispatcher.BeginInvoke(new Action(PositionFavouriteBar));
    }
    private void FavouriteOrientationClick(object sender,RoutedEventArgs e){favouritesHorizontal=!favouritesHorizontal;BuildFavourites();}
    private void PositionFavouriteBar(){
        if(FavouriteBar is null || GearHost is null || GearHost.ActualWidth<=0)return;
        favouritePosition=new(Math.Clamp(favouritePosition.X,0,Math.Max(0,GearHost.ActualWidth-FavouriteBar.ActualWidth)),Math.Clamp(favouritePosition.Y,0,Math.Max(0,GearHost.ActualHeight-FavouriteBar.ActualHeight)));
        Canvas.SetLeft(FavouriteBar,favouritePosition.X);Canvas.SetTop(FavouriteBar,favouritePosition.Y);
    }
    private void FavouriteBarSizeChanged(object sender,SizeChangedEventArgs e)=>PositionFavouriteBar();
    private void StartFavouriteDrag(Point point){favouriteDragging=true;favouriteDragStart=point;favouriteDragOrigin=favouritePosition;}
    private void MoveFavouriteDrag(Point point){if(!favouriteDragging)return;favouritePosition=favouriteDragOrigin+(point-favouriteDragStart);PositionFavouriteBar();}
    private void FavouriteHandleMouseDown(object sender,MouseButtonEventArgs e){StartFavouriteDrag(e.GetPosition(GearHost));((UIElement)sender).CaptureMouse();e.Handled=true;}
    private void FavouriteHandleMouseMove(object sender,MouseEventArgs e){if(((UIElement)sender).IsMouseCaptured)MoveFavouriteDrag(e.GetPosition(GearHost));}
    private void FavouriteHandleMouseUp(object sender,MouseButtonEventArgs e){favouriteDragging=false;((UIElement)sender).ReleaseMouseCapture();e.Handled=true;}
    private void FavouriteHandleLostMouseCapture(object sender,MouseEventArgs e)=>favouriteDragging=false;
    private void FavouriteHandleTouchDown(object sender,TouchEventArgs e){if(favouriteDragging){e.Handled=true;return;}StartFavouriteDrag(e.GetTouchPoint(GearHost).Position);e.TouchDevice.Capture((IInputElement)sender);e.Handled=true;}
    private void FavouriteHandleTouchMove(object sender,TouchEventArgs e){if(e.TouchDevice.Captured==(IInputElement)sender)MoveFavouriteDrag(e.GetTouchPoint(GearHost).Position);e.Handled=true;}
    private void FavouriteHandleTouchUp(object sender,TouchEventArgs e){if(e.TouchDevice.Captured==(IInputElement)sender){favouriteDragging=false;e.TouchDevice.Capture(null);}e.Handled=true;}
    private void FavouriteHandleLostTouchCapture(object sender,TouchEventArgs e)=>favouriteDragging=false;
    private void FavouriteClick(object sender,RoutedEventArgs e){if(Surface.Preset.Tool is Tool.Select or Tool.Eraser)return;favourites.Add(Surface.Preset with{});BuildFavourites();}
    private bool ConfirmDiscard(){if(!dirty)return true;var answer=MessageBox.Show(this,"Save your sketch before continuing?","Unsaved sketch",MessageBoxButton.YesNoCancel);return answer==MessageBoxResult.No || (answer==MessageBoxResult.Yes && Save(false));}
    private void NewClick(object sender,RoutedEventArgs e){if(!ConfirmDiscard())return;Surface.Document=new();Surface.PageIndex=0;Surface.History.Clear();Surface.Refresh();path=null;dirty=false;Surface.Fit();Sync();}
    private void OpenClick(object sender,RoutedEventArgs e){if(!ConfirmDiscard())return;var dialog=new OpenFileDialog{Filter="GEAR sketch (*.gear)|*.gear"};if(dialog.ShowDialog(this)!=true)return;try{if(new FileInfo(dialog.FileName).Length>50*1024*1024)throw new InvalidDataException("Sketch exceeds the 50 MB starter limit.");var doc=DocumentFile.Decode(File.ReadAllText(dialog.FileName));Surface.Document=doc;Surface.PageIndex=0;Surface.History.Clear();Surface.Refresh();path=dialog.FileName;dirty=false;Surface.Fit();Sync();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Unable to open sketch");}}
    private bool Save(bool saveAs){var target=path;if(saveAs || target is null){var dialog=new SaveFileDialog{Filter="GEAR sketch (*.gear)|*.gear",DefaultExt=".gear",FileName=path is null?"Sketch.gear":System.IO.Path.GetFileName(path)};if(dialog.ShowDialog(this)!=true)return false;target=dialog.FileName;}try{DocumentFile.Save(target!,Surface.Document);path=target;dirty=false;Sync();return true;}catch(Exception ex){MessageBox.Show(this,ex.Message,"Unable to save sketch");return false;}}
    private void SaveClick(object sender,RoutedEventArgs e)=>Save(false);
    private void SaveAsClick(object sender,RoutedEventArgs e)=>Save(true);
    private void UndoClick(object sender,RoutedEventArgs e){Surface.Document=Surface.History.Undo(Surface.Document);Surface.PageIndex=Math.Min(Surface.PageIndex,Surface.Document.Pages.Count-1);Surface.Refresh();Changed();}
    private void RedoClick(object sender,RoutedEventArgs e){Surface.Document=Surface.History.Redo(Surface.Document);Surface.PageIndex=Math.Min(Surface.PageIndex,Surface.Document.Pages.Count-1);Surface.Refresh();Changed();}
    private void FitClick(object sender,RoutedEventArgs e)=>Surface.Fit();
    private void PaperChanged(object sender,SelectionChangedEventArgs e){if(Surface is null || syncing)return;Surface.History.Record(Surface.Document);var dims=PaperSize.SelectedIndex switch{1=>(297d,210d),2=>(297d,420d),3=>(420d,297d),4=>(215.9,279.4),_=>(210d,297d)};Surface.Page.WidthMm=dims.Item1;Surface.Page.HeightMm=dims.Item2;Surface.Fit();Changed();}
    private void GridChanged(object sender,RoutedPropertyChangedEventArgs<double> e){if(Surface is null || syncing)return;Surface.Page.GridOpacity=e.NewValue/100;Changed();}
    private void AddPageClick(object sender,RoutedEventArgs e){Surface.History.Record(Surface.Document);var p=Surface.Page;Surface.Document.Pages.Add(new(){WidthMm=p.WidthMm,HeightMm=p.HeightMm,GridOpacity=p.GridOpacity});Surface.PageIndex=Surface.Document.Pages.Count-1;Surface.Refresh();Changed();}
    private void PreviousClick(object sender,RoutedEventArgs e){Surface.PageIndex=Math.Max(0,Surface.PageIndex-1);Surface.Refresh();Surface.Fit();Sync();}
    private void NextClick(object sender,RoutedEventArgs e){Surface.PageIndex=Math.Min(Surface.Document.Pages.Count-1,Surface.PageIndex+1);Surface.Refresh();Surface.Fit();Sync();}
    private static Mark Clone(Mark m)=>new(){Tool=m.Tool,Colour=m.Colour,WidthMm=m.WidthMm,Opacity=m.Opacity,Points=m.Points.ToList()};
    private void ContextAction(string label){var mark=Surface.Page.Marks.FirstOrDefault(m=>m.Id==Surface.Selected);if(label is "Copy" or "Cut"){if(mark==null)return;clipboard=Clone(mark);if(label=="Cut")Surface.DeleteSelection();}else if(label=="Paste" && clipboard!=null){Surface.History.Record(Surface.Document);var copy=Clone(clipboard);copy.Points=copy.Points.Select(p=>new Vec(p.X+5,p.Y+5)).ToList();Surface.Page.Marks.Add(copy);Surface.Selected=copy.Id;Changed();}else if(label=="Delete")Surface.DeleteSelection();else if(label=="Properties" && mark!=null){var dialog=new Window{Title="Annotation properties",Owner=this,Width=310,Height=230,WindowStartupLocation=WindowStartupLocation.CenterOwner};var panel=new StackPanel{Margin=new(15)};var colour=new TextBox{Text=mark.Colour,Margin=new(0,5,0,5)};var width=new TextBox{Text=mark.WidthMm.ToString(System.Globalization.CultureInfo.InvariantCulture),Margin=new(0,5,0,5)};var opacity=new Slider{Minimum=0,Maximum=1,Value=mark.Opacity};panel.Children.Add(new Label{Content="Colour (#RRGGBB), width (mm), opacity"});panel.Children.Add(colour);panel.Children.Add(width);panel.Children.Add(opacity);panel.Children.Add(MakeButton("Apply",()=>{if(!System.Text.RegularExpressions.Regex.IsMatch(colour.Text,"^#[0-9a-fA-F]{6}$") || !double.TryParse(width.Text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double w) || !double.IsFinite(w) || w is <=0 or >50){MessageBox.Show(dialog,"Enter #RRGGBB and a width between 0 and 50 mm.");return;}Surface.History.Record(Surface.Document);mark.Colour=colour.Text;mark.WidthMm=w;mark.Opacity=opacity.Value;Changed();dialog.Close();}));dialog.Content=panel;dialog.ShowDialog();}Surface.InvalidateVisual();}
    private void OnKeyDown(object sender,KeyEventArgs e){
        if(e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase or PasswordBox)return;
        if(Keyboard.Modifiers==ModifierKeys.None && e.Key is Key.E or Key.P){
            if(!e.IsRepeat){
                if(e.Key==Key.E){
                    if(Surface.Preset.Tool==Tool.Eraser){activeSlot=beforeEraseSlot;Surface.Preset=beforeErasePreset;gearPropertiesOpen=false;Sync();}
                    else Activate("Eraser");
                }else if(activeSlot!=lastPenSlot)Activate(lastPenSlot);
                BuildGear();
            }
            e.Handled=true;return;
        }
        if(e.Key==Key.Escape){if(gearExpanded){gearExpanded=false;gearPropertiesOpen=false;BuildGear();}e.Handled=true;return;}if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control)){switch(e.Key){case Key.S:Save(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));break;case Key.Z:UndoClick(this,e);break;case Key.Y:RedoClick(this,e);break;case Key.C:ContextAction("Copy");break;case Key.X:ContextAction("Cut");break;case Key.V:ContextAction("Paste");break;default:return;}e.Handled=true;}else if(e.Key==Key.Delete){Surface.DeleteSelection();e.Handled=true;}}
    private void OnClosing(object? sender,CancelEventArgs e){if(!ConfirmDiscard())e.Cancel=true;}
}
