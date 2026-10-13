using System.Drawing.Drawing2D;
namespace PetTracker.App;

// A locally rendered, keyboard and mouse-scrollable document with an animated dark thumb.
public sealed class TermsView : Control
{
    private Bitmap? document;
    private float scroll,target;
    private bool dragging;
    private readonly System.Windows.Forms.Timer timer=new(){Interval=16};
    public TermsView()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);TabStop=true;BackColor=PremiumTheme.Panel;ForeColor=PremiumTheme.Ink;
        timer.Tick+=(_,_)=>{scroll+=(target-scroll)*.17f;Invalidate();if(Math.Abs(target-scroll)<.3f){scroll=target;timer.Stop();}};
    }
    private float Maximum=>Math.Max(0,(document?.Height??0)-Height+14);
    private RectangleF Thumb
    {get{var height=Math.Max(35,Height*Height/(float)Math.Max(Height,document?.Height??Height));return new(Width-9,scroll/Math.Max(1,Maximum)*(Height-height),4,height);}}
    private void Rebuild()
    {
        document?.Dispose();document=null;if(Width<40||Height<30||string.IsNullOrEmpty(Text))return;
        using var measure=CreateGraphics();using var format=new StringFormat{Trimming=StringTrimming.None};if(RightToLeft==RightToLeft.Yes)format.FormatFlags|=StringFormatFlags.DirectionRightToLeft;
        var height=(int)Math.Ceiling(measure.MeasureString(Text,Font,Width-38,format).Height)+28;
        document=new Bitmap(Width-14,Math.Max(Height,height));using var g=Graphics.FromImage(document);g.Clear(BackColor);g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;using var brush=new SolidBrush(ForeColor);g.DrawString(Text,Font,brush,new RectangleF(12,12,Width-38,height),format);
        target=Math.Clamp(target,0,Maximum);scroll=Math.Clamp(scroll,0,Maximum);Invalidate();
    }
    protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);scroll=target=0;Rebuild();}
    protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);Rebuild();}
    protected override void OnResize(EventArgs e){base.OnResize(e);Rebuild();}
    protected override void OnRightToLeftChanged(EventArgs e){base.OnRightToLeftChanged(e);Rebuild();}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);if(document is not null)e.Graphics.DrawImageUnscaled(document,0,-(int)scroll);
        if(Maximum>0){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var track=new Pen(Color.FromArgb(45,PremiumTheme.Gold),2);e.Graphics.DrawLine(track,Width-7,4,Width-7,Height-4);using var path=PremiumTheme.Round(Thumb,2);using var thumb=new SolidBrush(PremiumTheme.Gold);e.Graphics.FillPath(thumb,path);}
    }
    private void MoveTo(float value){target=Math.Clamp(value,0,Maximum);timer.Start();}
    protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);MoveTo(target-e.Delta/120f*70);}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();if(e.Button==MouseButtons.Left&&e.X>Width-18){dragging=true;Capture=true;Drag(e.Y);}}
    private void Drag(int y)=>MoveTo((y-Thumb.Height/2)/Math.Max(1,Height-Thumb.Height)*Maximum);
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)Drag(e.Y);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
    protected override bool IsInputKey(Keys keyData)=>keyData is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End||base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);switch(e.KeyCode){case Keys.Down:MoveTo(target+35);break;case Keys.Up:MoveTo(target-35);break;case Keys.PageDown:MoveTo(target+Height-40);break;case Keys.PageUp:MoveTo(target-Height+40);break;case Keys.Home:MoveTo(0);break;case Keys.End:MoveTo(Maximum);break;}}
    protected override void Dispose(bool disposing){if(disposing){timer.Dispose();document?.Dispose();}base.Dispose(disposing);}
}
