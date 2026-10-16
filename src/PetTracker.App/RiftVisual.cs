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
        var accent=frame.Opened?Cyan:Violet;
        using var shape=PremiumTheme.Round(box,8);
        using var fill=new LinearGradientBrush(box,Color.FromArgb(34,23,51),Color.FromArgb(17,20,31),10);g.FillPath(fill,shape);
        using var aura=new Pen(Color.FromArgb((int)(18+frame.Pulse*20),accent),4);g.DrawPath(aura,shape);
        using var border=new Pen(Color.FromArgb((int)(115+frame.Pulse*85),accent),1);g.DrawPath(border,shape);
        var saved=g.Save();g.SetClip(shape,CombineMode.Intersect);
        var sweepX=box.X-70+(float)((frame.Age*.3)%1)*(box.Width+140);
        using(var sweep=new LinearGradientBrush(new RectangleF(sweepX,box.Y,70,box.Height),Color.Transparent,Color.FromArgb(17,Violet),0f))g.FillRectangle(sweep,sweepX,box.Y,70,box.Height);
        g.DrawImage(Icon(Math.Max(1,(int)Math.Round(iconSize*scale))),new RectangleF(box.X+12,box.Y+(box.Height-iconSize)/2,iconSize,iconSize));
        var orbit=new RectangleF(box.X+10,box.Y+(box.Height-iconSize)/2-2,iconSize+4,iconSize+4);
        using(var ring=new Pen(Color.FromArgb((int)(90+65*frame.Pulse),accent),1.5f){StartCap=LineCap.Round,EndCap=LineCap.Round})
        {g.DrawArc(ring,orbit,(float)(frame.Age*75),98);g.DrawArc(ring,orbit,(float)(frame.Age*75+180),46);}
        using(var edge=new LinearGradientBrush(new RectangleF(box.X+2,box.Y+10,3,box.Height-20),accent,Color.FromArgb(45,accent),90))g.FillRectangle(edge,box.X+2,box.Y+10,2,box.Height-20);
        PremiumTheme.Text(g,"SPACETIME RIFT",new(textX,box.Y+9,box.Right-textX-12,15),compact?6.5f:7.7f,accent,"en",FontStyle.Bold,StringAlignment.Center);
        PremiumTheme.Text(g,RiftText.Get(language,frame.Opened?"opened":"left",frame.Minutes),new(textX,box.Y+26,box.Right-textX-12,box.Height-32),compact?11:16,PremiumTheme.Ink,language,FontStyle.Bold,StringAlignment.Center);
        using var progress=new SolidBrush(Color.FromArgb(160,accent));g.FillRectangle(progress,box.X+9,box.Bottom-3,(box.Width-18)*(float)Math.Clamp((RiftBannerTimeline.TotalSeconds-frame.Age)/RiftBannerTimeline.TotalSeconds,0,1),1);
        g.Restore(saved);
    }
}
