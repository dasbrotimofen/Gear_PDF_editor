// SPDX-License-Identifier: AGPL-3.0-or-later
using GearPdf.Core;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
namespace GearPdf.Pdf;
public static class PdfExporter {
    private const double PointsPerMm=72/25.4;
    private static XPoint Point(Vec p)=>new(p.X*PointsPerMm,p.Y*PointsPerMm);
    private static XRect Bounds(Mark m){var a=Point(m.Points[0]);var b=Point(m.Points[^1]);return new(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));}
    private static XColor Colour(string hex,double opacity){int rgb=Convert.ToInt32(hex[1..],16);return XColor.FromArgb((int)Math.Round(opacity*255),(rgb>>16)&255,(rgb>>8)&255,rgb&255);}
    public static void Export(string path,Document document,bool includeGrid){
        DocumentFile.Validate(document);
        using var pdf=new PdfDocument();pdf.Info.Title="GEAR sketch and markup";pdf.Info.Creator="GEAR PDF Editor";
        foreach(var source in document.Pages){
            var page=pdf.AddPage();page.Width=XUnit.FromMillimeter(source.WidthMm);page.Height=XUnit.FromMillimeter(source.HeightMm);
            using var graphics=XGraphics.FromPdfPage(page);graphics.IntersectClip(new XRect(0,0,page.Width.Point,page.Height.Point));
            if(source.BackgroundImageId is string background)DrawImage(graphics,document.Assets[background],new(0,0,page.Width.Point,page.Height.Point));
            if(includeGrid && source.GridOpacity>0){
                for(int axis=0;axis<2;axis++){
                    double limit=axis==0?source.WidthMm:source.HeightMm;
                    for(int i=1;i*source.GridSpacingMm<limit;i++){
                        double v=i*source.GridSpacingMm*PointsPerMm;
                        var pen=new XPen(Colour("#6d91ad",Math.Min(1,source.GridOpacity*(i%5==0?1.5:1))),i%5==0?.3:.18);
                        if(axis==0)graphics.DrawLine(pen,v,0,v,page.Height.Point);else graphics.DrawLine(pen,0,v,page.Width.Point,v);
                    }
                }
            }
            foreach(var mark in source.Marks){
                if(mark.Tool==Tool.Image){var bounds=Bounds(mark);var state=graphics.Save();double cx=bounds.X+bounds.Width/2,cy=bounds.Y+bounds.Height/2;graphics.TranslateTransform(cx,cy);graphics.RotateTransform(mark.RotationDegrees);DrawImage(graphics,document.Assets[mark.ImageAssetId!],new(-bounds.Width/2,-bounds.Height/2,bounds.Width,bounds.Height));graphics.Restore(state);continue;}
                var colour=Colour(mark.Colour,mark.Opacity);var pen=new XPen(colour,mark.WidthMm*PointsPerMm){LineCap=XLineCap.Round,LineJoin=XLineJoin.Round};
                if(mark.Tool==Tool.Rectangle)graphics.DrawRectangle(pen,Bounds(mark));
                else if(mark.Tool==Tool.Ellipse)graphics.DrawEllipse(pen,Bounds(mark));
                else if(mark.Points.Count==1){var point=Point(mark.Points[0]);double radius=pen.Width/2;graphics.DrawEllipse(new XSolidBrush(colour),point.X-radius,point.Y-radius,radius*2,radius*2);}
                else graphics.DrawLines(pen,mark.Points.Select(Point).ToArray());
            }
        }
        string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{pdf.Save(temporary);File.Move(temporary,path,true);}finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    private static void DrawImage(XGraphics graphics,ImageAsset asset,XRect bounds){using var stream=new MemoryStream(asset.Png,false);using var image=XImage.FromStream(stream);graphics.DrawImage(image,bounds);}
}
