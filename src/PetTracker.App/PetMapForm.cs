using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PetTracker.App;

public sealed class PetMapForm : Form
{
    private readonly OverlayController controller;
    private readonly OverlayPreferences preferences;
    private readonly Action save;
    private readonly Action? riftSettings;
    private PremiumButton riftButton=null!;
    private PremiumButton handbookButton=null!;
    private HandbookForm? handbook;
    private PetGuideForm? discovery;
    private PremiumButton discoveryButton=null!;
    private readonly MapData data;
    private readonly string mapFolder;
    private readonly PetMapCanvas canvas=new();
    private readonly IReadOnlyDictionary<int,Image> portraits;
    private readonly PetSources petSources;
    private PetDetailCard? detailCard;
    private readonly PetMapCarousel carousel;
    private readonly CrystalCursor pointer=new();
    private readonly System.Windows.Forms.Timer refresh=new(){Interval=500};
    private PremiumButton regions=null!;
    private PremiumButton genera=null!;
    private Panel genusMenu=null!;
    public event Action<int>? PetAdded;
    private Panel worldMenu=null!;
    private TextBox search=null!;
    private CheckBox showAll=null!,portraitMarkers=null!,trackerOnly=null!,exclusiveOnly=null!;
    private Panel? petWorldMenu;
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
    private float filterOffset;
    private float FilterOffset=>filterOffset;
    private HashSet<int> TrackedIds()=>controller.Tracker.HasCurrent?controller.Tracker.VisibleIds.ToHashSet():[];
    private string M(string key,params object[] args)=>MapText.Get(language,key,args);
    private Rectangle Scaled(RectangleF r)=>new((int)Math.Round(r.X*DpiFactor),(int)Math.Round(r.Y*DpiFactor),(int)Math.Round(r.Width*DpiFactor),(int)Math.Round(r.Height*DpiFactor));

