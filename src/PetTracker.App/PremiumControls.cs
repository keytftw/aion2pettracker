using PetTracker.Core;
using System.Drawing.Drawing2D;

namespace PetTracker.App;

internal static class PremiumTheme
{
    internal static readonly Color Background=Color.FromArgb(13,16,22),Panel=Color.FromArgb(23,27,35),Gold=Color.FromArgb(216,179,105),Ink=Color.FromArgb(237,233,224),Muted=Color.FromArgb(145,150,161),Pink=Color.FromArgb(220,128,218);
    private readonly record struct TextKey(string Text,string Language,float Size,FontStyle Style,bool Heading,int Width,int Height,int Rgb,StringAlignment Align);
    private static readonly Dictionary<TextKey,Bitmap> TextCache=[];
    private static readonly Queue<TextKey> TextOrder=[];
    internal static GraphicsPath Round(RectangleF r,float radius=6)
    {
        var p=new GraphicsPath();var d=radius*2;
        p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    internal static void Text(Graphics g,string text,RectangleF r,float size,Color color,string language="tr",FontStyle style=FontStyle.Regular,StringAlignment align=StringAlignment.Near,bool heading=false)
    {
        var state=g.Save();
        // Draw glyphs at their final device size instead of transforming grid-fitted text.
        using var transform=g.Transform;
        var corners=new[]{new PointF(r.Left,r.Top),new PointF(r.Right,r.Bottom)};
        transform.TransformPoints(corners);
        var elements=transform.Elements;
        var scale=(float)Math.Sqrt(elements[0]*elements[0]+elements[1]*elements[1]);
        r=new RectangleF((float)Math.Round(corners[0].X),(float)Math.Round(corners[0].Y),corners[1].X-corners[0].X,corners[1].Y-corners[0].Y);
        g.ResetTransform();size*=scale;
        try
        {
            if(color.A==0||r.Width<=0||r.Height<=0)return;
            var width=Math.Max(1,(int)Math.Ceiling(r.Width));var height=Math.Max(1,(int)Math.Ceiling(r.Height));
            var key=new TextKey(text,language,(float)Math.Round(size,2),style,heading,width,height,color.ToArgb()&0xffffff,align);
            if(!TextCache.TryGetValue(key,out var bitmap))
            {
                // Render at twice the final resolution with unhinted grayscale edges.
                // Cache opaque RGB glyphs; animated/window opacity is applied only at compositing.
                using var source=new Bitmap(width*2,height*2,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using(var ink=Graphics.FromImage(source))
                using(var font=FontAssets.Create(language,key.Size*96f/72f*2,style,heading))
                using(var brush=new SolidBrush(Color.FromArgb(255,color.R,color.G,color.B)))
                using(var format=new StringFormat { Alignment=align,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter })
                {
                    ink.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAlias;
                    if(language=="ar")format.FormatFlags|=StringFormatFlags.DirectionRightToLeft;
                    ink.DrawString(text,font,brush,new RectangleF(0,0,width*2,height*2),format);
                }
                bitmap=new Bitmap(width,height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                using(var reduced=Graphics.FromImage(bitmap))
                {
                    reduced.CompositingMode=CompositingMode.SourceCopy;
                    reduced.InterpolationMode=InterpolationMode.HighQualityBicubic;reduced.PixelOffsetMode=PixelOffsetMode.HighQuality;
                    using var wrap=new System.Drawing.Imaging.ImageAttributes();wrap.SetWrapMode(WrapMode.Clamp);
                    reduced.DrawImage(source,new Rectangle(0,0,width,height),0,0,source.Width,source.Height,GraphicsUnit.Pixel,wrap);
                }
                while(TextCache.Count>=256){var old=TextOrder.Dequeue();TextCache[old].Dispose();TextCache.Remove(old);}
                TextCache.Add(key,bitmap);TextOrder.Enqueue(key);
            }
            if(color.A==255){g.DrawImageUnscaled(bitmap,(int)r.X,(int)r.Y);return;}
            g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            using var attributes=new System.Drawing.Imaging.ImageAttributes();
            attributes.SetWrapMode(WrapMode.Clamp);
            attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix{Matrix33=color.A/255f});
            g.DrawImage(bitmap,new Rectangle((int)r.X,(int)r.Y,width,height),0,0,bitmap.Width,bitmap.Height,GraphicsUnit.Pixel,attributes);
        }
        finally{g.Restore(state);}
    }
}

public sealed class PremiumLabel : Label
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string TextLanguage { get; set; }="en";
    public PremiumLabel()=>SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
    protected override void OnPaint(PaintEventArgs e)
    {
        var alignment=TextAlign is ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter?StringAlignment.Center:
            TextAlign is ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight?StringAlignment.Far:StringAlignment.Near;
        PremiumTheme.Text(e.Graphics,Text,ClientRectangle,Font.SizeInPoints*DeviceDpi/96f,ForeColor,TextLanguage,Font.Style,alignment);
    }
}

public sealed class PremiumButton : Control
{
    private readonly System.Windows.Forms.Timer timer=new(){Interval=8};
    private readonly AnimationClock animationClock=new();
    private bool hover;private float blend;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string TextLanguage { get; set; }="tr";
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool DpiAwareText { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public float TextSize { get; set; }=8.8f;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool? LockIcon { get; set; }
    public PremiumButton()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
        BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Gold;Size=new(120,30);TabStop=true;
        timer.Tick+=(_,_)=>{blend=FrameSmoothing.Approach(blend,hover?1:0,animationClock.Step(),16);Invalidate();if(Math.Abs(blend-(hover?1:0))<.01f)timer.Stop();};
    }
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;timer.Start();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;timer.Start();}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode is Keys.Enter or Keys.Space)OnClick(EventArgs.Empty);}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;var r=new RectangleF(1,1,Width-2,Height-2);using var p=PremiumTheme.Round(r);
        using var b=new SolidBrush(Color.FromArgb((int)(22+blend*25),PremiumTheme.Gold));e.Graphics.FillPath(b,p);
        using var pen=new Pen(Color.FromArgb((int)(75+blend*75),PremiumTheme.Gold));e.Graphics.DrawPath(pen,p);
        if(LockIcon is { } locked)
        {
            var scale=DeviceDpi/96f;var cx=Width/2f;var cy=Height/2f;
            using var icon=new Pen(ForeColor,1.5f*scale);
            e.Graphics.DrawRectangle(icon,cx-5*scale,cy,10*scale,7*scale);
            e.Graphics.DrawArc(icon,cx-(locked?4:1)*scale,cy-7*scale,8*scale,10*scale,180,180);
            e.Graphics.DrawLine(icon,cx,cy+2*scale,cx,cy+5*scale);return;
        }
        var fontSize=TextSize;
