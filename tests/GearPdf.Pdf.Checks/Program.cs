// SPDX-License-Identifier: AGPL-3.0-or-later
using GearPdf.Core;
using GearPdf.Pdf;
using PdfSharp.Pdf.IO;
static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
static ImageAsset Fixture(string name,int width,int height)=>new(){Png=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures",name)),PixelWidth=width,PixelHeight=height};
var doc=new Document{Assets=new(){["page"]=Fixture("background.png",600,849),["photo"]=Fixture("transparent.png",120,60)}};
doc.Pages[0].GridOpacity=0;doc.Pages[0].BackgroundImageId="page";
doc.Pages[0].Marks.Add(new(){Tool=Tool.Image,ImageAssetId="photo",RotationDegrees=30,Points=[new(50,85),new(130,125)]});
doc.Pages[0].Marks.Add(new(){Tool=Tool.Pen,Colour="#2463cf",WidthMm=1,Points=[new(25,55),new(45,65),new(65,55),new(85,65)]});
doc.Pages[0].Marks.Add(new(){Tool=Tool.Highlighter,Colour="#f7cd38",WidthMm=5,Opacity=.35,Points=[new(20,65),new(190,65)]});
doc.Pages[0].Marks.Add(new(){Tool=Tool.Rectangle,Colour="#2a9d60",WidthMm=.75,Points=[new(25,155),new(180,215)]});
doc.Pages[0].Marks.Add(new(){Tool=Tool.Ellipse,Colour="#e34444",WidthMm=1,Points=[new(60,170),new(130,200)]});
doc.Pages.Add(new(){WidthMm=297,HeightMm=210,GridOpacity=.25,Marks=[new(){Tool=Tool.Line,Colour="#2463cf",WidthMm=2,Points=[new(30,30),new(240,175)]},new(){Tool=Tool.Pen,Colour="#202020",WidthMm=3,Points=[new(50,160)]}]});
var reopened=DocumentFile.Decode(DocumentFile.Encode(doc));Check(reopened.Assets["photo"].Png.SequenceEqual(doc.Assets["photo"].Png),"embedded PNG changed");Check(reopened.Pages[0].BackgroundImageId=="page","background lost");Check(reopened.Pages[0].Marks[0].ImageAssetId=="photo","image reference lost");
Check(reopened.Pages[0].Marks[0].RotationDegrees==30,"image rotation lost");
var geometryImage=new Mark{Tool=Tool.Image,Points=[new(10,10),new(50,30)],RotationDegrees=90};var corner=ImageGeometry.Corner(geometryImage,true);Check(Math.Abs(corner.X-20)<1e-8 && Math.Abs(corner.Y-40)<1e-8,"rotated handle wrong");
foreach(double angle in new[]{0d,30d,90d,270d}){geometryImage.RotationDegrees=angle;var anchor=ImageGeometry.Corner(geometryImage,false);var pointer=ImageGeometry.Rotate(new(anchor.X+80,anchor.Y+40),anchor,angle);geometryImage.Points=ImageGeometry.ResizeFromAnchor(geometryImage,anchor,pointer,1000);var after=ImageGeometry.Corner(geometryImage,false);Check(Math.Abs(after.X-anchor.X)<1e-8 && Math.Abs(after.Y-anchor.Y)<1e-8,"rotated resize anchor moved");Check(Math.Abs((geometryImage.Points[1].X-geometryImage.Points[0].X)/(geometryImage.Points[1].Y-geometryImage.Points[0].Y)-2)<1e-8,"rotated resize aspect changed");}
var rotationHistory=new History();rotationHistory.Record(doc);doc.Pages[0].Marks[0].RotationDegrees=90;var rotationUndo=rotationHistory.Undo(doc);Check(rotationUndo.Pages[0].Marks[0].RotationDegrees==30,"rotation undo failed");Check(rotationHistory.Redo(rotationUndo).Pages[0].Marks[0].RotationDegrees==90,"rotation redo failed");
var history=new History();var empty=new Document();history.Record(empty);empty.Assets.Add("photo",doc.Assets["photo"]);empty.Pages[0].Marks.Add(new(){Tool=Tool.Image,ImageAssetId="photo",Points=[new(10,10),new(30,20)]});var undone=history.Undo(empty);Check(undone.Assets.Count==0,"image insert undo did not restore assets");var redone=history.Redo(undone);Check(redone.Pages[0].Marks.Count==1 && redone.Assets.ContainsKey("photo"),"image redo lost data");
var bad=DocumentFile.Decode(DocumentFile.Encode(doc));bad.Assets.Remove("photo");bool rejected=false;try{DocumentFile.Validate(bad);}catch(InvalidDataException){rejected=true;}Check(rejected,"missing asset accepted");
var v1=DocumentFile.Decode("{\"Version\":1,\"Pages\":[{}]}");Check(v1.Version==2,"v1 migration failed");
string output=args.Length>0?Path.GetFullPath(args[0]):Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".pdf");PdfExporter.Export(output,reopened,true);using(var pdf=PdfReader.Open(output,PdfDocumentOpenMode.Import)){Check(pdf.PageCount==2,"export page count wrong");Check(Math.Abs(pdf.Pages[1].Width.Millimeter-297)<.01,"landscape page size wrong");}
var imported=PdfImporter.Import(output);Check(imported.Pages.Count==2,"PDF render import lost a page");Check(imported.Pages.All(p=>p.BackgroundImageId!=null && p.GridOpacity==0),"import background/grid incorrect");Check(Math.Abs(imported.Pages[0].WidthMm-210)<.2 && Math.Abs(imported.Pages[1].HeightMm-210)<.2,"import dimensions incorrect");
var importRoundTrip=DocumentFile.Decode(DocumentFile.Encode(imported));Check(importRoundTrip.Assets.Count==2,"import/save/reopen lost page assets");
if(args.Length>0){string png=Path.ChangeExtension(output,"imported.png");File.WriteAllBytes(png,imported.Assets[imported.Pages[0].BackgroundImageId!].Png);}
string rotatedPath=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".pdf");
try{using(var rotated=PdfReader.Open(output,PdfDocumentOpenMode.Modify)){rotated.Pages[0].Rotate=90;rotated.Save(rotatedPath);}var rotatedImport=PdfImporter.Import(rotatedPath);Check(Math.Abs(rotatedImport.Pages[0].WidthMm-297)<.2 && Math.Abs(rotatedImport.Pages[0].HeightMm-210)<.2,"rotated PDF dimensions wrong");}finally{File.Delete(rotatedPath);}
Console.WriteLine("PASS: image/background persistence, asset undo/redo, missing assets, v1 migration, PDF export and PDFium render import.");
Console.WriteLine(output);
if(args.Length==0)File.Delete(output);
