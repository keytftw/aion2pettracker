using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using PetTracker.Core;

namespace PetTracker.App;

public sealed class OverlayForm : Form
{
    public const int FixedWidth=427; private const int DesignWidth=320;
    private const int HeaderHeight=80,FooterHeight=42,CardHeight=65,CardGap=6;
    private readonly OverlayController controller;
    private readonly OverlayPreferences preferences;
    private readonly string preferencesPath;
    private readonly Dictionary<int,Image> portraits=[];
    private readonly Dictionary<(int Id,int Size),Bitmap> cardPortraits=[];
    private readonly List<string> drawnButtons=[];
    private readonly List<(RectangleF Bounds,string Action,int Id)> hits=[];
    private readonly Dictionary<int,float> hover=[];
    private readonly Dictionary<string,float> buttonHover=[];
    private readonly Dictionary<int,float> progressBars=[];
    private readonly CardMotion motion=new(CardHeight+CardGap);
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly System.Windows.Forms.Timer timer=new(){Interval=8};
    private readonly System.Windows.Forms.Timer appearanceSave=new(){Interval=250};
    private readonly AnimationClock animationClock=new();
    private double frameElapsed=1d/60;
    private bool progressAnimating;
    private int[] visibleIds=[];
    private readonly CrystalCursor crystal=new(),brightCrystal=new(true);
    private readonly MaxChime chime=new();
    private readonly ServerNames serverNames=new(Path.Combine(AppContext.BaseDirectory,"data","servers.json"));
    private readonly NotifyIcon tray;
    private readonly ToolTip tips=new();
    private HandbookForm? handbook;
    private string? handbookLanguage;
    private float scroll,scrollTarget;
    private int wheelDelta;
    private HashSet<int> lastVisible=[];
    private double lastPump;
    private double revealStart;private bool revealing;
    private int hoveredCard=-1;
    private string hovered="";
    private bool closing,allowClose,busy,dragScroll;
    private bool compactMode;
    private int registeredHideKey,registeredHideModifiers,registeredCompactKey,registeredCompactModifiers;
    private bool hideHotkeyRegistered,compactHotkeyRegistered;
    private readonly bool preview;
    private PetMapForm? map;
    private readonly RiftAlertService rift;
    private float ScaleFactor=>DeviceDpi/96f*4f/3f;
    private float LogicalHeight=>ClientSize.Height/ScaleFactor;
    private double Now=>clock.Elapsed.TotalSeconds;
    private string Language=>preferences.Language;
    private string T(string key,params object[] args)=>UiText.Get(Language,key,args);
    public OverlayController Controller=>controller;
    public bool PositionLocked=>preferences.Locked;