#if SETUP_UI
        fontSize*=DeviceDpi/96f;
#else
        if(DpiAwareText)fontSize*=DeviceDpi/96f;
#endif
        PremiumTheme.Text(e.Graphics,Text,r,fontSize,ForeColor,TextLanguage,style:FontStyle.Bold,align:StringAlignment.Center);
    }
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}

public sealed class PremiumSlider : Control
{
    private readonly AnimationClock animationClock=new();
    private int value=30;private float shown=30;private bool dragging,hover,painted;
    private readonly System.Windows.Forms.Timer timer=new(){Interval=8};
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
        timer.Tick+=(_,_)=>{shown=FrameSmoothing.Approach(shown,value,animationClock.Step(),18,.05f);Invalidate();if(Math.Abs(shown-value)<.05){shown=value;timer.Stop();}};
    }
    private void UpdateValue(int x)=>Value=Minimum+(int)Math.Round(Math.Clamp((x-11f)/Math.Max(1,Width-22),0,1)*(Maximum-Minimum));
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;dragging=true;Capture=true;UpdateValue(e.X);}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)UpdateValue(e.X);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
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
        var timer=new System.Windows.Forms.Timer {Interval=8};var time=new System.Diagnostics.Stopwatch();
        form.Opacity=0;
        form.Shown+=(_,_)=>{time.Restart();timer.Start();};
        timer.Tick+=(_,_)=>{var t=Math.Clamp(time.Elapsed.TotalSeconds/.18,0,1);form.Opacity=1-Math.Pow(1-t,3);if(t>=1)timer.Stop();};
        form.Disposed+=(_,_)=>timer.Dispose();
    }
}
