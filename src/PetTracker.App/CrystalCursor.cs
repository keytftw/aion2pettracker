using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PetTracker.App;

// App-local cursor handles only; never changes the Windows/game cursor globally.
public sealed class CrystalCursor : IDisposable
{
    private readonly IntPtr handle;
    public Cursor Pointer { get; }
    public CrystalCursor(bool active=false)
    {
        using var bitmap=Draw(active); using var mask=new Bitmap(32,32,PixelFormat.Format1bppIndexed);
        var color=bitmap.GetHbitmap(Color.FromArgb(0));var monochrome=mask.GetHbitmap();
        try
        {
            var info=new IconInfo { Icon=false,HotX=6,HotY=3,Mask=monochrome,Color=color };
            handle=CreateIconIndirect(ref info);
            if(handle==IntPtr.Zero)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            Pointer=new(handle);
        }
        finally { DeleteObject(color);DeleteObject(monochrome); }
    }
    public static Bitmap Draw(bool active=false)
    {
        var bitmap=new Bitmap(32,32,PixelFormat.Format32bppArgb); using var g=Graphics.FromImage(bitmap);g.SmoothingMode=SmoothingMode.AntiAlias;
        PointF[] points=[new(6,3),new(24,12),new(15,16),new(11,27),new(8,16)];
        using var glow=new Pen(Color.FromArgb(active?95:55,236,97,210),4);g.DrawPolygon(glow,points);
        using var fill=new LinearGradientBrush(new Rectangle(5,3,20,25),Color.FromArgb(250,176,239),Color.FromArgb(146,74,234),90f);g.FillPolygon(fill,points);
        using var outline=new Pen(Color.FromArgb(253,220,250),1.2f);g.DrawPolygon(outline,points);
        using var edge=new Pen(Color.FromArgb(188,91,238),1);g.DrawLine(edge,6,3,15,16);
        using var light=new Pen(Color.FromArgb(255,211,246),1);g.DrawLine(light,20,22,26,22);g.DrawLine(light,23,19,23,25);
        return bitmap;
    }
    public void Dispose(){Pointer.Dispose();DestroyIcon(handle);}
    [StructLayout(LayoutKind.Sequential)]private struct IconInfo { [MarshalAs(UnmanagedType.Bool)]public bool Icon;public uint HotX,HotY;public IntPtr Mask,Color; }
    [DllImport("user32.dll",SetLastError=true)]private static extern IntPtr CreateIconIndirect(ref IconInfo info);
    [DllImport("user32.dll")]private static extern bool DestroyIcon(IntPtr icon);
    [DllImport("gdi32.dll")]private static extern bool DeleteObject(IntPtr item);
}
