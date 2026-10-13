using System.Drawing.Drawing2D;
namespace PetTracker.App;

public sealed class PetMapCarousel : Control
{
    private readonly IReadOnlyDictionary<int,Image> images;
    private PetDefinition[] pets=[];
    private readonly Func<int,long?> total;
    private readonly System.Windows.Forms.Timer animation=new(){Interval=8};
    private readonly AnimationClock animationClock=new();
    private float offset,target;
    private int selected,hover=-1;
    private bool dragging,moved;
    private int startX;
    private float startOffset;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Language { get; set; }="en";
    public event Action<int>? PetSelected;
    private float DpiFactor=>DeviceDpi/96f;
    private float Stride=>210*DpiFactor;
    private float Maximum=>Math.Max(0,pets.Length*Stride-Width);
    public PetMapCarousel(IReadOnlyDictionary<int,Image> images,Func<int,long?> total)
    {
        this.images=images;this.total=total;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=PremiumTheme.Background;TabStop=true;
        animation.Tick+=(_,_)=>{offset=FrameSmoothing.Approach(offset,target,animationClock.Step(),18,.2f);if(Math.Abs(offset-target)<.2){offset=target;animation.Stop();}Invalidate();};
    }
    public void SetPets(PetDefinition[] matches,int selectedId)
    {pets=matches;selected=selectedId;target=Math.Clamp(target,0,Maximum);offset=Math.Clamp(offset,0,Maximum);EnsureSelectedVisible();Invalidate();}
    public void EnsureSelectedVisible()
    {
        var index=Array.FindIndex(pets,pet=>pet.Id==selected);if(index<0)return;
        var left=index*Stride;var right=left+Stride;
        if(left<target)target=left;else if(right>target+Width)target=right-Width;
        target=Math.Clamp(target,0,Maximum);animation.Start();
    }
    public void ScrollBy(double direction){target=Math.Clamp(target+(float)direction*Stride*2,0,Maximum);animation.Start();}
    protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);ScrollBy(-e.Delta/120d);}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);if(FindForm()?.ContainsFocus==true)Focus();}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();dragging=true;moved=false;startX=e.X;startOffset=target;Capture=true;}
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if(dragging){if(Math.Abs(e.X-startX)>5*DpiFactor)moved=true;target=Math.Clamp(startOffset-(e.X-startX),0,Maximum);animation.Start();}
        var next=(int)((e.X+offset)/Stride);if(next!=hover){hover=next;Invalidate();}
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);if(!dragging)return;dragging=false;Capture=false;
        var index=(int)((e.X+offset)/Stride);if(!moved&&index>=0&&index<pets.Length)PetSelected?.Invoke(pets[index].Id);
    }
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=-1;Invalidate();}
    protected override bool IsInputKey(Keys keyData)=>keyData is Keys.Left or Keys.Right or Keys.Home or Keys.End||base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);if(pets.Length==0)return;var index=Array.FindIndex(pets,pet=>pet.Id==selected);
        var next=e.KeyCode switch{Keys.Left=>index<0?pets.Length-1:Math.Max(0,index-1),Keys.Right=>Math.Min(pets.Length-1,index+1),Keys.Home=>0,Keys.End=>pets.Length-1,_=>-1};
        if(next>=0){PetSelected?.Invoke(pets[next].Id);e.Handled=true;}
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);target=Math.Clamp(target,0,Maximum);offset=Math.Clamp(offset,0,Maximum);Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        if(pets.Length==0){PremiumTheme.Text(g,UiText.Get(Language,"noResults"),ClientRectangle,10*DpiFactor,PremiumTheme.Muted,Language,align:StringAlignment.Center);return;}
        for(var index=Math.Max(0,(int)(offset/Stride));index<pets.Length;index++)
        {
            var x=index*Stride-offset;if(x>Width)break;var pet=pets[index];var isSelected=pet.Id==selected;
            var rect=new RectangleF(x+2*DpiFactor,2*DpiFactor,Stride-9*DpiFactor,Height-12*DpiFactor);using var path=PremiumTheme.Round(rect,7*DpiFactor);
            using var fill=new SolidBrush(isSelected?Color.FromArgb(43,36,36):index==hover?Color.FromArgb(32,31,39):PremiumTheme.Panel);g.FillPath(fill,path);
            using var border=new Pen(Color.FromArgb(isSelected?190:index==hover?115:45,PremiumTheme.Gold),DpiFactor);g.DrawPath(border,path);
            if(images.TryGetValue(pet.Id,out var image))g.DrawImage(image,new RectangleF(x+10*DpiFactor,13*DpiFactor,33*DpiFactor,33*DpiFactor));
            PremiumTheme.Text(g,pet.Name,new(x+50*DpiFactor,8*DpiFactor,Stride-64*DpiFactor,36*DpiFactor),10.2f*DpiFactor,PremiumTheme.Ink,"en",FontStyle.Bold);
            var known=total(pet.Id);var progress=known.HasValue?Progress.FromTotal(known.Value):null;
            var label=progress is null?pet.Genus:progress.Level==3?"★★★  MAX":new string('★',progress.Level)+new string('☆',3-progress.Level)+"  "+progress.WithinLevel+"/"+progress.Goal;
            PremiumTheme.Text(g,label,new(x+50*DpiFactor,44*DpiFactor,Stride-64*DpiFactor,19*DpiFactor),9f*DpiFactor,PremiumTheme.Gold,"en");
        }
        if(Maximum>0){var width=Math.Max(30*DpiFactor,Width*(float)Width/(pets.Length*Stride));using var thumb=new SolidBrush(Color.FromArgb(135,PremiumTheme.Gold));g.FillRectangle(thumb,offset/Maximum*(Width-width),Height-3*DpiFactor,width,2*DpiFactor);}
    }
    protected override void Dispose(bool disposing){if(disposing)animation.Dispose();base.Dispose(disposing);}
}
