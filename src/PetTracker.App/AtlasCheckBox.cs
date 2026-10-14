using System.Drawing.Drawing2D;
namespace PetTracker.App;

public sealed class AtlasCheckBox : CheckBox
{
    private readonly string language;
    public AtlasCheckBox(string language)
    {
        this.language=language;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.Clear(BackColor);var scale=DeviceDpi/96f;g.ScaleTransform(scale,scale);g.SmoothingMode=SmoothingMode.AntiAlias;
        var y=(Height/scale-15)/2;var box=new RectangleF(1,y,15,15);
        using var path=PremiumTheme.Round(box,3);using var fill=new SolidBrush(Checked&&Enabled?PremiumTheme.Gold:PremiumTheme.Background);
        using var edge=new Pen(Enabled?PremiumTheme.Gold:PremiumTheme.Muted);g.FillPath(fill,path);g.DrawPath(edge,path);
        if(Checked){using var check=new Pen(Enabled?PremiumTheme.Background:PremiumTheme.Muted,1.8f);g.DrawLines(check,new PointF[]{new(4,y+7),new(7,y+10),new(13,y+4)});}
        PremiumTheme.Text(g,Text,new(25,0,Width/scale-27,Height/scale),Font.Size,Enabled?ForeColor:PremiumTheme.Muted,language);
    }
    protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
}

