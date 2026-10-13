using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PetTracker.App;

public sealed class PetMapForm : Form
{
    private readonly OverlayController controller;
    private readonly OverlayPreferences preferences;
    private readonly Action save;
    private readonly MapData data;
    private readonly string mapFolder;
    private readonly PetMapCanvas canvas=new();
    private readonly PetMapCarousel carousel;
    private readonly CrystalCursor pointer=new();
    private readonly System.Windows.Forms.Timer refresh=new(){Interval=500};
    private PremiumButton regions=null!;
    private Panel worldMenu=null!;
    private TextBox search=null!;
    private Panel searchFrame=null!;
    private PremiumButton farmable=null!,maxedButton=null!,previous=null!,next=null!,minus=null!,plus=null!,focus=null!,close=null!,minimize=null!;
    private PremiumSlider zoom=null!;
    private Label zoomLabel=null!,selectionLabel=null!,status=null!;
    private string language;
    private bool maxed,updatingZoom;
    private int worldId,selectedId;
    private PetDefinition[] matches=[];
    private string lastFingerprint="";
    private float DpiFactor=>DeviceDpi/96f;
    private string M(string key,params object[] args)=>MapText.Get(language,key,args);
    private Rectangle Scaled(RectangleF r)=>new((int)Math.Round(r.X*DpiFactor),(int)Math.Round(r.Y*DpiFactor),(int)Math.Round(r.Width*DpiFactor),(int)Math.Round(r.Height*DpiFactor));

