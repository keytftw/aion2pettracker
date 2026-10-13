using System.Drawing.Drawing2D;

namespace PetTracker.App;

internal static class PremiumTheme
{
    internal static readonly Color Background=Color.FromArgb(13,16,22),Panel=Color.FromArgb(23,27,35),Gold=Color.FromArgb(216,179,105),Ink=Color.FromArgb(237,233,224),Muted=Color.FromArgb(145,150,161),Pink=Color.FromArgb(220,128,218);
    internal static GraphicsPath Round(RectangleF r,float radius=6)
    {
        var p=new GraphicsPath();var d=radius*2;
        p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    internal static void Text(Graphics g,string text,RectangleF r,float size,Color color,string language="tr",FontStyle style=FontStyle.Regular,StringAlignment align=StringAlignment.Near)
    {
        var rendering=g.TextRenderingHint;
        // Grayscale antialiasing remains smooth with fractional DPI scaling and window opacity.
        g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        try
        {
        using var font=new Font(UiText.FontFamily(language),size*96f/72f,style,GraphicsUnit.Pixel);using var b=new SolidBrush(color);
        using var format=new StringFormat { Alignment=align,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter };
        if(language=="ar")format.FormatFlags|=StringFormatFlags.DirectionRightToLeft;
        g.DrawString(text,font,b,r,format);
        }
        finally{g.TextRenderingHint=rendering;}
    }
}

public sealed class PremiumButton : Control
{
    private readonly System.Windows.Forms.Timer timer=new(){Interval=16};
    private bool hover;private float blend;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string TextLanguage { get; set; }="tr";
    public PremiumButton()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Gold;Size=new(120,30);TabStop=true;
        timer.Tick+=(_,_)=>{blend+=( (hover?1:0)-blend)*.2f;Invalidate();if(Math.Abs(blend-(hover?1:0))<.01f)timer.Stop();};
    }
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;timer.Start();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;timer.Start();}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode is Keys.Enter or Keys.Space)OnClick(EventArgs.Empty);}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(1,1,Width-2,Height-2);using var p=PremiumTheme.Round(r);
        using var b=new SolidBrush(Color.FromArgb((int)(22+blend*25),PremiumTheme.Gold));e.Graphics.FillPath(b,p);
        using var pen=new Pen(Color.FromArgb((int)(75+blend*75),PremiumTheme.Gold));e.Graphics.DrawPath(pen,p);
        PremiumTheme.Text(e.Graphics,Text,r,8.8f,ForeColor,TextLanguage,style:FontStyle.Bold,align:StringAlignment.Center);
    }
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}

public sealed class PremiumSlider : Control
{
    private int value=30;private float shown=30;private bool dragging,hover,painted;
    private readonly System.Windows.Forms.Timer timer=new(){Interval=16};
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Minimum { get; set; }=30;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Maximum { get; set; }=100;
    public event EventHandler? ValueChanged;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Value { get=>value;set {var next=Math.Clamp(value,Minimum,Maximum);if(next==this.value)return;this.value=next;ValueChanged?.Invoke(this,EventArgs.Empty);timer.Start();Invalidate();} }
    public PremiumSlider()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Size=new(280,34);BackColor=PremiumTheme.Background;TabStop=true;
        timer.Tick+=(_,_)=>{shown+=(value-shown)*.24f;Invalidate();if(Math.Abs(shown-value)<.05){shown=value;timer.Stop();}};
    }
    private void UpdateValue(int x)=>Value=Minimum+(int)Math.Round(Math.Clamp((x-11f)/Math.Max(1,Width-22),0,1)*(Maximum-Minimum));
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;dragging=true;Capture=true;UpdateValue(e.X);}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)UpdateValue(e.X);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Left)Value--;if(e.KeyCode==Keys.Right)Value++;}
    protected override void OnPaint(PaintEventArgs e)
    {
        if(!painted){shown=value;painted=true;}
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var y=Height/2f;var x=11+(Math.Clamp(shown,Minimum,Maximum)-Minimum)/Math.Max(1,Maximum-Minimum)*(Width-22);
        using var track=new Pen(Color.FromArgb(44,47,57),3){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(track,11,y,Width-11,y);
        using var active=new Pen(PremiumTheme.Gold,3){StartCap=LineCap.Round,EndCap=LineCap.Round};g.DrawLine(active,11,y,x,y);
        using var glow=new SolidBrush(Color.FromArgb(hover||dragging?35:18,PremiumTheme.Gold));g.FillEllipse(glow,x-11,y-11,22,22);
        using var fill=new SolidBrush(PremiumTheme.Ink);g.FillEllipse(fill,x-5,y-5,10,10);
        using var outline=new Pen(PremiumTheme.Gold,1.5f);g.DrawEllipse(outline,x-5,y-5,10,10);
    }
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}

internal static class PremiumDialogMotion
{
    public static void ShowForPreview(Form form)
    {
        form.StartPosition=FormStartPosition.Manual;form.Location=new(-3000,-3000);form.Show();
        var time=System.Diagnostics.Stopwatch.StartNew();
        while(time.Elapsed.TotalSeconds<.23){Application.DoEvents();System.Threading.Thread.Sleep(5);}
        form.Opacity=1;
    }
    public static void Attach(Form form)
    {
        var timer=new System.Windows.Forms.Timer {Interval=16};var time=new System.Diagnostics.Stopwatch();
        form.Opacity=0;
        form.Shown+=(_,_)=>{time.Restart();timer.Start();};
        timer.Tick+=(_,_)=>{var t=Math.Clamp(time.Elapsed.TotalSeconds/.18,0,1);form.Opacity=1-Math.Pow(1-t,3);if(t>=1)timer.Stop();};
        form.Disposed+=(_,_)=>timer.Dispose();
    }
}
