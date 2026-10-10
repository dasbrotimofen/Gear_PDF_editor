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
foreach(var bad in new[]{"{\"Version\":2}","{\"Pages\":[]}","{\"Pages\":[{\"WidthMm\":-1}]}","{\"Pages\":[{\"GridOpacity\":2}]}"}){
 bool rejected=false;try{DocumentFile.Decode(bad);}catch(InvalidDataException){rejected=true;}Check(rejected,"invalid file accepted");
}
var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".gear");try{DocumentFile.Save(path,d);Check(DocumentFile.Decode(File.ReadAllText(path)).Pages[0].Marks.Count==1,"disk round trip failed");DocumentFile.Save(path,new());Check(DocumentFile.Decode(File.ReadAllText(path)).Pages[0].Marks.Count==0,"overwrite failed");}finally{File.Delete(path);}
Console.WriteLine("PASS: geometry + grid round trip, undo/redo, invalid file rejection, save and overwrite.");
