using System.Drawing.Drawing2D;
using PetTracker.Core;
namespace PetTracker.App;
internal static class RiftVisual
{
    internal static readonly Color Violet=Color.FromArgb(185,139,255),Cyan=Color.FromArgb(105,211,226),Coral=Color.FromArgb(243,159,151);
    private static readonly Dictionary<int,Bitmap> Icons=[];
    internal static Image Icon(int size)
    {
        lock(Icons)
        {
            if(Icons.TryGetValue(size,out var cached))return cached;
            using var stream=typeof(RiftVisual).Assembly.GetManifestResourceStream("Rift.Emblem")!;
            using var original=Image.FromStream(stream);var bitmap=new Bitmap(size,size,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using(var graphics=Graphics.FromImage(bitmap)){graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.DrawImage(original,0,0,size,size);}
            Icons[size]=bitmap;return bitmap;
        }
    }
    internal static void DrawNotice(Graphics g,RectangleF box,RiftBannerFrame frame,string language,float scale)
    {
        var compact=box.Width<240;var iconSize=compact?35:48;var textX=box.X+(compact?53:76);
        using var shape=PremiumTheme.Round(box,8);
        using var fill=new LinearGradientBrush(box,Color.FromArgb(34,23,51),Color.FromArgb(17,20,31),10);g.FillPath(fill,shape);
        using var aura=new Pen(Color.FromArgb((int)(20+frame.Pulse*23),Violet),4);g.DrawPath(aura,shape);
        using var border=new Pen(Color.FromArgb((int)(100+frame.Pulse*100),Violet),1);g.DrawPath(border,shape);
        var saved=g.Save();g.SetClip(shape,CombineMode.Intersect);
        var sweepX=box.X-70+(float)((frame.Age*.3)%1)*(box.Width+140);
        using(var sweep=new LinearGradientBrush(new RectangleF(sweepX,box.Y,70,box.Height),Color.Transparent,Color.FromArgb(17,Violet),0f))g.FillRectangle(sweep,sweepX,box.Y,70,box.Height);
        g.DrawImage(Icon(Math.Max(1,(int)Math.Round(iconSize*scale))),new RectangleF(box.X+12,box.Y+(box.Height-iconSize)/2,iconSize,iconSize));
        PremiumTheme.Text(g,"SPACETIME RIFT",new(textX,box.Y+9,box.Right-textX-12,15),compact?6.5f:7.7f,Violet,"en",FontStyle.Bold,StringAlignment.Center);
        PremiumTheme.Text(g,RiftText.Get(language,frame.Opened?"opened":"left",frame.Minutes),new(textX,box.Y+26,box.Right-textX-12,box.Height-32),compact?11:16,PremiumTheme.Ink,language,FontStyle.Bold,StringAlignment.Center);
        using var progress=new SolidBrush(Color.FromArgb(160,Violet));g.FillRectangle(progress,box.X+9,box.Bottom-3,(box.Width-18)*(float)Math.Clamp((RiftBannerTimeline.TotalSeconds-frame.Age)/RiftBannerTimeline.TotalSeconds,0,1),1);
        g.Restore(saved);
    }
}
