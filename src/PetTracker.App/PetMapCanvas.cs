using System.Diagnostics;
using System.Drawing.Drawing2D;
namespace PetTracker.App;

public sealed class PetMapCanvas : Control
{
    private Bitmap? terrain;
    private MapLocation[] markers=[];
    private IReadOnlyDictionary<int,Image> portraits=new Dictionary<int,Image>();
    private readonly Dictionary<(int Id,int Size),Bitmap> portraitMarkers=[];
    private bool showPortraits;
    private int selectedPet;
    private List<MapLocation> drawnMarkers=[];
    public event Action<int>? MarkerSelected;
    public void SetPortraits(IReadOnlyDictionary<int,Image> images)=>portraits=images;
    public void SetMarkerStyle(bool portraitStyle,int selectedId){showPortraits=portraitStyle;selectedPet=selectedId;Invalidate();}
    private readonly MapCamera camera=new();
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly System.Windows.Forms.Timer animation=new(){Interval=8};
    private readonly ToolTip tooltip=new(){AutoPopDelay=10000,InitialDelay=180,ReshowDelay=100};
    private bool dragging,moved,active=true;
    private Point last,down;
    private MapLocation? hovered;
    private double focusStart=-10;
    private string message="";
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Language { get; set; }="en";
    public double Zoom=>camera.Pose.Zoom;
    public event Action<double>? ZoomChanged;
    private double Now=>clock.Elapsed.TotalSeconds;
    private float DpiScale=>DeviceDpi/96f;
    public PetMapCanvas()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=PremiumTheme.Background;TabStop=true;
        animation.Tick+=(_,_)=>{camera.Tick(Now);ZoomChanged?.Invoke(camera.Pose.Zoom);Invalidate();if(!camera.IsAnimating&&Now-focusStart>2.8)animation.Stop();};
    }
    private void Animate(){if(active)animation.Start();Invalidate();}
    public void SetActive(bool value){active=value;if(value)Animate();else animation.Stop();}
    public void SetWorld(string path)
    {
        Bitmap next;
        try{using var image=Image.FromFile(path);next=new Bitmap(image);}
        catch{terrain?.Dispose();terrain=null;markers=[];message=MapText.Get(Language,"loadFailed");Invalidate();throw;}
        var previous=terrain;terrain=next;previous?.Dispose();
        ResizeCamera();camera.SetTarget(new(.5,.5,1),Now,0);hovered=null;tooltip.SetToolTip(this,null);Invalidate();
    }
    public void SetLocations(IReadOnlyList<MapLocation> locations,string emptyMessage)
    {markers=terrain is null?[]:locations.ToArray();message=terrain is null?MapText.Get(Language,"loadFailed"):markers.Length==0?emptyMessage:"";hovered=null;tooltip.SetToolTip(this,null);Invalidate();}
    public void FocusLocations()
        =>FocusLocations(markers);
    public void FocusLocations(IReadOnlyList<MapLocation> locations)
    {ResizeCamera();if(camera.Focus(locations.Select(marker=>marker.Point),Now)){focusStart=Now;Animate();}}
    public void SetZoom(double zoom){ResizeCamera();camera.ZoomAt(new(Width/2d,Height/2d),zoom,Now);Animate();}
    public void ChangeZoom(double factor)=>SetZoom(camera.Target.Zoom*factor);
    private void ResizeCamera(){if(terrain is not null&&Width>0&&Height>0)camera.Resize(Width,Height,terrain.Width,terrain.Height);}
    protected override void OnResize(EventArgs e){base.OnResize(e);ResizeCamera();Invalidate();}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);if(FindForm()?.ContainsFocus==true)Focus();}
    protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);ResizeCamera();camera.ZoomAt(new(e.X,e.Y),camera.Target.Zoom*Math.Pow(1.2,e.Delta/120d),Now);Animate();}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();hovered=null;tooltip.SetToolTip(this,null);dragging=true;moved=false;last=down=e.Location;Capture=true;Cursor=Cursors.SizeAll;}
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if(dragging){if(Math.Abs(e.X-down.X)+Math.Abs(e.Y-down.Y)>5*DpiScale)moved=true;camera.Pan(e.X-last.X,e.Y-last.Y,Now);last=e.Location;Animate();return;}
        var nearest=(showPortraits?drawnMarkers:markers.AsEnumerable()).Select(marker=>(Marker:marker,Screen:camera.ToScreen(marker.Point))).Where(pair=>Math.Abs(pair.Screen.X-e.X)<12*DpiScale&&Math.Abs(pair.Screen.Y-e.Y)<12*DpiScale)
            .OrderBy(pair=>Math.Pow(pair.Screen.X-e.X,2)+Math.Pow(pair.Screen.Y-e.Y,2)).Select(pair=>pair.Marker).FirstOrDefault();
        if(nearest!=hovered){hovered=nearest;tooltip.SetToolTip(this,nearest is null?null:$"{nearest.NpcName} · Lv {nearest.Level}\n{nearest.Area??""}");Invalidate();}
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);if(!dragging)return;dragging=false;Capture=false;Cursor=Cursors.Hand;
        if(!moved)
        {
            var nearest=(showPortraits?drawnMarkers:markers.AsEnumerable()).Select(marker=>(Marker:marker,Screen:camera.ToScreen(marker.Point)))
                .Where(pair=>Math.Abs(pair.Screen.X-e.X)<12*DpiScale&&Math.Abs(pair.Screen.Y-e.Y)<12*DpiScale)
                .OrderBy(pair=>Math.Pow(pair.Screen.X-e.X,2)+Math.Pow(pair.Screen.Y-e.Y,2)).Select(pair=>pair.Marker).FirstOrDefault();
            if(nearest is not null){hovered=nearest;camera.SetTarget(new(nearest.Point.X,nearest.Point.Y,Math.Max(6,camera.Target.Zoom)),Now,.5);focusStart=Now;Animate();MarkerSelected?.Invoke(nearest.PetId);}
        }
    }
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hovered=null;Invalidate();}
    protected override bool IsInputKey(Keys keyData)=>keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down||base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);switch(e.KeyCode){case Keys.Add:case Keys.Oemplus:ChangeZoom(1.25);break;case Keys.Subtract:case Keys.OemMinus:ChangeZoom(.8);break;case Keys.Left:camera.Pan(40,0,Now);break;case Keys.Right:camera.Pan(-40,0,Now);break;case Keys.Up:camera.Pan(0,40,Now);break;case Keys.Down:camera.Pan(0,-40,Now);break;default:return;}e.Handled=true;Animate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBilinear;
        if(terrain is not null)
        {
            var topLeft=camera.ToScreen(new(0,0));var bottomRight=camera.ToScreen(new(1,1));
            g.DrawImage(terrain,new RectangleF((float)topLeft.X,(float)topLeft.Y,(float)(bottomRight.X-topLeft.X),(float)(bottomRight.Y-topLeft.Y)));
            using var shade=new SolidBrush(Color.FromArgb(24,PremiumTheme.Background));g.FillRectangle(shade,ClientRectangle);
        }
        drawnMarkers.Clear();var occupied=new HashSet<(int Pet,int X,int Y)>();
        foreach(var marker in markers.OrderBy(marker=>marker.PetId==selectedPet))
        {
            var point=camera.ToScreen(marker.Point);var x=(float)point.X;var y=(float)point.Y;if(x<-20||y<-20||x>Width+20||y>Height+20)continue;
            if(showPortraits)
            {
                // Coalesce nearby locations of the same pet at this zoom, without
                // changing coordinates or combining different pet identities.
                if(!occupied.Add((marker.PetId,(int)(x/(24*DpiScale)),(int)(y/(24*DpiScale)))))continue;
                drawnMarkers.Add(marker);var pixels=Math.Max(16,(int)Math.Round(24*DpiScale));
                var icon=PortraitMarker(marker.PetId,pixels);
                g.DrawImageUnscaled(icon,(int)Math.Round(x-pixels/2f),(int)Math.Round(y-pixels/2f));
                if(marker.PetId==selectedPet||marker==hovered)
                {using var selected=new Pen(PremiumTheme.Gold,2*DpiScale);g.DrawEllipse(selected,x-pixels/2f,y-pixels/2f,pixels,pixels);}
                continue;
            }
            var radius=(marker==hovered?7f:5.5f)*DpiScale;
            if(Now-focusStart<2.8){var pulse=(float)(((Now-focusStart)*.8)%1);using var ripple=new Pen(Color.FromArgb((int)(70*(1-pulse)),Color.FromArgb(239,49,53)),1.5f*DpiScale);var r=(9+14*pulse)*DpiScale;g.DrawEllipse(ripple,x-r,y-r,r*2,r*2);}
            using var glow=new SolidBrush(Color.FromArgb(marker==hovered?80:36,Color.FromArgb(239,49,53)));g.FillEllipse(glow,x-radius-3*DpiScale,y-radius-3*DpiScale,2*(radius+3*DpiScale),2*(radius+3*DpiScale));
            using var fill=new SolidBrush(marker==hovered?Color.FromArgb(255,78,75):Color.FromArgb(239,49,53));g.FillEllipse(fill,x-radius,y-radius,radius*2,radius*2);
            using var border=new Pen(Color.Black,2*DpiScale);g.DrawEllipse(border,x-radius,y-radius,radius*2,radius*2);
        }
        if(message.Length>0)
        {
            var box=new RectangleF(Width*.15f,Height*.38f,Width*.7f,Height*.24f);using var path=PremiumTheme.Round(box,10*DpiScale);using var veil=new SolidBrush(Color.FromArgb(225,PremiumTheme.Background));g.FillPath(veil,path);
            PremiumTheme.Text(g,message,box,12*DpiScale,PremiumTheme.Gold,Language,align:StringAlignment.Center);
        }
        using var edge=new Pen(Color.FromArgb(95,PremiumTheme.Gold),DpiScale);g.DrawRectangle(edge,0,0,Width-1,Height-1);
    }
    private Bitmap PortraitMarker(int id,int size)
    {
        if(portraitMarkers.TryGetValue((id,size),out var cached))return cached;
        var result=new Bitmap(size+2,size+2);using(var g=Graphics.FromImage(result))
        {
            g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            using var bg=new SolidBrush(PremiumTheme.Panel);g.FillEllipse(bg,1,1,size-1,size-1);
            using var clip=new GraphicsPath();clip.AddEllipse(2,2,size-3,size-3);g.SetClip(clip);
            if(portraits.TryGetValue(id,out var image))g.DrawImage(image,2,2,size-3,size-3);
            else {using var fill=new SolidBrush(Color.FromArgb(239,49,53));g.FillEllipse(fill,2,2,size-3,size-3);}
            g.ResetClip();using var edge=new Pen(Color.Black,2);g.DrawEllipse(edge,1,1,size-1,size-1);
        }
        portraitMarkers[(id,size)]=result;return result;
    }
    protected override void OnDpiChangedAfterParent(EventArgs e){base.OnDpiChangedAfterParent(e);foreach(var image in portraitMarkers.Values)image.Dispose();portraitMarkers.Clear();Invalidate();}
    protected override void Dispose(bool disposing){if(disposing){animation.Dispose();tooltip.Dispose();terrain?.Dispose();foreach(var image in portraitMarkers.Values)image.Dispose();portraitMarkers.Clear();}base.Dispose(disposing);}
}