    public PetMapForm(OverlayController controller,IReadOnlyDictionary<int,Image> images,OverlayPreferences preferences,Action save)
    {
        this.controller=controller;this.preferences=preferences;this.save=save;language=preferences.Language;
        mapFolder=Path.Combine(AppContext.BaseDirectory,"data","maps");data=MapData.Load(Path.Combine(mapFolder,"spawns.json"));
        worldId=data.Worlds.ContainsKey(preferences.MapWorldId)?preferences.MapWorldId:1010;
        if(!data.Worlds.ContainsKey(worldId))worldId=data.Worlds.Keys.First();
        AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;DoubleBuffered=true;StartPosition=FormStartPosition.Manual;
        BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;ShowInTaskbar=true;TopMost=preferences.AlwaysOnTop;Cursor=pointer.Pointer;Icon=AppBrand.CreateIcon();
        ClientSize=new((int)(preferences.MapWidth*DpiFactor),(int)(preferences.MapHeight*DpiFactor));MinimumSize=new((int)(760*DpiFactor),(int)(550*DpiFactor));
        var area=Screen.PrimaryScreen!.WorkingArea;
        Location=new(preferences.MapLeft??area.Left+(area.Width-Width)/2,preferences.MapTop??area.Top+(area.Height-Height)/2);EnsureVisible();
        carousel=new(images,id=>controller.Tracker.HasCurrent?controller.Tracker.KnownTotal(id):null){Cursor=pointer.Pointer};
        carousel.PetSelected+=id=>Try(()=>FocusPet(id));canvas.ZoomChanged+=OnZoomChanged;
        Controls.AddRange([canvas,carousel]);BuildChrome();LayoutChrome();LoadWorld();UpdateSelection(false);
        refresh.Tick+=(_,_)=>RefreshProgress();refresh.Start();
        FormClosed+=(_,_)=>
        {
            refresh.Stop();var bounds=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;
            preferences.MapWidth=(int)Math.Round(bounds.Width/DpiFactor);preferences.MapHeight=(int)Math.Round(bounds.Height/DpiFactor);preferences.MapLeft=bounds.Left;preferences.MapTop=bounds.Top;preferences.MapWorldId=worldId;
            try{save();}catch(Exception ex){controller.Log("Harita ayarları kaydedilemedi: "+ex.Message);}
        };
        PremiumDialogMotion.Attach(this);
    }
    private long? Total(int id)=>controller.Tracker.HasCurrent?controller.Tracker.KnownTotal(id):null;
    private PremiumButton Button(string text,Action action)
    {var result=new PremiumButton{Text=text,TextLanguage=language,DpiAwareText=true,TextSize=10,Cursor=pointer.Pointer,AccessibleName=text};result.Click+=(_,_)=>Try(action);Controls.Add(result);return result;}
    private Label Label(string text,float size=9)
    {var result=new PremiumLabel{Text=text,TextLanguage=language,Font=new(UiText.FontFamily(language),size),BackColor=PremiumTheme.Background,ForeColor=PremiumTheme.Muted,AutoSize=false};Controls.Add(result);return result;}
    private void BuildChrome()
    {
        foreach(var control in Controls.Cast<Control>().Where(control=>control!=canvas&&control!=carousel).ToArray()){Controls.Remove(control);control.Dispose();}
        Text="Aion 2 Pet Tracker · "+M("title");carousel.Language=language;canvas.Language=language;
        farmable=Button(M("farmable"),()=>SelectTab(false));maxedButton=Button(UiText.Get(language,"maxedPets"),()=>SelectTab(true));
        regions=Button(data.Worlds[worldId].Name+"  ▾",()=>{worldMenu.Visible=!worldMenu.Visible;if(worldMenu.Visible)worldMenu.BringToFront();});
        regions.AccessibleName=M("region");
        worldMenu=new Panel{BackColor=PremiumTheme.Panel,Visible=false};Controls.Add(worldMenu);
        foreach(var world in data.Worlds.Values.OrderBy(world=>world.Id))
        {
            var choice=new PremiumButton{Text=world.Name,TextLanguage=language,DpiAwareText=true,TextSize=10,Cursor=pointer.Pointer};
            choice.Click+=(_,_)=>Try(()=>{worldMenu.Hide();worldId=world.Id;regions.Text=world.Name+"  ▾";LoadWorld();UpdateSelection(true);});
            worldMenu.Controls.Add(choice);
        }
        searchFrame=new(){BackColor=PremiumTheme.Panel};search=new(){BorderStyle=BorderStyle.None,BackColor=PremiumTheme.Panel,ForeColor=PremiumTheme.Ink,Font=new(UiText.FontFamily(language),10),PlaceholderText=UiText.Get(language,"search"),Cursor=Cursors.IBeam,RightToLeft=language=="ar"?RightToLeft.Yes:RightToLeft.No};
        search.TextChanged+=(_,_)=>Filter();searchFrame.Controls.Add(search);Controls.Add(searchFrame);
        previous=Button("‹",()=>carousel.ScrollBy(-1));next=Button("›",()=>carousel.ScrollBy(1));
        minus=Button("−",()=>canvas.ChangeZoom(.8));plus=Button("+",()=>canvas.ChangeZoom(1.25));focus=Button(M("focus"),()=>canvas.FocusLocations());
        zoom=new(){Minimum=0,Maximum=100,Value=0,Cursor=pointer.Pointer,AccessibleName=M("zoom")};zoom.ValueChanged+=(_,_)=>{if(!updatingZoom)canvas.SetZoom(Math.Pow(MapCamera.MaximumZoom,zoom.Value/100d));};Controls.Add(zoom);
        zoomLabel=Label("1.0×",10);selectionLabel=Label(M("select"),10);selectionLabel.ForeColor=PremiumTheme.Gold;status=Label(M("static"),8);
        minimize=Button("−",()=>WindowState=FormWindowState.Minimized);close=Button("×",()=>Close());
        Filter();OnZoomChanged(canvas.Zoom);LayoutChrome();worldMenu.BringToFront();
    }
    private void SelectTab(bool value)
    {
        maxed=value;Filter();
        if(!matches.Any(pet=>pet.Id==selectedId)){selectedId=0;carousel.SetPets(matches,selectedId);UpdateSelection(false);}
    }
    private void Filter()
    {
        matches=PetPickerQuery.Filter(controller.Catalog,search.Text,maxed,Total);carousel.SetPets(matches,selectedId);
        farmable.ForeColor=maxed?PremiumTheme.Muted:PremiumTheme.Gold;maxedButton.ForeColor=maxed?PremiumTheme.Gold:PremiumTheme.Muted;farmable.Invalidate();maxedButton.Invalidate();Invalidate();
    }
    public void FocusPet(int petId)
    {
        selectedId=petId;maxed=Total(petId) is >=105;search.Text="";
        if(data.BestWorld(petId,worldId) is { } best&&worldId!=best)
        {worldId=best;worldMenu.Hide();regions.Text=data.Worlds[worldId].Name+"  ▾";LoadWorld();}
        Filter();UpdateSelection(true);carousel.EnsureSelectedVisible();
    }
    private void LoadWorld()
    {canvas.SetWorld(Path.Combine(mapFolder,data.Worlds[worldId].ImageKey+".png"));preferences.MapWorldId=worldId;}
    private void UpdateSelection(bool animate)
    {
        var locations=selectedId>0?data.Locations(selectedId,worldId):[];
        var empty=selectedId<=0?M("select"):data.WorldsForPet(selectedId).Count==0?M("noLocations"):M("noRegionLocations");
        canvas.SetLocations(locations,empty);
        selectionLabel.Text=selectedId<=0?M("select"):controller.Catalog.Name(selectedId)+"  ·  "+M("locations",locations.Count);
        focus.Enabled=locations.Count>0;focus.ForeColor=focus.Enabled?PremiumTheme.Gold:PremiumTheme.Muted;
        if(animate)canvas.FocusLocations();RefreshStatus();
    }
    private void RefreshStatus()
    {
        var quality=controller.Tracker.Quality==DataQuality.Exact?"":UiText.Get(language,"last")+" · ";
        status.Text=quality+M("static")+" · "+data.SnapshotDate.ToString("yyyy-MM-dd");
    }
    public void SetLanguage(string code)
    {
        if(language==code)return;language=code;var query=search.Text;BuildChrome();search.Text=query;UpdateSelection(false);Invalidate();
    }
    private void RefreshProgress()
    {
        if(preferences.Language!=language)SetLanguage(preferences.Language);
        if(TopMost!=preferences.AlwaysOnTop)TopMost=preferences.AlwaysOnTop;
        var fingerprint=(controller.Tracker.HasCurrent?controller.Tracker.Current.Scope.Key:"")+"|"+string.Join(",",controller.Catalog.Pets.Keys.Where(id=>Total(id) is >=105));
        if(lastFingerprint!=fingerprint)
        {
            lastFingerprint=fingerprint;
            if(selectedId>0&&matches.Any(pet=>pet.Id==selectedId))maxed=Total(selectedId) is >=105;
            Filter();
        }
        carousel.Invalidate();RefreshStatus();
    }
    private void OnZoomChanged(double value)
    {
        if(zoom is null||zoom.IsDisposed)return;
        if(!zoom.Capture){updatingZoom=true;zoom.Value=(int)Math.Round(Math.Log(Math.Max(1,value))/Math.Log(MapCamera.MaximumZoom)*100);updatingZoom=false;}
        zoomLabel.Text=value.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"×";
    }
    private void LayoutChrome()
    {
        if(regions is null)return;var width=ClientSize.Width/DpiFactor;var height=ClientSize.Height/DpiFactor;
        farmable.Bounds=Scaled(new(22,70,190,32));maxedButton.Bounds=Scaled(new(222,70,190,32));
        regions.Bounds=Scaled(new(432,70,245,32));worldMenu.Bounds=Scaled(new(432,104,245,224));
        foreach(var pair in worldMenu.Controls.Cast<Control>().Select((control,index)=>(control,index)))pair.control.Bounds=Scaled(new(4,4+pair.index*36,237,32));
        searchFrame.Bounds=Scaled(new(22,114,width-44,35));search.Bounds=Scaled(new(12,7,width-68,24));
        previous.Bounds=Scaled(new(22,160,26,74));next.Bounds=Scaled(new(width-48,160,26,74));carousel.Bounds=Scaled(new(57,157,width-114,82));
        selectionLabel.Bounds=Scaled(new(23,247,width-46,27));canvas.Bounds=Scaled(new(22,283,width-44,Math.Max(100,height-373)));
        var footer=height-73;minus.Bounds=Scaled(new(width-365,footer,29,29));zoom.Bounds=Scaled(new(width-325,footer,185,29));plus.Bounds=Scaled(new(width-127,footer,29,29));zoomLabel.Bounds=Scaled(new(width-86,footer+4,64,24));
        focus.Bounds=Scaled(new(22,footer,210,29));status.Bounds=Scaled(new(23,height-30,width-46,20));
        minimize.Bounds=Scaled(new(width-85,17,27,27));close.Bounds=Scaled(new(width-49,17,27,27));Invalidate();
    }
    private void EnsureVisible()
    {
        var screen=Screen.AllScreens.FirstOrDefault(screen=>screen.WorkingArea.IntersectsWith(Bounds))??Screen.PrimaryScreen!;var area=screen.WorkingArea;
        Width=Math.Min(Width,area.Width);Height=Math.Min(Height,area.Height);Location=new(Math.Clamp(Left,area.Left,Math.Max(area.Left,area.Right-Width)),Math.Clamp(Top,area.Top,Math.Max(area.Top,area.Bottom-Height)));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.ScaleTransform(DpiFactor,DpiFactor);g.SmoothingMode=SmoothingMode.AntiAlias;var width=ClientSize.Width/DpiFactor;var height=ClientSize.Height/DpiFactor;
        using var background=new LinearGradientBrush(new RectangleF(0,0,width,60),Color.FromArgb(29,24,36),PremiumTheme.Background,0f);g.FillRectangle(background,0,0,width,60);
        using var line=new Pen(Color.FromArgb(90,PremiumTheme.Gold));g.DrawRectangle(line,1,1,width-2,height-2);g.DrawLine(line,22,60,width-22,60);
        using var emblem=AppBrand.Draw(36);g.DrawImage(emblem,22,13,32,32);
        PremiumTheme.Text(g,M("title"),new(66,11,width-190,35),18,PremiumTheme.Gold,language,FontStyle.Bold);
        PremiumTheme.Text(g,M("hint"),new(248,height-76,width-635,34),7.5f,PremiumTheme.Muted,language);
        var tab=maxed?maxedButton:farmable;if(tab is not null){using var selected=new Pen(PremiumTheme.Gold,2);g.DrawLine(selected,tab.Left/DpiFactor+12,tab.Bottom/DpiFactor+3,tab.Right/DpiFactor-12,tab.Bottom/DpiFactor+3);}
        using var grip=new Pen(PremiumTheme.Gold);g.DrawLine(grip,width-12,height-4,width-4,height-12);g.DrawLine(grip,width-18,height-4,width-4,height-18);
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);LayoutChrome();canvas.SetActive(WindowState!=FormWindowState.Minimized);}
    protected override void OnDpiChanged(DpiChangedEventArgs e){base.OnDpiChanged(e);MinimumSize=new((int)(760*DpiFactor),(int)(550*DpiFactor));LayoutChrome();EnsureVisible();}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);worldMenu?.Hide();if(e.Button==MouseButtons.Left&&e.Y<60*DpiFactor){ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}}
    protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==Keys.Escape){if(worldMenu.Visible)worldMenu.Hide();else Close();return true;}return base.ProcessCmdKey(ref message,key);}
    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);if(message.Msg!=0x84||message.Result.ToInt32()!=1)return;
        var packed=message.LParam.ToInt64();var point=PointToClient(new(unchecked((short)(packed&0xffff)),unchecked((short)((packed>>16)&0xffff))));
        var edge=(int)(7*DpiFactor);var left=point.X<edge;var right=point.X>=Width-edge;var top=point.Y<edge;var bottom=point.Y>=Height-edge;
        message.Result=(IntPtr)(top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:1);
    }
    private void Try(Action action){try{action();}catch(Exception ex){controller.Log("Harita hatası: "+ex.Message);MessageBox.Show(this,ex.Message,M("title"),MessageBoxButtons.OK,MessageBoxIcon.Error);}}
    protected override void Dispose(bool disposing){if(disposing){refresh.Dispose();pointer.Dispose();Icon?.Dispose();}base.Dispose(disposing);}
    [DllImport("user32.dll")]private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
}