    public OverlayForm(bool liveMode=true,string? stateFolder=null,bool preview=false,OverlayController? sharedController=null,string? selectedLanguage=null)
    {
        this.preview=preview;controller=sharedController??new(liveMode,stateFolder);
        preferencesPath=Path.Combine(controller.Folder,"overlay-preferences.json");preferences=OverlayPreferences.Load(preferencesPath,controller.Log);
        if(selectedLanguage is not null)preferences.Language=selectedLanguage;
        registeredHideKey=preferences.HideHotkeyKey;registeredHideModifiers=preferences.HideHotkeyModifiers;
        registeredCompactKey=preferences.CompactHotkeyKey;registeredCompactModifiers=preferences.CompactHotkeyModifiers;
        AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;DoubleBuffered=true;
        BackColor=PremiumTheme.Background;Text="Aion 2 Pet Tracker";Font=new(UiText.FontFamily(preferences.Language),9);ShowInTaskbar=true;StartPosition=FormStartPosition.Manual;
        Icon=AppBrand.CreateIcon();Cursor=crystal.Pointer;ApplyAppearance();
        var area=Screen.PrimaryScreen!.WorkingArea;
        Location=new(preferences.Left??area.Right-Width-24,preferences.Top??area.Top+60);EnsureVisible();
        foreach(var pet in controller.Catalog.Pets.Values)
        {
            var path=Path.Combine(AppContext.BaseDirectory,"data","pets",pet.Id+".png");
            if(!File.Exists(path))continue;
            try {using var original=Image.FromFile(path);portraits[pet.Id]=new Bitmap(original);}
            catch(Exception ex){controller.Log($"Portre {pet.Id} okunamadı: {ex.Message}");}
        }
        tray=new(){Icon=Icon,Text="Aion 2 Pet Tracker",Visible=!preview};RefreshTray();tray.DoubleClick+=(_,_)=>Restore();
        controller.Tracker.ReachedMax+=PlayMaxChime;
        if(preview)controller.LoadPreview();SyncCards();
        timer.Tick+=(_,_)=>
        {
            var now=Now;if(revealing){var t=Math.Clamp((now-revealStart)/.38,0,1);Opacity=Math.Max(.01,preferences.OpacityPercent/100d*(1-Math.Pow(1-t,3)));if(t>=1)revealing=false;}
            if(!preview&&now-lastPump>=.05){lastPump=now;try{if(controller.Pump())SyncCards();}catch(Exception ex){controller.Log("HATA: "+ex.Message);}}
            frameElapsed=animationClock.Step();motion.Tick(now);
            scroll=FrameSmoothing.Approach(scroll,scrollTarget,frameElapsed,22,.05f);
            var animating=revealing||motion.IsAnimating||scroll!=scrollTarget||progressAnimating;
            foreach(var row in motion.Rows){var target=hoveredCard==row.Id?1:0;hover[row.Id]=FrameSmoothing.Approach(hover.GetValueOrDefault(row.Id),target,frameElapsed,14);animating|=hover[row.Id]!=target;}
            foreach(var key in drawnButtons){var target=hovered==key?1:0;buttonHover[key]=FrameSmoothing.Approach(buttonHover.GetValueOrDefault(key),target,frameElapsed,16);animating|=buttonHover[key]!=target;}
            var visible=Visible&&WindowState!=FormWindowState.Minimized;
            timer.Interval=visible?(animating?8:32):50;
            if(visible)Invalidate();
        };
        appearanceSave.Tick+=(_,_)=>{appearanceSave.Stop();try{SaveAppearance();}catch(Exception ex){Error(ex);}};
        rift=new(controller,preferences,SaveAppearance,!preview);
        timer.Start();
        FormClosing+=async(_,e)=>
        {
            if(allowClose)return;e.Cancel=true;if(closing)return;closing=true;
            try{timer.Stop();appearanceSave.Stop();map?.Close();await controller.DisposeAsync();SaveAppearance();allowClose=true;BeginInvoke(()=>Close());}
            catch(Exception ex){closing=false;timer.Start();Error(ex);}
        };
    }
    protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);ApplyGlobalHotkeys();}
    protected override void OnHandleDestroyed(EventArgs e){UnregisterGlobalHotkeys();base.OnHandleDestroyed(e);}
    private void UnregisterGlobalHotkeys()
    {
        if(!IsHandleCreated)return;
        if(hideHotkeyRegistered)UnregisterHotKey(Handle,0xA201);
        if(compactHotkeyRegistered)UnregisterHotKey(Handle,0xA202);
        hideHotkeyRegistered=compactHotkeyRegistered=false;
    }
    private bool ApplyGlobalHotkeys()
    {
        if(preview)return true;
        if(!IsHandleCreated)return true;
        UnregisterGlobalHotkeys();
        hideHotkeyRegistered=RegisterHotKey(Handle,0xA201,((uint)preferences.HideHotkeyModifiers|0x4000),(uint)preferences.HideHotkeyKey);
        compactHotkeyRegistered=RegisterHotKey(Handle,0xA202,((uint)preferences.CompactHotkeyModifiers|0x4000),(uint)preferences.CompactHotkeyKey);
        if(hideHotkeyRegistered&&compactHotkeyRegistered)
        {
            registeredHideKey=preferences.HideHotkeyKey;registeredHideModifiers=preferences.HideHotkeyModifiers;
            registeredCompactKey=preferences.CompactHotkeyKey;registeredCompactModifiers=preferences.CompactHotkeyModifiers;
            return true;
        }
        UnregisterGlobalHotkeys();
        hideHotkeyRegistered=RegisterHotKey(Handle,0xA201,((uint)registeredHideModifiers|0x4000),(uint)registeredHideKey);
        compactHotkeyRegistered=RegisterHotKey(Handle,0xA202,((uint)registeredCompactModifiers|0x4000),(uint)registeredCompactKey);
        preferences.HideHotkeyKey=registeredHideKey;preferences.HideHotkeyModifiers=registeredHideModifiers;
        preferences.CompactHotkeyKey=registeredCompactKey;preferences.CompactHotkeyModifiers=registeredCompactModifiers;
        controller.Log("Klavye kısayolu Windows tarafından kullanımda; önceki kısayollar korundu.");
        return false;
    }
    private void ToggleVisibility(){if(Visible)Hide();else Restore();}
    private void ApplyCompactSize()
    {
        MinimumSize=Size.Empty;MaximumSize=Size.Empty;
        ClientSize=new((int)Math.Round(190*ScaleFactor),(int)Math.Round(43*ScaleFactor));
        Opacity=preferences.OpacityPercent/100d;TopMost=preferences.AlwaysOnTop;
    }
    private void ToggleCompact()
    {
        compactMode=!compactMode;
        hits.Clear();
        if(compactMode)ApplyCompactSize();
        else ApplyAppearance();
        EnsureVisible();Invalidate();
    }
    private void PlayMaxChime(int id){try{chime.Play();}catch(Exception ex){controller.Log("Ses çalınamadı: "+ex.Message);}}
    private void RefreshTray()
    {
        var menu=new ContextMenuStrip {Cursor=crystal.Pointer};
        menu.Items.Add(T("show"),null,(_,_)=>Restore());menu.Items.Add(T(preferences.Locked?"unlock":"lock"),null,(_,_)=>ToggleLock());
        menu.Items.Add(T("settings"),null,(_,_)=>{Restore();ShowSettings();});menu.Items.Add(T("close"),null,(_,_)=>Close());
        var old=tray.ContextMenuStrip;tray.ContextMenuStrip=menu;old?.Dispose();
    }
    private void ApplyAppearance()
    {
        preferences.Validate();
        if(compactMode){ApplyCompactSize();return;}
        var width=(int)Math.Round(FixedWidth*DeviceDpi/96f);
        MinimumSize=new(width,(int)Math.Round(OverlayHeight.Pixels(1)*DeviceDpi/96f));MaximumSize=new(width,(int)Math.Round(OverlayHeight.Pixels(7)*DeviceDpi/96f));
        Width=width;Height=(int)(preferences.Height*DeviceDpi/96f);Opacity=preferences.OpacityPercent/100d;TopMost=preferences.AlwaysOnTop;
    }
    private void EnsureVisible()
    {
        var screen=Screen.AllScreens.FirstOrDefault(s=>s.WorkingArea.IntersectsWith(Bounds))??Screen.PrimaryScreen!;var area=screen.WorkingArea;
        Height=Math.Min(Height,area.Height);Location=new(Math.Clamp(Left,area.Left,Math.Max(area.Left,area.Right-Width)),Math.Clamp(Top,area.Top,Math.Max(area.Top,area.Bottom-Height)));
    }
    private void SaveAppearance()
    {
        if(preview)return;var bounds=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;
        preferences.Left=bounds.Left;preferences.Top=bounds.Top;preferences.LayoutVersion=4;preferences.Save(preferencesPath);
    }
    private void QueueAppearanceSave(){appearanceSave.Stop();appearanceSave.Start();}
    private bool ValidateSettingsHotkeys(){var result=ApplyGlobalHotkeys();UnregisterGlobalHotkeys();return result;}
    private void Restore(){Show();WindowState=FormWindowState.Normal;TopMost=preferences.AlwaysOnTop;Activate();}
    internal void RestoreFromShortcut()
    {
        Restore();BringToFront();
        var dialog=OwnedForms.LastOrDefault(form=>form.Visible&&!form.IsDisposed);
        if(dialog is not null){dialog.BringToFront();dialog.Activate();}
    }
    private void ToggleLock(){preferences.Locked=!preferences.Locked;SaveAppearance();RefreshTray();Invalidate();}
    protected override void WndProc(ref Message m)
    {
        if(m.Msg==0x0312&&preferences is not null)
        {
            if(m.WParam.ToInt32()==0xA201)ToggleVisibility();
            else if(m.WParam.ToInt32()==0xA202){if(!Visible)Restore();ToggleCompact();}
            return;
        }
        if(m.Msg==0x112&&compactMode&&(m.WParam.ToInt64()&0xFFF0)==0xF000)return;
        if(m.Msg==0x112&&preferences is not null&&preferences.Locked&&(m.WParam.ToInt64()&0xFFF0)==0xF010)return;
        if(m.Msg==0x20&&crystal is not null){var pointer=PointToClient(MousePosition);Cursor.Current=pointer.Y>=ClientSize.Height-18&&!compactMode?Cursors.SizeNS:hovered.Length>0?brightCrystal.Pointer:crystal.Pointer;m.Result=(IntPtr)1;return;}
        base.WndProc(ref m);
        if(m.Msg==0x84&&m.Result.ToInt32()==1&&!compactMode)
        {
            var packed=m.LParam.ToInt64();var point=PointToClient(new(unchecked((short)(packed&0xffff)),unchecked((short)((packed>>16)&0xffff))));
            if(point.Y>=ClientSize.Height-18)m.Result=(IntPtr)15;
        }
        if(m.Msg==0x232&&preferences is not null){try{if(!compactMode){preferences.VisibleCards=OverlayHeight.CardsFromPixels((int)Math.Round(Height/(DeviceDpi/96f)));ApplyAppearance();}EnsureVisible();SaveAppearance();}catch(Exception ex){Error(ex);}}
    }
    protected override void OnDpiChanged(DpiChangedEventArgs e){base.OnDpiChanged(e);if(compactMode)ApplyCompactSize();else ApplyAppearance();EnsureVisible();}
    protected override void OnShown(EventArgs e)
    {if(!preview){revealStart=Now;revealing=true;Opacity=.01;}base.OnShown(e);}
    protected override void OnResize(EventArgs e){base.OnResize(e);Invalidate();}
    private int[] Ids()=>visibleIds;
    private float Viewport=>Math.Max(1,LogicalHeight-HeaderHeight-FooterHeight-4);
    private int VisibleCardSlots=>Math.Max(1,(int)Math.Floor((Viewport+.5f)/(CardHeight+CardGap)));
    private float MaximumScroll=>CardScroll.Maximum(Ids().Length,VisibleCardSlots,CardHeight+CardGap);
    private float SnapScroll(float offset)=>CardScroll.Snap(offset,Ids().Length,VisibleCardSlots,CardHeight+CardGap);
    private void SyncCards()
    {
        var ids=controller.Tracker.HasCurrent?controller.Tracker.VisibleIds.ToArray():[];
        var orderChanged=!visibleIds.SequenceEqual(ids);
        visibleIds=ids;
        if(ids.Any(id=>!lastVisible.Contains(id)))
        {
            var slots=OverlayHeight.Grow(preferences.VisibleCards,ids.Length);
            if(slots!=preferences.VisibleCards){preferences.VisibleCards=slots;ApplyAppearance();EnsureVisible();SaveAppearance();}
        }
        if(orderChanged){lastVisible=ids.ToHashSet();motion.Sync(ids,Now);timer.Interval=8;}
        scrollTarget=SnapScroll(scrollTarget);
    }
    protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);wheelDelta+=e.Delta;var steps=wheelDelta/120;wheelDelta%=120;scrollTarget=SnapScroll(scrollTarget-steps*(CardHeight+CardGap));timer.Interval=8;}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.ScaleTransform(ScaleFactor,ScaleFactor);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        hits.Clear();drawnButtons.Clear();progressAnimating=false;if(compactMode)
        {
            var box=new RectangleF(0,0,190,43);using var fill=new LinearGradientBrush(box,Color.FromArgb(34,29,40),PremiumTheme.Background,0f);g.FillRectangle(fill,box);
            using var border=new Pen(Color.FromArgb(150,PremiumTheme.Gold),1.2f);g.DrawRectangle(border,.6f,.6f,188.8f,41.8f);
            DrawText(g,"Pet Tracker",new(13,4,128,34),12,PremiumTheme.Gold,FontStyle.Bold,language:"en");
            var button=new RectangleF(151,9,27,25);using var buttonShape=PremiumTheme.Round(button,6);using var buttonFill=new SolidBrush(Color.FromArgb(35,PremiumTheme.Gold));g.FillPath(buttonFill,buttonShape);g.DrawPath(border,buttonShape);
            using var arrow=new Pen(PremiumTheme.Ink,1.5f);g.DrawLine(arrow,158,23,170,23);g.DrawLine(arrow,170,23,166,19);g.DrawLine(arrow,170,23,166,27);hits.Add((button,"expand",0));return;
        }
        using var bg=new LinearGradientBrush(new RectangleF(0,0,DesignWidth,LogicalHeight),Color.FromArgb(20,20,28),Color.FromArgb(10,14,20),90f);g.FillRectangle(bg,0,0,DesignWidth,LogicalHeight);
        using var edge=new Pen(Color.FromArgb(90,PremiumTheme.Gold));g.DrawRectangle(edge,.5f,.5f,DesignWidth-1,LogicalHeight-1);
        DrawCrest(g,new(10,12,20,23));
        DrawText(g,"Aion 2 Pet Tracker",new(38,11,178,22),10.4f,PremiumTheme.Gold,FontStyle.Bold);
        var identity=controller.Tracker.HasCurrent?controller.Tracker.Current.Scope.Character+" · "+serverNames.Name(controller.Tracker.Current.Scope.Server,Language):T("character");
        DrawText(g,"SOUL COLLECTION",new(39,32,180,15),6.8f,PremiumTheme.Muted,language:"en");
        IconButton(g,new(220,13,16,22),"lock",preferences.Locked);IconButton(g,new(239,13,16,22),"compact");IconButton(g,new(258,13,16,22),"settings");IconButton(g,new(277,13,16,22),"minimize");IconButton(g,new(296,13,16,22),"close");
        DrawText(g,identity,new(12,52,168,24),8.8f,PremiumTheme.Gold,FontStyle.Bold,language:"en");
        IconButton(g,new(184,53,18,21),"handbook");IconButton(g,new(204,53,18,21),"map");
        ActionButton(g,new(226,54,84,20),"+ "+T("add"),"add");
        
        using var line=new Pen(Color.FromArgb(48,PremiumTheme.Gold));g.DrawLine(line,10,78,DesignWidth-10,78);
        var saved=g.Save();g.SetClip(new RectangleF(6,HeaderHeight,DesignWidth-12,Math.Max(1,LogicalHeight-HeaderHeight-FooterHeight)));
        if(motion.Rows.Count==0)
        {
            DrawText(g,T("empty"),new(19,HeaderHeight+43,DesignWidth-38,30),10,PremiumTheme.Ink,FontStyle.Bold,StringAlignment.Center);
            DrawText(g,T("emptyHint"),new(20,HeaderHeight+78,DesignWidth-40,36),8,PremiumTheme.Muted,align:StringAlignment.Center);
        }
        foreach(var row in motion.Rows.OrderBy(r=>r.Y))
        {
            var y=HeaderHeight+4+row.Y-scroll;if(y+CardHeight<HeaderHeight||y>LogicalHeight-FooterHeight)continue;
            DrawCard(g,row,new(10+row.X,y,DesignWidth-20,CardHeight));
        }
        g.Restore(saved);
        if(MaximumScroll>0)
        {
            var h=Math.Max(18,Viewport*Viewport/(Ids().Length*(CardHeight+CardGap)));var y=HeaderHeight+scroll/MaximumScroll*(Viewport-h);
            using var thumb=new SolidBrush(Color.FromArgb(120,PremiumTheme.Gold));g.FillRectangle(thumb,DesignWidth-5,y,2,h);
        }
        var footer=LogicalHeight-FooterHeight;using var footerBrush=new SolidBrush(PremiumTheme.Background);g.FillRectangle(footerBrush,1,footer,DesignWidth-2,FooterHeight-1);g.DrawLine(line,10,footer,DesignWidth-10,footer);
        var exact=controller.Tracker.Quality==DataQuality.Exact&&(!controller.LiveMode||controller.Session.IdentityKnown);
        var warning=!exact;
        var warningColor=Color.FromArgb(255,102,105);
        using var dot=new SolidBrush(warning?warningColor:exact?Color.FromArgb(139,192,154):PremiumTheme.Gold);g.FillEllipse(dot,12,footer+11,4,4);
        var status=!controller.LiveMode?T("demo"):exact?T("exact"):controller.Session.IdentityKnown?T("uncertain"):T("snapshot");
        DrawText(g,warning?T("resyncWarning"):status,new(23,footer+3,DesignWidth-40,warning?32:18),warning?7.5f:6.8f,warning?warningColor:exact?PremiumTheme.Ink:PremiumTheme.Gold);
        using(var grip=new Pen(Color.FromArgb(hovered=="resize"?210:110,PremiumTheme.Gold),1.4f)){var gx=DesignWidth/2f;g.DrawLine(grip,gx-12,LogicalHeight-4,gx+12,LogicalHeight-4);g.DrawLine(grip,gx-7,LogicalHeight-7,gx+7,LogicalHeight-7);}
        if(!warning){using var creditRule=new Pen(Color.FromArgb(24,PremiumTheme.Gold));g.DrawLine(creditRule,43,footer+23,DesignWidth-43,footer+23);
        PremiumTheme.Text(g,"Designed & Developed by Keyt",new(15,footer+24,DesignWidth-30,11),5.9f,Color.FromArgb(149,142,136),"en",align:StringAlignment.Center);}
    }
    private void DrawCard(Graphics g,CardMotion.Row row,RectangleF box)
    {
        var id=row.Id;var alpha=Math.Clamp(row.Alpha,0,1);var tracker=controller.Tracker;
        Color Fade(Color color)=>Color.FromArgb((int)(color.A*alpha),color.R,color.G,color.B);
        var hoverValue=hover.GetValueOrDefault(id);var pinned=tracker.Current.Preferences.Pinned.Contains(id);
        using var shape=PremiumTheme.Round(box,6);using var fill=new SolidBrush(Fade(Color.FromArgb((int)(24+hoverValue*23),(int)(26+hoverValue*12),(int)(34+hoverValue*13))));g.FillPath(fill,shape);
        using var border=new Pen(Fade(Color.FromArgb((int)(pinned?105+hoverValue*100:48+hoverValue*155),PremiumTheme.Gold)));g.DrawPath(border,shape);
        if(hoverValue>.01f){using var aura=new Pen(Fade(Color.FromArgb((int)(22*hoverValue),PremiumTheme.Gold)),3);g.DrawPath(aura,shape);using var sheen=new LinearGradientBrush(box,Color.FromArgb((int)(24*hoverValue),PremiumTheme.Gold),Color.Transparent,25);g.FillPath(sheen,shape);}
        var photo=new RectangleF(box.X+21,box.Y+12,34,34);using var photoShape=PremiumTheme.Round(photo,4);
        var pulse=(float)(.5+.5*Math.Sin(Now*1.65+id*.07));
        using var glow=new Pen(Fade(Color.FromArgb((int)(13+pulse*10),PremiumTheme.Gold)),4);g.DrawPath(glow,photoShape);
        var state=g.Save();g.SetClip(photoShape,CombineMode.Intersect);
        if(portraits.TryGetValue(id,out var image))
        {
            var size=Math.Max(1,(int)Math.Round(photo.Width*ScaleFactor));
            if(!cardPortraits.TryGetValue((id,size),out var scaled))
            {
                scaled=new Bitmap(size,size,PixelFormat.Format32bppPArgb);
                using var raster=Graphics.FromImage(scaled);raster.InterpolationMode=InterpolationMode.HighQualityBicubic;
                raster.DrawImage(image,new Rectangle(0,0,size,size));cardPortraits.Add((id,size),scaled);
            }
            image=scaled;g.InterpolationMode=InterpolationMode.Bilinear;
            if(alpha>.99f)g.DrawImage(image,photo);
            else{using var attributes=new ImageAttributes();var matrix=new ColorMatrix{Matrix33=alpha};attributes.SetColorMatrix(matrix);g.DrawImage(image,Rectangle.Round(photo),0,0,image.Width,image.Height,GraphicsUnit.Pixel,attributes);}
        }
        g.Restore(state);using var photoBorder=new Pen(Fade(Color.FromArgb((int)(75+pulse*16),PremiumTheme.Gold)));g.DrawPath(photoBorder,photoShape);
        DrawText(g,controller.Catalog.Name(id),new(box.X+60,box.Y+6,133,29),9.2f,Fade(PremiumTheme.Ink),FontStyle.Bold,language:"en");
        var total=tracker.KnownTotal(id);var progress=total.HasValue?Progress.FromTotal(total.Value):null;
        for(var star=0;star<3;star++)DrawStar(g,new(box.X+66+star*14,box.Y+46),4.7f,progress is not null&&progress.Level>star,alpha,pulse);
        var level=progress is null?"—":"Lv "+progress.Level;
        if(tracker.Session.Attacked.Contains(id)&&tracker.Session.Souls.GetValueOrDefault(id)==0)level+=" · "+T("target");
        DrawText(g,level,new(box.X+113,box.Y+37,79,18),7f,Fade(PremiumTheme.Muted));
        var count=progress is null?"—":progress.Level==3?"MAX":$"{progress.WithinLevel}/{progress.Goal}";
        var countColor=progress?.Level==3?Color.FromArgb((int)(255*(1-hoverValue*.8f)),PremiumTheme.Gold):PremiumTheme.Ink;
        DrawText(g,count,new(box.X+195,box.Y+8,70,29),14,Fade(countColor),FontStyle.Bold,StringAlignment.Center,language:"en");
        var caption=progress is null?T("waiting"):tracker.Quality!=DataQuality.Exact?T("unverified"):progress.Level==3?T("complete"):T("remaining",progress.Remaining);
        DrawText(g,caption,new(box.X+193,box.Y+36,73,16),6.8f,Fade(PremiumTheme.Muted),align:StringAlignment.Center);
        if(progress is not null)
        {
            var bar=new RectangleF(box.X+60,box.Bottom-5,198,1.4f);using var track=new SolidBrush(Fade(Color.FromArgb(43,46,53)));g.FillRectangle(track,bar);
            var target=progress.Level==3?1:(float)progress.WithinLevel/progress.Goal;
            var shown=FrameSmoothing.Approach(progressBars.GetValueOrDefault(id,target),target,frameElapsed,18);progressBars[id]=shown;progressAnimating|=shown!=target;
            using var value=new SolidBrush(Fade(Color.FromArgb(170,PremiumTheme.Gold)));g.FillRectangle(value,bar.X,bar.Y,bar.Width*shown,bar.Height);
        }
        if(!row.Removing)hits.Add((box,progress?.Level==3?"clear":"row",id));
        if(progress?.Level==3&&hoverValue>.01f)
        {
            using var clearGlow=new Pen(Fade(Color.FromArgb((int)((10+8*Math.Sin(Now*2))*hoverValue),PremiumTheme.Gold)),3);g.DrawPath(clearGlow,shape);
            using var veil=new SolidBrush(Fade(Color.FromArgb((int)(55*hoverValue),PremiumTheme.Background)));g.FillPath(veil,shape);
            DrawText(g,"CLEAR",new(box.X+48+(1-hoverValue)*18,box.Y+(float)Math.Sin(Now*2)*1.2f,box.Width-80,box.Height),23,Fade(Color.FromArgb((int)(98*hoverValue),PremiumTheme.Gold)),FontStyle.Bold,StringAlignment.Center,language:"en");
        }
        IconButton(g,new(box.Right-26,box.Y+1,20,20),"pin",pinned,id,alpha);
        IconButton(g,new(box.Right-26,box.Y+22,20,20),"map",false,id,alpha);
        IconButton(g,new(box.Right-26,box.Y+43,20,20),"dismiss",false,id,alpha);
        var index=Array.IndexOf(Ids(),id);
        IconButton(g,new(box.X+2,box.Y+10,16,20),"moveUp",false,id,alpha,enabled:!row.Removing&&index>0);
        IconButton(g,new(box.X+2,box.Y+34,16,20),"moveDown",false,id,alpha,enabled:!row.Removing&&index>=0&&index<Ids().Length-1);
    }
    private void ActionButton(Graphics g,RectangleF box,string label,string action)
    {
        var key=action+"0";drawnButtons.Add(key);buttonHover.TryAdd(key,0);var mix=buttonHover[key];
        using var path=PremiumTheme.Round(box,4);using var fill=new SolidBrush(Color.FromArgb((int)(20+mix*25),PremiumTheme.Gold));g.FillPath(fill,path);
        using var pen=new Pen(Color.FromArgb((int)(75+mix*65),PremiumTheme.Gold));g.DrawPath(pen,path);
        DrawText(g,label,box,7.1f,PremiumTheme.Gold,FontStyle.Bold,StringAlignment.Center);hits.Add((box,action,0));
    }
    private void IconButton(Graphics g,RectangleF box,string action,bool active=false,int id=0,float alpha=1,bool enabled=true)
    {
        var key=action+id;drawnButtons.Add(key);buttonHover.TryAdd(key,0);var blend=buttonHover[key];
        using var path=PremiumTheme.Round(box,4);using var fill=new SolidBrush(Color.FromArgb((int)((active?30:blend*25)*alpha),PremiumTheme.Gold));g.FillPath(fill,path);
        using var p=new Pen(Color.FromArgb((int)((enabled?255:65)*alpha),active||action=="map"||enabled&&(action=="moveUp"||action=="moveDown")?PremiumTheme.Gold:PremiumTheme.Muted),1.15f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round};
        var cx=box.X+box.Width/2;var cy=box.Y+box.Height/2;
        switch(action)
        {
            case "moveUp":g.DrawLines(p,new PointF[]{new(cx-3,cy+2),new(cx,cy-2),new(cx+3,cy+2)});break;
            case "moveDown":g.DrawLines(p,new PointF[]{new(cx-3,cy-2),new(cx,cy+2),new(cx+3,cy-2)});break;
            case "handbook":g.DrawRectangle(p,cx-5,cy-5,10,10);g.DrawLine(p,cx,cy-5,cx,cy+5);g.DrawLine(p,cx-4,cy-2,cx-1,cy-2);g.DrawLine(p,cx+1,cy-2,cx+4,cy-2);break;
            case "map":g.DrawEllipse(p,cx-5,cy-5,10,10);g.DrawEllipse(p,cx-2.3f,cy-5,4.6f,10);g.DrawLine(p,cx-5,cy,cx+5,cy);g.DrawArc(p,cx-4.6f,cy-2.7f,9.2f,3.2f,0,180);break;
            case "pin":g.DrawLine(p,cx-3.5f,cy-5,cx+3.5f,cy-5);g.DrawLines(p,new PointF[]{new(cx-2,cy-5),new(cx-2,cy),new(cx-4,cy+2),new(cx+4,cy+2),new(cx+2,cy),new(cx+2,cy-5)});g.DrawLine(p,cx,cy+2,cx,cy+6);break;
            case "dismiss":g.DrawLine(p,cx-5,cy-4,cx+5,cy-4);g.DrawRectangle(p,cx-2,cy-6,4,2);g.DrawRectangle(p,cx-3.5f,cy-3,7,9);g.DrawLine(p,cx,cy-1,cx,cy+4);break;
            case "lock":g.DrawRectangle(p,cx-4,cy,8,6);g.DrawArc(p,cx-3,cy-6,6,8,active?180:120,180);break;
            case "settings":g.DrawEllipse(p,cx-4,cy-4,8,8);g.DrawEllipse(p,cx-1.5f,cy-1.5f,3,3);for(var i=0;i<8;i++){var a=i*Math.PI/4;g.DrawLine(p,cx+(float)Math.Cos(a)*5,cy+(float)Math.Sin(a)*5,cx+(float)Math.Cos(a)*6.5f,cy+(float)Math.Sin(a)*6.5f);}break;
            case "minimize":g.DrawLine(p,cx-4,cy+3,cx+4,cy+3);break;
            case "compact":g.DrawLine(p,cx-5,cy-3,cx-5,cy+3);g.DrawLine(p,cx-5,cy-3,cx-2,cy-3);g.DrawLine(p,cx-5,cy+3,cx-2,cy+3);g.DrawLine(p,cx+5,cy-3,cx+5,cy+3);g.DrawLine(p,cx+5,cy-3,cx+2,cy-3);g.DrawLine(p,cx+5,cy+3,cx+2,cy+3);break;
            case "close":g.DrawLine(p,cx-3.5f,cy-3.5f,cx+3.5f,cy+3.5f);g.DrawLine(p,cx+3.5f,cy-3.5f,cx-3.5f,cy+3.5f);break;
        }
        hits.Add((box,enabled?action:"disabled",id));
    }
    private void DrawText(Graphics g,string text,RectangleF box,float size,Color color,FontStyle style=FontStyle.Regular,StringAlignment align=StringAlignment.Near,string? language=null)=>PremiumTheme.Text(g,text,box,size,color,language??Language,style,align);
    private static void DrawStar(Graphics g,PointF center,float radius,bool filled,float alpha,float pulse)
    {
        var points=Enumerable.Range(0,10).Select(i=>{var a=-Math.PI/2+i*Math.PI/5;var r=i%2==0?radius:radius*.44f;return new PointF(center.X+(float)Math.Cos(a)*r,center.Y+(float)Math.Sin(a)*r);}).ToArray();
        if(filled){using var glow=new Pen(Color.FromArgb((int)((15+pulse*14)*alpha),PremiumTheme.Gold),3);g.DrawPolygon(glow,points);using var b=new SolidBrush(Color.FromArgb((int)(255*alpha),PremiumTheme.Gold));g.FillPolygon(b,points);}
        using var outline=new Pen(Color.FromArgb((int)(210*alpha),filled?PremiumTheme.Gold:Color.FromArgb(83,93,110)),.8f);g.DrawPolygon(outline,points);
    }
    private static void DrawCrest(Graphics g,RectangleF r)
    {
        using var p=new Pen(PremiumTheme.Gold,.9f);var cx=r.X+r.Width/2;var cy=r.Y+r.Height/2;
        g.DrawPolygon(p,new PointF[]{new(cx,r.Y),new(r.Right,cy),new(cx,r.Bottom),new(r.X,cy)});
        using var fill=new SolidBrush(Color.FromArgb(25,PremiumTheme.Gold));g.FillPolygon(fill,new PointF[]{new(cx,r.Y+3),new(r.Right-3,cy),new(cx,r.Bottom-3),new(r.X+3,cy)});
        g.DrawLines(p,new PointF[]{new(cx-4,cy+3),new(cx,cy-4),new(cx+4,cy+3)});
    }
    private (RectangleF Bounds,string Action,int Id) Hit(PointF point)=>hits.LastOrDefault(h=>h.Bounds.Contains(point)&&(compactMode||h.Id==0||point.Y>=HeaderHeight&&point.Y<LogicalHeight-FooterHeight));
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);var point=new PointF(e.X/ScaleFactor,e.Y/ScaleFactor);
        if(dragScroll){scrollTarget=SnapScroll(Math.Clamp((point.Y-HeaderHeight)/Viewport,0,1)*MaximumScroll);return;}
        if(!compactMode&&e.Y>=ClientSize.Height-18){hoveredCard=-1;hovered="resize";Cursor=Cursors.SizeNS;tips.SetToolTip(this,StartupText.Get(Language,"resize"));return;}
        var hit=Hit(point);hoveredCard=hit.Id>0?hit.Id:-1;var next=hit.Action is null or "disabled"?"":hit.Action+hit.Id;
        if(next!=hovered)
        {
            timer.Interval=8;
            hovered=next;Cursor=next.Length>0?brightCrystal.Pointer:crystal.Pointer;
            var tip=hit.Action switch{"moveUp"=>T("moveUp"),"moveDown"=>T("moveDown"),"map"=>MapText.Get(Language,"title"),"handbook"=>HelpText.Get(Language,"handbook"),"lock"=>T(preferences.Locked?"unlock":"lock"),"compact"=>T("compact"),"settings"=>T("settings"),"pin"=>T("pin"),"dismiss"=>T("remove"),"minimize"=>T("minimize"),"close"=>T("close"),"clear"=>"CLEAR · "+T("remove"),"expand"=>T("expand"),_=>""};tips.SetToolTip(this,tip);
        }
    }
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hoveredCard=-1;hovered="";}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragScroll=false;Capture=false;}
    protected override async void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||busy)return;var point=new PointF(e.X/ScaleFactor,e.Y/ScaleFactor);
        if(point.X>DesignWidth-10&&point.Y>=HeaderHeight&&point.Y<LogicalHeight-FooterHeight&&MaximumScroll>0){dragScroll=true;Capture=true;scrollTarget=SnapScroll(Math.Clamp((point.Y-HeaderHeight)/Viewport,0,1)*MaximumScroll);timer.Interval=8;return;}
        var hit=Hit(point);
        try
        {
            switch(hit.Action)
            {
                case "lock":ToggleLock();break;
                case "compact":ToggleCompact();break;
                case "expand":ToggleCompact();break;
                case "settings":ShowSettings();break;
                case "map":ShowMap(hit.Id);break;
                case "handbook":ShowHandbook();break;
                case "minimize":WindowState=FormWindowState.Minimized;break;
                case "close":Close();break;
                case "add":ShowPetPicker();break;
                case "moveUp":case "moveDown":if(controller.Tracker.MoveCard(hit.Id,hit.Action=="moveUp"?-1:1)){controller.Save();SyncCards();var moved=Array.IndexOf(Ids(),hit.Id);if(moved*(CardHeight+CardGap)<scrollTarget)scrollTarget=SnapScroll(moved*(CardHeight+CardGap));else if(moved>=scrollTarget/(CardHeight+CardGap)+VisibleCardSlots)scrollTarget=SnapScroll((moved-VisibleCardSlots+1)*(CardHeight+CardGap));}break;
                case "pin":controller.Tracker.Pin(hit.Id,!controller.Tracker.Current.Preferences.Pinned.Contains(hit.Id));controller.Save();break;
            case "dismiss":case "clear":controller.Tracker.Dismiss(hit.Id);controller.Save();break;
                case "capture":busy=true;if(controller.LiveMode){if(controller.Running)await controller.Stop();else controller.Start();}else controller.LoadPreview();busy=false;break;
                default:if(!preferences.Locked&&point.Y<49){ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}break;
            }
            SyncCards();Invalidate();
        }
        catch(Exception ex){busy=false;Error(ex);}
    }
    private void Error(Exception ex){controller.Log("HATA: "+ex.Message);MessageBox.Show(this,ex.Message,"Aion 2 Pet Tracker",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    private void ShowSettings()
    {
        if(compactMode)ToggleCompact();
        UnregisterGlobalHotkeys();
        try
        {
            var previousLanguage=Language;var previousLock=preferences.Locked;
            using var dialog=new OverlaySettingsForm(preferences,crystal.Pointer,7,()=>
            {
                ApplyAppearance();EnsureVisible();QueueAppearanceSave();
                if(previousLanguage!=Language||previousLock!=preferences.Locked){RefreshTray();map?.SetLanguage(Language);previousLanguage=Language;previousLock=preferences.Locked;}
                scrollTarget=SnapScroll(scrollTarget);Invalidate();
            },ShowLogs,ValidateSettingsHotkeys,OpenRiftSettings);
            dialog.ShowDialog(this);
        }
        finally{appearanceSave.Stop();ApplyGlobalHotkeys();SaveAppearance();}
    }
    private void ShowHandbook()
    {
        if(handbookLanguage!=Language){handbook?.Dispose();handbook=null;}
        if(handbook is null||handbook.IsDisposed){handbook=new(Language);handbookLanguage=Language;handbook.TopMost=preferences.AlwaysOnTop;handbook.Show(this);}
        else {handbook.Show();handbook.Activate();}
    }
    private void ShowLogs()
    {
#if LOCAL_TEST
        using var dialog=new TeleportTestForm(controller){TopMost=preferences.AlwaysOnTop};dialog.ShowDialog(this);
#else
        using var dialog=new Form{Text=T("logs"),Size=new(720,420),StartPosition=FormStartPosition.CenterParent,TopMost=true,Cursor=crystal.Pointer};
        dialog.Controls.Add(new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,BackColor=PremiumTheme.Background,ForeColor=PremiumTheme.Ink,Font=new("Consolas",9),Text=string.Join(Environment.NewLine,controller.Logs)});dialog.ShowDialog(this);
#endif
    }
    private void ShowPetPicker()
    {
        if(!controller.Tracker.HasCurrent)throw new InvalidOperationException(T("characterRequired"));
        using var dialog=new PetPickerForm(controller.Catalog,portraits,Language,crystal.Pointer,id=>{controller.Tracker.Pin(id,true);controller.Save();SyncCards();Invalidate();},knownTotal:controller.Tracker.KnownTotal);dialog.ShowDialog(this);
    }
    private void ShowMap(int petId=0)
    {
        if(map is null||map.IsDisposed)
        {
            var window=new PetMapForm(controller,portraits,preferences,SaveAppearance,OpenRiftSettings);
            window.FormClosed+=(_,_)=>{if(ReferenceEquals(map,window))map=null;};map=window;
        }
        if(petId>0)map.FocusPet(petId);
        if(!map.Visible)map.Show(this);map.WindowState=FormWindowState.Normal;map.Activate();
    }
    public void ExportPreview(string path,bool clear=false)
    {
        motion.Tick(Now+1);if(clear){hoveredCard=Ids().FirstOrDefault(id=>controller.Tracker.KnownTotal(id)>=105);hover[hoveredCard]=1;}
        File.WriteAllText(path+".layout.txt",$"Bounds={Bounds};Client={ClientSize};Min={MinimumSize};Max={MaximumSize};DPI={DeviceDpi};LogicalHeight={LogicalHeight}");
        using var bitmap=new Bitmap(ClientSize.Width,ClientSize.Height);DrawToBitmap(bitmap,ClientRectangle);bitmap.Save(path);
    }
    public void ExportDialogs(string folder)
    {
        using var picker=new PetPickerForm(controller.Catalog,portraits,Language,crystal.Pointer,_=>{},knownTotal:controller.Tracker.KnownTotal);picker.CreateControl();picker.ExportPreview(Path.Combine(folder,"pet-picker.png"));picker.ExportPreview(Path.Combine(folder,"pet-picker-maxed.png"),maxed:true);
        using var settings=new OverlaySettingsForm(preferences,crystal.Pointer,1000,()=>{},()=>{});settings.CreateControl();settings.ExportPreview(Path.Combine(folder,"settings.png"));
        using var cursor=CrystalCursor.Draw(true);cursor.Save(Path.Combine(folder,"cursor.png"));
    }
    public void CheckLockedMove(){if(!preferences.Locked)throw new InvalidOperationException("Konum kilidi kapalı.");var before=Location;SendMessage(Handle,0x112,(IntPtr)0xF010,IntPtr.Zero);if(Location!=before)throw new InvalidOperationException("Kilitli pencere hareket etti.");}
    private void OpenRiftSettings()
    {using var settings=new RiftSettingsForm(preferences,()=>rift.Region,SaveAppearance,rift.ShowNotice);settings.ShowDialog(Application.OpenForms.OfType<OverlaySettingsForm>().LastOrDefault(f=>f.Visible) is {} activeSettings?activeSettings:map is {IsDisposed:false,Visible:true}?map:this);}
    protected override void Dispose(bool disposing)
    {
        if(disposing){rift?.Dispose();handbook?.Dispose();map?.Dispose();controller.Tracker.ReachedMax-=PlayMaxChime;timer.Dispose();appearanceSave.Dispose();tray?.Dispose();tips.Dispose();chime.Dispose();Icon?.Dispose();foreach(var image in portraits.Values)image.Dispose();foreach(var image in cardPortraits.Values)image.Dispose();crystal.Dispose();brightCrystal.Dispose();}base.Dispose(disposing);
    }
    [DllImport("user32.dll")]private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wp,IntPtr lp);
    [DllImport("user32.dll",SetLastError=true)]private static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
    [DllImport("user32.dll",SetLastError=true)]private static extern bool UnregisterHotKey(IntPtr window,int id);
}
