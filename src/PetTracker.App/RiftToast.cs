using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace PetTracker.App;

public sealed class RiftToast : Form
{
    private readonly System.Windows.Forms.Timer lifetime=new(){Interval=40};
    private readonly Stopwatch elapsed=Stopwatch.StartNew();
    private readonly string message,language;
    public RiftToast(string message,string language)
    {
        this.message=message;this.language=language;
        AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new(96,96);
        ClientSize=new(350,82);FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;
        BackColor=PremiumTheme.Background;DoubleBuffered=true;
        var area=Screen.PrimaryScreen!.WorkingArea;Location=new(area.Right-Width-18,area.Top+18);
        lifetime.Tick+=(_,_)=>{if(elapsed.Elapsed.TotalSeconds>=5)Close();else Invalidate();};
        Shown+=(_,_)=>{var working=Screen.PrimaryScreen!.WorkingArea;var margin=(int)(18*DeviceDpi/96f);Location=new(working.Right-Width-margin,working.Top+margin);elapsed.Restart();lifetime.Start();};
    }
    protected override bool ShowWithoutActivation=>true;
    protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var scale=DeviceDpi/96f;e.Graphics.ScaleTransform(scale,scale);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using var pen=new Pen(PremiumTheme.Gold);e.Graphics.DrawRectangle(pen,0,0,349,81);
        PremiumTheme.Text(e.Graphics,"SPACETIME RIFT",new(16,10,318,22),9,PremiumTheme.Gold,"en",FontStyle.Bold);
        PremiumTheme.Text(e.Graphics,message,new(16,33,318,31),12,PremiumTheme.Ink,language);
        using var brush=new SolidBrush(PremiumTheme.Gold);e.Graphics.FillRectangle(brush,0,79,(float)(350*Math.Max(0,1-elapsed.Elapsed.TotalSeconds/5)),2);
    }
    protected override void Dispose(bool disposing){if(disposing)lifetime.Dispose();base.Dispose(disposing);}
}
