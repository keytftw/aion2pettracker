using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PetTracker.App;
public static class AppBrand
{
    // Original wing/crystal emblem inspired by a fantasy theme, not an official AION logo.
    public static Bitmap Draw(int size)
    {
        var bitmap=new Bitmap(size,size);using var g=Graphics.FromImage(bitmap);g.ScaleTransform(size/128f,size/128f);g.SmoothingMode=SmoothingMode.AntiAlias;
        using var background=new LinearGradientBrush(new Rectangle(0,0,128,128),Color.FromArgb(32,24,47),Color.FromArgb(8,15,23),70);using var box=PremiumTheme.Round(new(1,1,126,126),25);g.FillPath(background,box);
        using var gold=new LinearGradientBrush(new Rectangle(0,10,128,108),Color.FromArgb(255,233,170),Color.FromArgb(164,109,44),80);
        for(var side=-1;side<=1;side+=2)for(var feather=0;feather<4;feather++){float y=35+feather*10,x=64+side*(19+feather*3);g.FillPolygon(gold,new PointF[]{new(x,y),new(64+side*53,25+feather*11),new(64+side*43,45+feather*10),new(x,y+8)});}
        using var glow=new Pen(Color.FromArgb(45,PremiumTheme.Pink),10);PointF[] crystal=[new(64,15),new(87,65),new(64,108),new(41,65)];g.DrawPolygon(glow,crystal);g.FillPolygon(gold,crystal);
        using var dark=new SolidBrush(Color.FromArgb(20,20,31));g.FillPolygon(dark,new PointF[]{new(64,34),new(76,64),new(64,88),new(52,64)});using var edge=new Pen(PremiumTheme.Ink,1.3f);g.DrawLines(edge,new PointF[]{new(64,40),new(57,65),new(64,80)});return bitmap;
    }
    public static Icon CreateIcon(){using var bitmap=Draw(64);var handle=bitmap.GetHicon();try{return (Icon)Icon.FromHandle(handle).Clone();}finally{DestroyIcon(handle);}}
    public static void SaveIcon(string path)
    {
        using var stream=File.Create(path);using var writer=new BinaryWriter(stream);writer.Write((ushort)0);writer.Write((ushort)1);writer.Write((ushort)1);
        using var bitmap=Draw(256);using var png=new MemoryStream();bitmap.Save(png,System.Drawing.Imaging.ImageFormat.Png);writer.Write((byte)0);writer.Write((byte)0);writer.Write((byte)0);writer.Write((byte)0);writer.Write((ushort)1);writer.Write((ushort)32);writer.Write((int)png.Length);writer.Write(22);writer.Write(png.ToArray());
    }
    [DllImport("user32.dll")]private static extern bool DestroyIcon(IntPtr handle);
}
