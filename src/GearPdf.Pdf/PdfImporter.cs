// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Docnet.Core;
using Docnet.Core.Models;
using GearPdf.Core;
namespace GearPdf.Pdf;
public static class PdfImporter {
    private static readonly object RendererLock=new();
    public static Task<Document> ImportAsync(string path,IProgress<string>? progress=null)=>Task.Run(()=>Import(path,progress));
    public static Document Import(string path,IProgress<string>? progress=null){
        if(new FileInfo(path).Length>50L*1024*1024)throw new InvalidDataException("PDF import is limited to 50 MB.");
        var bytes=File.ReadAllBytes(path);lock(RendererLock){
            const double scale=2;using var reader=DocLib.Instance.GetDocReader(bytes,new PageDimensions(scale));
            int count=reader.GetPageCount();if(count is <1 or >25)throw new InvalidDataException("This starter supports importing up to 25 PDF pages at once.");
            var document=new Document{Pages=[]};long total=0;
            for(int i=0;i<count;i++){
                progress?.Report($"Importing PDF page {i+1}/{count}…");using var page=reader.GetPageReader(i);
                int width=page.GetPageWidth(),height=page.GetPageHeight();if(width<1 || height<1 || width>10000 || height>10000 || (long)width*height>40000000)throw new InvalidDataException("Rendered page exceeds the 40-megapixel limit.");
                var raw=page.GetImage(RenderFlags.RenderAnnotations);var png=PngRaster.EncodeBgraOnWhite(raw,width,height);total+=png.Length;
                if(total>128L*1024*1024)throw new InvalidDataException("Rendered PDF pages exceed the 128 MB image limit.");
                string id=Guid.NewGuid().ToString("N");document.Assets.Add(id,new(){Png=png,PixelWidth=width,PixelHeight=height});
                document.Pages.Add(new(){WidthMm=width/scale*25.4/72,HeightMm=height/scale*25.4/72,GridOpacity=0,BackgroundImageId=id});
            }
            DocumentFile.Validate(document);return document;
        }
    }
}
internal static class PngRaster {
    public static byte[] EncodeBgraOnWhite(byte[] pixels,int width,int height){
        if(pixels.Length!=checked(width*height*4))throw new InvalidDataException("Unexpected PDF bitmap size.");
        using var compressed=new MemoryStream();
        using(var zlib=new ZLibStream(compressed,CompressionLevel.Optimal,true)){
            var row=new byte[checked(width*3+1)];
            for(int y=0;y<height;y++){
                for(int x=0;x<width;x++){
                    int src=(y*width+x)*4,dest=x*3+1,alpha=pixels[src+3];
                    row[dest]=(byte)((pixels[src+2]*alpha+255*(255-alpha)+127)/255);
                    row[dest+1]=(byte)((pixels[src+1]*alpha+255*(255-alpha)+127)/255);
                    row[dest+2]=(byte)((pixels[src]*alpha+255*(255-alpha)+127)/255);
                }zlib.Write(row);
            }
        }
        using var output=new MemoryStream();output.Write(new byte[]{137,80,78,71,13,10,26,10});
        byte[] header=new byte[13];BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0,4),width);BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4,4),height);header[8]=8;header[9]=2;
        Chunk(output,"IHDR",header);Chunk(output,"IDAT",compressed.ToArray());Chunk(output,"IEND",[]);return output.ToArray();
    }
    private static void Chunk(Stream stream,string name,byte[] data){
        Span<byte> number=stackalloc byte[4];BinaryPrimitives.WriteInt32BigEndian(number,data.Length);stream.Write(number);var type=Encoding.ASCII.GetBytes(name);stream.Write(type);stream.Write(data);
        uint crc=0xffffffff;foreach(byte value in type.Concat(data)){crc^=value;for(int i=0;i<8;i++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0);}BinaryPrimitives.WriteUInt32BigEndian(number,crc^0xffffffff);stream.Write(number);
    }
}
