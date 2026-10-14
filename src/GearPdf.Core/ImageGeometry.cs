// SPDX-License-Identifier: AGPL-3.0-or-later
namespace GearPdf.Core;
public static class ImageGeometry {
    public static double Normalize(double degrees)=>((degrees%360)+360)%360;
    public static Vec Center(Mark image)=>new((image.Points[0].X+image.Points[1].X)/2,(image.Points[0].Y+image.Points[1].Y)/2);
    public static Vec Rotate(Vec point,Vec center,double degrees){double radians=degrees*Math.PI/180,cos=Math.Cos(radians),sin=Math.Sin(radians),x=point.X-center.X,y=point.Y-center.Y;return new(center.X+x*cos-y*sin,center.Y+x*sin+y*cos);}
    public static Vec Corner(Mark image,bool bottomRight)=>Rotate(image.Points[bottomRight?1:0],Center(image),image.RotationDegrees);
    public static List<Vec> ResizeFromAnchor(Mark image,Vec anchor,Vec pointer,double maxWidth){
        double aspect=(image.Points[1].X-image.Points[0].X)/(image.Points[1].Y-image.Points[0].Y);
        var local=Rotate(pointer,anchor,-image.RotationDegrees);
        double width=Math.Clamp(Math.Max(local.X-anchor.X,(local.Y-anchor.Y)*aspect),1,Math.Max(1,maxWidth)),height=width/aspect;
        var center=Rotate(new(anchor.X+width/2,anchor.Y+height/2),anchor,image.RotationDegrees);
        return [new(center.X-width/2,center.Y-height/2),new(center.X+width/2,center.Y+height/2)];
    }
}
