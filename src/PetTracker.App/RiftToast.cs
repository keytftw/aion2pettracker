using PetTracker.Core;
namespace PetTracker.App;
public sealed class RiftToast : Form
{
    private readonly System.Windows.Forms.Timer refresh=new(){Interval=8};
    private long lastPaint;
    private readonly Func<RiftBannerFrame> frame;
    private readonly string language;
    public RiftToast(Func<RiftBannerFrame> frame,string language,Screen? screen=null)
    {
        this.frame=frame;this.language=language;
        AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new(96,96);ClientSize=new(350,86);
        FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;
        BackColor=PremiumTheme.Background;DoubleBuffered=true;Opacity=.01;
        void Position(){var area=(screen??Screen.PrimaryScreen!).WorkingArea;Location=new(area.Left+15,area.Top+15);}
        Position();Shown+=(_,_)=>{Position();refresh.Start();};
        refresh.Tick+=(_,_)=>{var state=frame();if(!state.Visible){Close();return;}Opacity=Math.Max(.01,state.Reveal);if(System.Diagnostics.Stopwatch.GetElapsedTime(lastPaint).TotalSeconds>=1d/70){lastPaint=System.Diagnostics.Stopwatch.GetTimestamp();Invalidate();}};
    }
    protected override bool ShowWithoutActivation=>true;
    protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
    protected override void OnPaint(PaintEventArgs e)
    {base.OnPaint(e);var scale=DeviceDpi/96f;e.Graphics.ScaleTransform(scale,scale);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;RiftVisual.DrawNotice(e.Graphics,new(3,3,344,80),frame(),language,scale);}
    protected override void Dispose(bool disposing){if(disposing)refresh.Dispose();base.Dispose(disposing);}
}