    public PetMapForm(OverlayController controller,IReadOnlyDictionary<int,Image> images,OverlayPreferences preferences,Action save,Action? riftSettings=null)
    {
        this.portraits=images;petSources=PetSources.Load(Path.Combine(AppContext.BaseDirectory,"data","pet-sources.json"));
        this.controller=controller;this.preferences=preferences;this.save=save;this.riftSettings=riftSettings;language=preferences.Language;
        if(!controller.Catalog.Pets.Values.Any(p=>p.Genus==preferences.MapGenus))preferences.MapGenus="";
        mapFolder=Path.Combine(AppContext.BaseDirectory,"data","maps");data=MapData.Load(Path.Combine(mapFolder,"spawns.json"));
        worldId=data.Worlds.ContainsKey(preferences.MapWorldId)?preferences.MapWorldId:1010;
        if(!data.Worlds.ContainsKey(worldId))worldId=data.Worlds.Keys.First();
        AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;DoubleBuffered=true;StartPosition=FormStartPosition.Manual;
        BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;ShowInTaskbar=true;TopMost=preferences.AlwaysOnTop;Cursor=pointer.Pointer;Icon=AppBrand.CreateIcon();
        ClientSize=new((int)(preferences.MapWidth*DpiFactor),(int)(preferences.MapHeight*DpiFactor));MinimumSize=new((int)(760*DpiFactor),(int)(586*DpiFactor));
        var area=Screen.PrimaryScreen!.WorkingArea;
        Location=new(preferences.MapLeft??area.Left+(area.Width-Width)/2,preferences.MapTop??area.Top+(area.Height-Height)/2);EnsureVisible();
        carousel=new(images,id=>controller.Tracker.HasCurrent?controller.Tracker.KnownTotal(id):null){Cursor=pointer.Pointer};
        carousel.PetSelected+=id=>Try(()=>{FocusPet(id);ShowPetCard(id);});canvas.ZoomChanged+=OnZoomChanged;
        canvas.SetPortraits(images);canvas.MarkerSelected+=id=>Try(()=>{FocusPet(id);ShowPetCard(id);});
        canvas.MouseDown+=(_,_)=>{petWorldMenu?.Hide();worldMenu?.Hide();genusMenu?.Hide();};
        carousel.MouseDown+=(_,_)=>{petWorldMenu?.Hide();worldMenu?.Hide();genusMenu?.Hide();};
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
    private string RegionName=>preferences.MapAllPetsCategory?M("allPets"):preferences.MapUnlocatedCategory?M("unlocated"):data.Worlds[worldId].Name;
    private void HidePetCard(){petWorldMenu?.Dispose();petWorldMenu=null;detailCard?.Dispose();detailCard=null;}
    private void ShowPetCard(int id)
    {
        HidePetCard();if(!controller.Catalog.Pets.TryGetValue(id,out var pet))return;
        detailCard=new(pet,portraits.GetValueOrDefault(id),data,petSources.Get(id),Total(id),controller.Tracker.Quality==DataQuality.Exact,language,HidePetCard,world=>FocusPetInWorld(id,world),anchor=>ShowPetWorldMenu(id,anchor),controller.Tracker.HasCurrent?()=>AddPetToTracker(id):null);
        Controls.Add(detailCard);LayoutChrome();detailCard.BringToFront();
    }
    private bool AddPetToTracker(int id)
    {
        var added=false;Try(()=>{if(!controller.Tracker.HasCurrent)return;controller.Tracker.Pin(id,true);controller.Save();PetAdded?.Invoke(id);added=true;});return added;
    }
    private string GenusName=>string.IsNullOrEmpty(preferences.MapGenus)?M("allTypes"):preferences.MapGenus;
    private void SelectGenus(string genus)
    {HidePetCard();genusMenu.Hide();preferences.MapGenus=genus;genera.Text=GenusName+"  ▾";save();Filter();UpdateSelection(false);}
    private void FocusPetInWorld(int petId,int targetWorld)
    {
        if(data.Locations(petId,targetWorld).Count==0)return;
        if(preferences.MapExclusiveOnly&&!PetGuideQuery.IsExclusive(data,petId,targetWorld,petSources.Get(petId))){preferences.MapExclusiveOnly=false;exclusiveOnly.Checked=false;}
        if(preferences.MapTrackerOnly&&!TrackedIds().Contains(petId)){preferences.MapTrackerOnly=false;trackerOnly.Checked=false;}
        HidePetCard();worldMenu.Hide();worldId=targetWorld;preferences.MapUnlocatedCategory=false;preferences.MapAllPetsCategory=false;LoadWorld();FocusPet(petId);save();
    }
    private void ShowPetWorldMenu(int petId,Control anchor)
    {
        petWorldMenu?.Dispose();var menu=new Panel{BackColor=PremiumTheme.Panel};petWorldMenu=menu;Controls.Add(menu);
        var worlds=data.WorldsForPet(petId);var rowHeight=(int)(34*DpiFactor);var height=worlds.Count*rowHeight+(int)(8*DpiFactor);
        var point=PointToClient(anchor.PointToScreen(new(0,anchor.Height)));var width=(int)(220*DpiFactor);
        menu.Bounds=new(Math.Clamp(point.X,0,ClientSize.Width-width),point.Y+height<=ClientSize.Height?point.Y:Math.Max(0,point.Y-anchor.Height-height),width,height);
        foreach(var pair in worlds.Select((world,index)=>(world,index)))
        {
            var choice=new PremiumButton{Text=data.Worlds[pair.world].Name,TextLanguage=language,DpiAwareText=true,TextSize=10,Bounds=new((int)(4*DpiFactor),(int)(4*DpiFactor)+pair.index*rowHeight,width-(int)(8*DpiFactor),rowHeight-2)};
            choice.Click+=(_,_)=>Try(()=>FocusPetInWorld(petId,pair.world));menu.Controls.Add(choice);
        }
        menu.BringToFront();
    }
    private void BuildChrome()
    {
        HidePetCard();
        foreach(var control in Controls.Cast<Control>().Where(control=>control!=canvas&&control!=carousel).ToArray()){Controls.Remove(control);control.Dispose();}
        Text="Aion 2 Pet Tracker · "+M("title");carousel.Language=language;canvas.Language=language;
        farmable=Button(M("farmable"),()=>SelectTab(false));maxedButton=Button(UiText.Get(language,"maxedPets"),()=>SelectTab(true));
        regions=Button(RegionName+"  ▾",()=>{genusMenu.Hide();worldMenu.Visible=!worldMenu.Visible;if(worldMenu.Visible)worldMenu.BringToFront();});
        regions.AccessibleName=M("region");
        worldMenu=new Panel{BackColor=PremiumTheme.Panel,Visible=false};Controls.Add(worldMenu);
        foreach(var world in data.Worlds.Values.OrderBy(world=>world.Id))
        {
            var choice=new PremiumButton{Text=world.Name,TextLanguage=language,DpiAwareText=true,TextSize=10,Cursor=pointer.Pointer};
            choice.Click+=(_,_)=>Try(()=>{HidePetCard();worldMenu.Hide();preferences.MapUnlocatedCategory=false;preferences.MapAllPetsCategory=false;worldId=world.Id;regions.Text=world.Name+"  ▾";LoadWorld();Filter();UpdateSelection(true);});
            worldMenu.Controls.Add(choice);
        }
        var unlocatedChoice=new PremiumButton{Text=M("unlocated"),TextLanguage=language,DpiAwareText=true,TextSize=10,Cursor=pointer.Pointer};
        unlocatedChoice.Click+=(_,_)=>Try(()=>{HidePetCard();worldMenu.Hide();preferences.MapUnlocatedCategory=true;preferences.MapAllPetsCategory=false;regions.Text=RegionName+"  ▾";save();Filter();UpdateSelection(false);});worldMenu.Controls.Add(unlocatedChoice);
        var allPetsChoice=new PremiumButton{Text=M("allPets"),TextLanguage=language,DpiAwareText=true,TextSize=10,Cursor=pointer.Pointer};
        allPetsChoice.Click+=(_,_)=>Try(()=>{HidePetCard();worldMenu.Hide();preferences.MapUnlocatedCategory=false;preferences.MapAllPetsCategory=true;regions.Text=RegionName+"  ▾";save();Filter();UpdateSelection(false);});worldMenu.Controls.Add(allPetsChoice);
        genera=Button(GenusName+"  ▾",()=>{worldMenu.Hide();genusMenu.Visible=!genusMenu.Visible;if(genusMenu.Visible)genusMenu.BringToFront();});genera.AccessibleName=M("petType");
        genusMenu=new Panel{BackColor=PremiumTheme.Panel,Visible=false};Controls.Add(genusMenu);
        foreach(var genus in new[]{""}.Concat(controller.Catalog.Pets.Values.Select(p=>p.Genus).Where(g=>!string.IsNullOrWhiteSpace(g)).Distinct().Order()))
        {
            var choice=new PremiumButton{Text=genus.Length==0?M("allTypes"):genus,TextLanguage=language,DpiAwareText=true,TextSize=10,Cursor=pointer.Pointer};
            choice.Click+=(_,_)=>Try(()=>SelectGenus(genus));genusMenu.Controls.Add(choice);
        }
        searchFrame=new(){BackColor=PremiumTheme.Panel};search=new(){BorderStyle=BorderStyle.None,BackColor=PremiumTheme.Panel,ForeColor=PremiumTheme.Ink,Font=new(UiText.FontFamily(language),10),PlaceholderText=UiText.Get(language,"search"),Cursor=Cursors.IBeam,RightToLeft=language=="ar"?RightToLeft.Yes:RightToLeft.No};
        search.TextChanged+=(_,_)=>{Filter();if(selectionLabel is not null)UpdateSelection(false);};searchFrame.Controls.Add(search);Controls.Add(searchFrame);
        showAll=Option(M("showAll"),preferences.MapShowAllFarmable,value=>{preferences.MapShowAllFarmable=value;save();UpdateSelection(false);if(value)canvas.FocusLocations();else FocusSelectedLocations();});
        portraitMarkers=Option(HelpText.Get(language,"portraits"),preferences.MapPortraitMarkers,value=>{preferences.MapPortraitMarkers=value;save();UpdateSelection(false);});
        trackerOnly=Option(M("trackerOnly"),preferences.MapTrackerOnly&&TrackedIds().Count>0,value=>{preferences.MapTrackerOnly=value;save();Filter();UpdateSelection(false);canvas.FocusLocations();});
        exclusiveOnly=Option(M("exclusive"),preferences.MapExclusiveOnly,value=>{preferences.MapExclusiveOnly=value;HidePetCard();save();Filter();UpdateSelection(false);canvas.FocusLocations();});
        exclusiveOnly.BackColor=Color.FromArgb(28,26,43);
        discoveryButton=Button("",()=>
        {
            if(discovery is {IsDisposed:false}){discovery.Show();discovery.Activate();return;}
            discovery=new(controller,data,petSources,portraits,language,worldId,preferences.MapGenus,AddPetToTracker,(id,w)=>{FocusPetInWorld(id,w);ShowPetCard(id);Activate();}){TopMost=TopMost};
            discovery.Show(this);
        });
        discoveryButton.BookIcon=true;discoveryButton.AccessibleName=M("petGuide");discoveryButton.ForeColor=RiftVisual.Cyan;discoveryButton.AccentColor=RiftVisual.Cyan;
        ((AtlasCheckBox)trackerOnly).WarningAccent=true;
        trackerOnly.CheckedChanged+=(_,_)=>trackerOnly.ForeColor=trackerOnly.Checked?Color.FromArgb(255,113,129):PremiumTheme.Ink;
        previous=Button("‹",()=>carousel.ScrollBy(-1));next=Button("›",()=>carousel.ScrollBy(1));
        minus=Button("−",()=>canvas.ChangeZoom(.8));plus=Button("+",()=>canvas.ChangeZoom(1.25));focus=Button(M("focus"),()=>FocusSelectedLocations());
        zoom=new(){Minimum=0,Maximum=100,Value=0,Cursor=pointer.Pointer,AccessibleName=M("zoom")};zoom.ValueChanged+=(_,_)=>{if(!updatingZoom)canvas.SetZoom(Math.Pow(MapCamera.MaximumZoom,zoom.Value/100d));};Controls.Add(zoom);
        zoomLabel=Label("1.0×",10);selectionLabel=Label(M("select"),10);selectionLabel.ForeColor=PremiumTheme.Gold;status=Label(M("static"),8);
        riftButton=Button("Spacetime Rift",()=>riftSettings?.Invoke());riftButton.Enabled=riftSettings is not null;
        handbookButton=Button("",()=>
        {
            if(handbook is null||handbook.IsDisposed){handbook=new(language){TopMost=TopMost};handbook.Show(this);}else{handbook.Show();handbook.Activate();}
        });
        handbookButton.AccessibleName=HelpText.Get(language,"handbook");
        handbookButton.BookIcon=true;
        minimize=Button("−",()=>WindowState=FormWindowState.Minimized);close=Button("×",()=>Close());
        Filter();OnZoomChanged(canvas.Zoom);LayoutChrome();worldMenu.BringToFront();
    }
    private CheckBox Option(string text,bool value,Action<bool> changed)
    {
        var box=new AtlasCheckBox(language){Text=text,Checked=value,AutoSize=false,FlatStyle=FlatStyle.Flat,ForeColor=value?PremiumTheme.Gold:PremiumTheme.Ink,BackColor=PremiumTheme.Panel,Font=new(UiText.FontFamily(language),10),Cursor=pointer.Pointer,AccessibleName=text};
        box.FlatAppearance.BorderColor=PremiumTheme.Gold;
        box.CheckedChanged+=(_,_)=>box.ForeColor=box.Checked?PremiumTheme.Gold:PremiumTheme.Ink;
        box.CheckedChanged+=(_,_)=>Try(()=>changed(box.Checked));Controls.Add(box);return box;
    }
    private void FocusSelectedLocations()
    {
        var selected=data.Locations(selectedId,worldId);
        if(selected.Count>0)canvas.FocusLocations(selected);else canvas.FocusLocations();
    }
    private void SelectTab(bool value)
    {
        maxed=value;Filter();UpdateSelection(false);
        if(!matches.Any(pet=>pet.Id==selectedId)){selectedId=0;carousel.SetPets(matches,selectedId);UpdateSelection(false);}
    }
    private void Filter()
    {
        var tracked=TrackedIds();trackerOnly.Enabled=tracked.Count>0;
        exclusiveOnly.Enabled=!preferences.MapAllPetsCategory&&!preferences.MapUnlocatedCategory;
        if(!exclusiveOnly.Enabled&&preferences.MapExclusiveOnly){preferences.MapExclusiveOnly=false;exclusiveOnly.Checked=false;save();}
        if(tracked.Count==0&&preferences.MapTrackerOnly){preferences.MapTrackerOnly=false;trackerOnly.Checked=false;save();}
        matches=MapPetQuery.Filter(controller.Catalog,data,preferences.MapAllPetsCategory?-1:preferences.MapUnlocatedCategory?0:worldId,search.Text,maxed,false,Total,preferences.MapGenus,preferences.MapTrackerOnly?tracked:null,preferences.MapExclusiveOnly,petSources);
        if(selectedId>0&&!matches.Any(pet=>pet.Id==selectedId)){selectedId=0;HidePetCard();}
        carousel.SetPets(matches,selectedId);showAll.Visible=!maxed;showAll.Enabled=!preferences.MapUnlocatedCategory&&!preferences.MapTrackerOnly;portraitMarkers.Enabled=!preferences.MapUnlocatedCategory;
        farmable.ForeColor=maxed?PremiumTheme.Muted:PremiumTheme.Gold;maxedButton.ForeColor=maxed?PremiumTheme.Gold:PremiumTheme.Muted;farmable.Invalidate();maxedButton.Invalidate();Invalidate();
    }
    public void FocusPet(int petId)
    {
        if(preferences.MapExclusiveOnly&&!PetGuideQuery.IsExclusive(data,petId,data.BestWorld(petId,worldId)??worldId,petSources.Get(petId))){preferences.MapExclusiveOnly=false;exclusiveOnly.Checked=false;}
        if(controller.Catalog.Pets.TryGetValue(petId,out var target)&&preferences.MapGenus.Length>0&&preferences.MapGenus!=target.Genus){preferences.MapGenus="";genera.Text=GenusName+"  ▾";}
        maxed=Total(petId) is >=105;search.Text="";
        preferences.MapUnlocatedCategory=data.WorldsForPet(petId).Count==0&&!preferences.MapAllPetsCategory;
        selectedId=petId;if(data.WorldsForPet(petId).Count>0)preferences.MapUnlocatedCategory=false;
        if(data.BestWorld(petId,worldId) is { } best&&worldId!=best)
        {worldId=best;worldMenu.Hide();regions.Text=data.Worlds[worldId].Name+"  ▾";LoadWorld();}
        regions.Text=RegionName+"  ▾";Filter();UpdateSelection(true);carousel.EnsureSelectedVisible();
    }
    private void LoadWorld()
    {canvas.SetWorld(Path.Combine(mapFolder,data.Worlds[worldId].ImageKey+".png"));preferences.MapWorldId=worldId;}
    private void UpdateSelection(bool animate)
    {
        var locations=selectedId>0?data.Locations(selectedId,worldId):[];
        var empty=selectedId<=0?M("select"):data.WorldsForPet(selectedId).Count==0?M("noLocations"):M("noRegionLocations");
        var displayed=preferences.MapTrackerOnly?MapPetQuery.TrackedLocations(data,worldId,matches.Select(p=>p.Id)):!maxed&&!preferences.MapUnlocatedCategory&&preferences.MapShowAllFarmable?MapPetQuery.TrackedLocations(data,worldId,matches.Select(p=>p.Id)):locations;
        canvas.SetLocations(displayed,empty);
        canvas.SetMarkerStyle(!preferences.MapUnlocatedCategory&&preferences.MapPortraitMarkers,selectedId);
        selectionLabel.Text=selectedId<=0?RegionName+"  ·  "+(!maxed&&!preferences.MapUnlocatedCategory&&preferences.MapShowAllFarmable?M("locations",displayed.Count):M("select")):controller.Catalog.Name(selectedId)+"  ·  "+M("locations",locations.Count);
        if(preferences.MapTrackerOnly)selectionLabel.Text=M("trackerOnlyNotice")+" · "+M("locations",displayed.Count);
        if(preferences.MapExclusiveOnly)selectionLabel.Text=M("exclusive")+" · "+selectionLabel.Text;
        selectionLabel.ForeColor=preferences.MapTrackerOnly?Color.FromArgb(255,113,129):PremiumTheme.Gold;
        focus.Enabled=displayed.Count>0;focus.ForeColor=focus.Enabled?PremiumTheme.Gold:PremiumTheme.Muted;
        if(animate)FocusSelectedLocations();RefreshStatus();
    }
    private void RefreshStatus()
    {
        var quality=controller.Tracker.Quality==DataQuality.Exact?"":UiText.Get(language,"last")+" · ";
        status.Text=quality+M("static")+" · "+data.SnapshotDate.ToString("yyyy-MM-dd");
    }
    public void SetLanguage(string code)
    {
        if(language==code)return;discovery?.Close();language=code;var query=search.Text;BuildChrome();search.Text=query;UpdateSelection(false);Invalidate();
    }
    private void RefreshProgress()
    {
        if(preferences.Language!=language)SetLanguage(preferences.Language);
        if(TopMost!=preferences.AlwaysOnTop)TopMost=preferences.AlwaysOnTop;
        if(WindowState==FormWindowState.Minimized)return;
        var fingerprint=(controller.Tracker.HasCurrent?controller.Tracker.Current.Scope.Key:"")+"|"+string.Join(",",controller.Catalog.Pets.Keys.Select(id=>Total(id)))+"|"+string.Join(",",TrackedIds().Order())+"|"+controller.Tracker.Quality;
        if(lastFingerprint!=fingerprint)
        {
            lastFingerprint=fingerprint;
            if(selectedId>0&&matches.Any(pet=>pet.Id==selectedId))maxed=Total(selectedId) is >=105;
            Filter();UpdateSelection(false);
        }
        RefreshStatus();
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
        var tabWidth=width<900?145:190;var regionWidth=width<900?190:245;var regionX=22+2*(tabWidth+10);var genusX=regionX+regionWidth+10;var genusWidth=Math.Min(210,width-genusX-22);
        farmable.Bounds=Scaled(new(22,70,tabWidth,32));maxedButton.Bounds=Scaled(new(32+tabWidth,70,tabWidth,32));
        regions.Bounds=Scaled(new(regionX,70,regionWidth,32));worldMenu.Bounds=Scaled(new(regionX,104,regionWidth,296));
        foreach(var pair in worldMenu.Controls.Cast<Control>().Select((control,index)=>(control,index)))pair.control.Bounds=Scaled(new(4,4+pair.index*36,regionWidth-8,32));
        genera.Bounds=Scaled(new(genusX,70,genusWidth,32));genusMenu.Bounds=Scaled(new(genusX,104,genusWidth,8+genusMenu.Controls.Count*36));
        foreach(var pair in genusMenu.Controls.Cast<Control>().Select((control,index)=>(control,index)))pair.control.Bounds=Scaled(new(4,4+pair.index*36,genusWidth-8,32));
        searchFrame.Bounds=Scaled(new(22,114,width-44,35));search.Bounds=Scaled(new(12,7,width-68,24));
        var available=width-60;var x=30f;var row=0;
        foreach(var option in new[]{showAll,portraitMarkers,trackerOnly})
        {
            var optionWidth=Math.Min(available,TextRenderer.MeasureText(option.Text,option.Font).Width/DpiFactor+38);
            if(x>30&&x+optionWidth>width-30){row++;x=30;}
            option.Bounds=Scaled(new(x,152+row*36,optionWidth,32));x+=optionWidth+12;
        }
        var exclusiveWidth=Math.Min(available-44,TextRenderer.MeasureText(exclusiveOnly.Text,exclusiveOnly.Font).Width/DpiFactor+38);
        var groupWidth=exclusiveWidth+40;
        if(x>30&&x+groupWidth>width-30){row++;x=30;}
        var groupX=width-30-groupWidth;
        exclusiveOnly.Bounds=Scaled(new(groupX,152+row*36,exclusiveWidth,32));discoveryButton.Bounds=Scaled(new(width-62,152+row*36,32,32));
        filterOffset=row*36;var offset=FilterOffset;
        var minimumHeight=(int)((550+offset)*DpiFactor);if(MinimumSize.Height!=minimumHeight)MinimumSize=new((int)(760*DpiFactor),minimumHeight);
        previous.Bounds=Scaled(new(22,192+offset,26,74));next.Bounds=Scaled(new(width-48,192+offset,26,74));carousel.Bounds=Scaled(new(57,189+offset,width-114,82));
        selectionLabel.Bounds=Scaled(new(23,279+offset,width-46,27));canvas.Bounds=Scaled(new(22,315+offset,width-44,Math.Max(100,height-405-offset)));
        var footer=height-73;minus.Bounds=Scaled(new(width-365,footer,29,29));zoom.Bounds=Scaled(new(width-325,footer,185,29));plus.Bounds=Scaled(new(width-127,footer,29,29));zoomLabel.Bounds=Scaled(new(width-86,footer+4,64,24));
        focus.Bounds=Scaled(new(22,footer,210,29));status.Bounds=Scaled(new(23,height-30,width-46,20));
        if(detailCard is not null){detailCard.Bounds=Scaled(new(width-322,315+offset,300,Math.Min(500,Math.Max(145,height-405-offset))));detailCard.BringToFront();petWorldMenu?.BringToFront();}
        riftButton.Bounds=Scaled(new(width-341,17,195,29));handbookButton.Bounds=Scaled(new(width-136,17,29,29));
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
        using var filterPath=PremiumTheme.Round(new(22,150,width-44,36+FilterOffset),6);using var filterFill=new SolidBrush(PremiumTheme.Panel);g.FillPath(filterFill,filterPath);
        using var separator=new Pen(Color.FromArgb(35,PremiumTheme.Gold));g.DrawLine(separator,22,307+FilterOffset,width-22,307+FilterOffset);
        using var emblem=AppBrand.Draw(36);g.DrawImage(emblem,22,13,32,32);
        PremiumTheme.Text(g,M("title"),new(66,11,width-390,35),18,PremiumTheme.Gold,language,FontStyle.Bold);
        PremiumTheme.Text(g,M("hint"),new(248,height-76,width-635,34),7.5f,PremiumTheme.Muted,language);
        var tab=maxed?maxedButton:farmable;if(tab is not null){using var selected=new Pen(PremiumTheme.Gold,2);g.DrawLine(selected,tab.Left/DpiFactor+12,tab.Bottom/DpiFactor+3,tab.Right/DpiFactor-12,tab.Bottom/DpiFactor+3);}
        using var grip=new Pen(PremiumTheme.Gold);g.DrawLine(grip,width-12,height-4,width-4,height-12);g.DrawLine(grip,width-18,height-4,width-4,height-18);
    }
    protected override void OnResize(EventArgs e){base.OnResize(e);LayoutChrome();canvas.SetActive(WindowState!=FormWindowState.Minimized);}
    protected override void OnDpiChanged(DpiChangedEventArgs e){base.OnDpiChanged(e);MinimumSize=new((int)(760*DpiFactor),(int)(586*DpiFactor));LayoutChrome();EnsureVisible();}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);worldMenu?.Hide();genusMenu?.Hide();if(e.Button==MouseButtons.Left&&e.Y<60*DpiFactor){ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}}
    protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==Keys.Escape){if(petWorldMenu?.Visible==true)petWorldMenu.Hide();else if(genusMenu.Visible)genusMenu.Hide();else if(worldMenu.Visible)worldMenu.Hide();else if(detailCard is not null)HidePetCard();else Close();return true;}return base.ProcessCmdKey(ref message,key);}
    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);if(message.Msg!=0x84||message.Result.ToInt32()!=1)return;
        var packed=message.LParam.ToInt64();var point=PointToClient(new(unchecked((short)(packed&0xffff)),unchecked((short)((packed>>16)&0xffff))));
        var edge=(int)(7*DpiFactor);var left=point.X<edge;var right=point.X>=Width-edge;var top=point.Y<edge;var bottom=point.Y>=Height-edge;
        message.Result=(IntPtr)(top?(left?13:right?14:12):bottom?(left?16:right?17:15):left?10:right?11:1);
    }
    private void Try(Action action){try{action();}catch(Exception ex){controller.Log("Harita hatası: "+ex.Message);MessageBox.Show(this,ex.Message,M("title"),MessageBoxButtons.OK,MessageBoxIcon.Error);}}
    protected override void Dispose(bool disposing){if(disposing){discovery?.Dispose();handbook?.Dispose();refresh.Dispose();pointer.Dispose();Icon?.Dispose();}base.Dispose(disposing);}
    [DllImport("user32.dll")]private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr window,int message,IntPtr wParam,IntPtr lParam);
}
