// SPDX-License-Identifier: AGPL-3.0-or-later
using GearPdf.Core;
static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
var d=new Document();var h=new History();h.Record(d);
d.Pages[0].Marks.Add(new(){Tool=Tool.Pen,Points=[new(12,20),new(30,45)],WidthMm=.75,Opacity=.5});
var reopened=DocumentFile.Decode(DocumentFile.Encode(d));
Check(reopened.Pages[0].GridSpacingMm==5,"grid metadata lost");
Check(reopened.Pages[0].Marks[0].Points[1]==new Vec(30,45),"ink geometry lost");
var undone=h.Undo(d);Check(undone.Pages[0].Marks.Count==0,"undo failed");
var redone=h.Redo(undone);Check(redone.Pages[0].Marks.Count==1,"redo failed");
foreach(var bad in new[]{"{\"Version\":99}","{\"Pages\":[]}","{\"Pages\":[{\"WidthMm\":-1}]}","{\"Pages\":[{\"GridOpacity\":2}]}"}){
 bool rejected=false;try{DocumentFile.Decode(bad);}catch(InvalidDataException){rejected=true;}Check(rejected,"invalid file accepted");
}
var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".gear");try{DocumentFile.Save(path,d);Check(DocumentFile.Decode(File.ReadAllText(path)).Pages[0].Marks.Count==1,"disk round trip failed");DocumentFile.Save(path,new());Check(DocumentFile.Decode(File.ReadAllText(path)).Pages[0].Marks.Count==0,"overwrite failed");}finally{File.Delete(path);}
Console.WriteLine("PASS: geometry + grid round trip, undo/redo, invalid file rejection, save and overwrite.");
var layers=new Document();var back=new Mark{Tool=Tool.Rectangle,Locked=true,StrokeStyle=StrokeStyle.DotDashed,Points=[new(10,10),new(20,20)]};var front=new Mark{Tool=Tool.Pen,Visible=false,Points=[new(12,12)]};layers.Pages[0].Marks.AddRange([back,front]);
Check(!PageEditing.CanEdit(back) && !PageEditing.CanEdit(front),"locked or hidden layer editable");
var layersHistory=new History();layersHistory.Record(layers);PageEditing.MoveLayer(layers.Pages[0],back.Id,1);Check(layers.Pages[0].Marks[^1].Id==back.Id,"bring to front failed");
var layerUndo=layersHistory.Undo(layers);Check(layerUndo.Pages[0].Marks[0].Id==back.Id,"layer ordering undo failed");
var savedLayers=DocumentFile.Decode(DocumentFile.Encode(layers));Check(savedLayers.Pages[0].Marks[^1].Locked && savedLayers.Pages[0].Marks[^1].StrokeStyle==StrokeStyle.DotDashed && !savedLayers.Pages[0].Marks[0].Visible,"layer settings lost");
layersHistory.Record(layers);layers.Pages.Add(new(){WidthMm=297,HeightMm=210});layers.Pages.Add(new(){WidthMm=420,HeightMm=297});int pageIndex=PageEditing.MovePage(layers,2,-1);Check(pageIndex==1 && layers.Pages[1].WidthMm==420,"page reorder failed");
pageIndex=PageEditing.DeletePage(layers,2);Check(pageIndex==1 && layers.Pages.Count==2,"delete final page selection failed");Check(layersHistory.Undo(layers).Pages.Count==1,"page undo failed");PageEditing.DeletePage(layers,1);PageEditing.DeletePage(layers,0);Check(layers.Pages.Count==1,"last page deleted");
foreach(var old in new[]{1,2}){var legacy=DocumentFile.Decode($"{{\"Version\":{old},\"Pages\":[{{\"Marks\":[{{\"Tool\":0,\"Points\":[{{\"X\":1,\"Y\":1}}]}}]}}]}}");Check(legacy.Version==3 && legacy.Pages[0].Marks[0].Visible && legacy.Pages[0].Marks[0].StrokeStyle==StrokeStyle.Solid,"legacy defaults failed");}
bool badStyle=false;try{DocumentFile.Decode("{\"Pages\":[{\"Marks\":[{\"StrokeStyle\":99,\"Points\":[{\"X\":1,\"Y\":1}]}]}]}");}catch(InvalidDataException){badStyle=true;}Check(badStyle,"invalid stroke style accepted");
Console.WriteLine("PASS: layer locks/visibility/style persistence, ordering undo, page deletion/reordering/undo, last-page guard, legacy defaults.");
