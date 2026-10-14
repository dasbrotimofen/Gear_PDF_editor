// SPDX-License-Identifier: AGPL-3.0-or-later
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using GearPdf.Core;
namespace GearPdf.App;
internal static class MediaImport {
    public static ImageAsset FromBitmap(BitmapSource source){
        if(source.PixelWidth<1 || source.PixelHeight<1 || source.PixelWidth>10000 || source.PixelHeight>10000 || (long)source.PixelWidth*source.PixelHeight>40000000)throw new InvalidDataException("Image exceeds the 40-megapixel limit.");
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(new FormatConvertedBitmap(source,PixelFormats.Bgra32,null,0)));using var stream=new MemoryStream();encoder.Save(stream);
        return new(){Png=stream.ToArray(),PixelWidth=source.PixelWidth,PixelHeight=source.PixelHeight};
    }
    public static BitmapSource Decode(ImageAsset asset){
        using var stream=new MemoryStream(asset.Png,false);var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);var source=decoder.Frames[0];
        if(source.PixelWidth!=asset.PixelWidth || source.PixelHeight!=asset.PixelHeight)throw new InvalidDataException("Embedded image dimensions do not match.");
        source.Freeze();return source;
    }
}
