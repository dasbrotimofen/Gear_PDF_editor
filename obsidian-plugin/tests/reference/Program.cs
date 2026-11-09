// SPDX-License-Identifier: AGPL-3.0-or-later
using GearPdf.Core;
using GearPdf.Pdf;

// Produce the source and reference pixels using the real application serializers,
// exporter and PDFium importer; no parallel implementation of GEAR's PDF renderer.
string root=Path.GetFullPath(args[0]), output=Path.GetFullPath(args[1]);
Directory.CreateDirectory(output);
ImageAsset Fixture(string name,int w,int h)=>new(){Png=File.ReadAllBytes(Path.Combine(root,"tests/GearPdf.Pdf.Checks/Fixtures",name)),PixelWidth=w,PixelHeight=h};
var doc=new Document{Assets=new(){["background"]=Fixture("background.png",600,849),["image"]=Fixture("transparent.png",120,60)}};
doc.Pages[0].BackgroundImageId="background";
doc.Pages[0].GridOpacity=.25;
doc.Pages.Add(new(){WidthMm=297,HeightMm=210,GridSpacingMm=10,GridOpacity=.7});
doc.Pages.Add(new(){WidthMm=150,HeightMm=150,GridOpacity=0});
foreach(var page in doc.Pages){
  foreach(var (angle,x,y) in new[]{(0d,10d,10d),(30d,65d,20d),(90d,110d,15d)})
    page.Marks.Add(new(){Tool=Tool.Image,ImageAssetId="image",RotationDegrees=angle,Opacity=.2,Points=[new(x,y),new(x+30,y+15)]});
  page.Marks.Add(new(){Tool=Tool.Rectangle,Colour="#2463cf",WidthMm=2,Points=[new(10,45),new(65,70)]});
  page.Marks.Add(new(){Tool=Tool.Highlighter,Colour="#f7cd38",Opacity=.35,WidthMm=8,Points=[new(5,55),new(80,55),new(45,60)]});
  foreach(var style in Enum.GetValues<StrokeStyle>()){
    double y=80+(int)style*15;
    page.Marks.Add(new(){Tool=Tool.Pen,StrokeStyle=style,Colour="#e34444",Opacity=.55,WidthMm=.9,Points=[new(8,y),new(18,y+5),new(28,y),new(40,y+5)]});
    page.Marks.Add(new(){Tool=Tool.Line,StrokeStyle=style,Colour="#2463cf",WidthMm=.8,Points=[new(45,y),new(65,y+5)]});
    page.Marks.Add(new(){Tool=Tool.Rectangle,StrokeStyle=style,Colour="#2a9d60",WidthMm=.7,Points=[new(75,y),new(95,y+8)]});
    page.Marks.Add(new(){Tool=Tool.Ellipse,StrokeStyle=style,WidthMm=.7,Points=[new(105,y),new(135,y+8)]});
  }
  page.Marks.Add(new(){Tool=Tool.Pen,WidthMm=3,Points=[new(140,65)]});
  page.Marks.Add(new(){Tool=Tool.Line,WidthMm=2,Points=[new(-10,20),new(20,40)]});
  page.Marks.Add(new(){Tool=Tool.Rectangle,Visible=false,Colour="#ff0000",WidthMm=30,Points=[new(0,0),new(140,140)]});
  page.Marks.Add(new(){Tool=Tool.Image,ImageAssetId="image",Visible=false,Points=[new(0,0),new(140,140)]});
}
DocumentFile.Save(Path.Combine(output,"comparison.gear"),doc);
PdfExporter.Export(Path.Combine(output,"comparison.pdf"),doc,true);
var rendered=PdfImporter.Import(Path.Combine(output,"comparison.pdf"));
for(int i=0;i<rendered.Pages.Count;i++)File.WriteAllBytes(Path.Combine(output,$"reference-{i+1}.png"),rendered.Assets[rendered.Pages[i].BackgroundImageId!].Png);
foreach(int version in new[]{1,2})File.WriteAllText(Path.Combine(output,$"legacy-v{version}.gear"),$"{{\"Version\":{version},\"Pages\":[{{\"Marks\":[{{\"Tool\":0,\"Points\":[{{\"X\":12,\"Y\":20}},{{\"X\":30,\"Y\":45}}]}}]}}]}}");
Console.WriteLine("Generated .gear, PDF and PDFium reference PNGs for three page sizes.");
