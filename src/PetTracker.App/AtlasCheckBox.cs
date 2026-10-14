using System.Drawing.Drawing2D;
namespace PetTracker.App;

public sealed class AtlasCheckBox : CheckBox
{
    private readonly string language;
    private readonly System.Windows.Forms.Timer animation=new(){Interval=16};
    private readonly AnimationClock clock=new();
    private float position;
    private bool hovered;
    public AtlasCheckBox(string language)
    {
        this.language=language;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        animation.Tick+=(_,_)=>{position=PetTracker.Core.FrameSmoothing.Approach(position,Checked?1:0,clock.Step(),22);Invalidate();if(Math.Abs(position-(Checked?1:0))<.005){position=Checked?1:0;animation.Stop();}};
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(BackColor);var scale=DeviceDpi/96f;g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;
        var y=(Height/scale-18)/2;var box=new RectangleF(3,y,18,18);
        var t=Enabled?position:0;var a=PremiumTheme.Background;var b=PremiumTheme.Gold;
        using var path=PremiumTheme.Round(box,5);using var fill=new SolidBrush(Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t)));
        using var edge=new Pen(Enabled?PremiumTheme.Gold:PremiumTheme.Muted);g.FillPath(fill,path);g.DrawPath(edge,path);
        if(position>0){using var check=new Pen(Color.FromArgb((int)(255*position),Enabled?PremiumTheme.Background:PremiumTheme.Muted),2f){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLines(check,new PointF[]{new(7,y+9),new(11,y+13),new(17,y+6)});}
        if(Focused||hovered){using var halo=new Pen(Color.FromArgb(65,PremiumTheme.Gold));using var ring=PremiumTheme.Round(new(1,y-2,22,22),6);g.DrawPath(halo,ring);}
        PremiumTheme.Text(g,Text,new(31,0,Width/scale-33,Height/scale),Font.Size,Enabled?ForeColor:PremiumTheme.Muted,language);
    }
    protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);if(IsHandleCreated){clock.Step();animation.Start();}else position=Checked?1:0;Invalidate();}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hovered=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hovered=false;Invalidate();}
    protected override void Dispose(bool disposing){if(disposing)animation.Dispose();base.Dispose(disposing);}
}
