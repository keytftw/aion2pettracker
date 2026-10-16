using System.Diagnostics;
using System.Runtime.InteropServices;
namespace PetTracker.App;

// A modeless, offline reference. Selection never changes soul counters.
public sealed class PetGuideForm : Form
{
    private readonly OverlayController controller;
    private readonly MapData data;
    private readonly PetSources sources;
    private readonly IReadOnlyDictionary<int,Image> portraits;
    private readonly string language;
    private readonly Func<int,bool> add;
    private readonly Action<int,int> focus;
    private readonly ComboBox regions=new(),genera=new(),progress=new(),kinds=new();
    private readonly TextBox search=new();
    private readonly DataGridView list=new();
    private readonly Label note=new(),count=new(),empty=new();
    private readonly Panel details=new();
    private readonly PremiumButton single,special;
    private readonly System.Windows.Forms.Timer refresh=new(){Interval=1000};
    private readonly ContextMenuStrip worldsMenu=new();
    private PetDefinition[] matches=[];
    private PetDetailCard? card;
    private bool specialMode,building=true;
    private string fingerprint="";
    private int selectedId;
    private string M(string key)=>MapText.Get(language,key);
    private long? Total(int id)=>controller.Tracker.HasCurrent?controller.Tracker.KnownTotal(id):null;
    private sealed record Choice(string Text,string Key){public override string ToString()=>Text;}
    private string Key(ComboBox box)=>(box.SelectedItem as Choice)?.Key??"";
    public PetGuideForm(OverlayController controller,MapData data,PetSources sources,IReadOnlyDictionary<int,Image> portraits,string language,int world,string genus,Func<int,bool> add,Action<int,int> focus)
    {
        this.controller=controller;this.data=data;this.sources=sources;this.portraits=portraits;this.language=language;this.add=add;this.focus=focus;
        Text=M("petGuide");BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;Font=new(UiText.FontFamily(language),10);
        AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new(1040,690);MinimumSize=new(880,620);StartPosition=FormStartPosition.CenterParent;Icon=AppBrand.CreateIcon();
        single=new(){Text=M("exclusiveList"),TextLanguage=language,AccentColor=RiftVisual.Cyan,ForeColor=RiftVisual.Cyan,Dock=DockStyle.Left,Width=210};
        special=new(){Text=M("specialList"),TextLanguage=language,AccentColor=RiftVisual.Violet,ForeColor=RiftVisual.Violet,Dock=DockStyle.Left,Width=210};
        single.Click+=(_,_)=>{specialMode=false;Rebuild();};special.Click+=(_,_)=>{specialMode=true;Rebuild();};
        var tabs=new Panel{Dock=DockStyle.Top,Height=48,Padding=new(12,7,12,7)};tabs.Controls.AddRange([special,single]);
        var heading=new Label{Text=M("petGuide"),Dock=DockStyle.Top,Height=48,Padding=new(14,12,0,0),ForeColor=RiftVisual.Cyan,Font=new(UiText.FontFamily(language),16,FontStyle.Bold)};
        var filters=new TableLayoutPanel{Dock=DockStyle.Top,Height=44,ColumnCount=4,Padding=new(12,4,12,4)};
        foreach(var box in new[]{regions,genera,progress,kinds})
        {
            box.DropDownStyle=ComboBoxStyle.DropDownList;box.FlatStyle=FlatStyle.Flat;box.BackColor=PremiumTheme.Panel;box.ForeColor=PremiumTheme.Ink;box.Dock=DockStyle.Fill;box.Margin=new(0,0,8,0);
            filters.ColumnStyles.Add(new(SizeType.Percent,25));filters.Controls.Add(box);box.SelectedIndexChanged+=(_,_)=>{if(!building)Rebuild();};
        }
        regions.AccessibleName=M("region");genera.AccessibleName=M("petType");progress.AccessibleName=M("farmable");kinds.AccessibleName=M("specialList");
        regions.Items.Add(new Choice(M("allMaps"),"-1"));foreach(var w in data.Worlds.Values.OrderBy(w=>w.Id))regions.Items.Add(new Choice(w.Name,w.Id.ToString()));
        regions.SelectedIndex=Math.Max(0,Enumerable.Range(0,regions.Items.Count).FirstOrDefault(i=>(regions.Items[i] as Choice)?.Key==world.ToString()));
        genera.Items.Add(new Choice(M("allTypes"),""));foreach(var g in controller.Catalog.Pets.Values.Select(p=>p.Genus).Distinct().Order())genera.Items.Add(new Choice(g,g));
        genera.SelectedIndex=Enumerable.Range(0,genera.Items.Count).FirstOrDefault(i=>(genera.Items[i] as Choice)?.Key==genus);
        progress.Items.AddRange([new Choice(M("farmable"),"farm"),new Choice(UiText.Get(language,"maxedPets"),"max")]);progress.SelectedIndex=0;
        kinds.Items.Add(new Choice(M("allTypes"),""));foreach(var kind in new[]{"worldBoss","dungeonBoss","shop","other"})kinds.Items.Add(new Choice(M(kind),kind));kinds.SelectedIndex=0;
        var searchPanel=new Panel{Dock=DockStyle.Top,Height=36,Padding=new(12,4,12,4)};
        search.PlaceholderText=UiText.Get(language,"search");search.BorderStyle=BorderStyle.FixedSingle;search.BackColor=PremiumTheme.Panel;search.ForeColor=PremiumTheme.Ink;search.Dock=DockStyle.Fill;search.AccessibleName=search.PlaceholderText;search.TextChanged+=(_,_)=>{if(!building)Rebuild();};searchPanel.Controls.Add(search);
        note.Dock=DockStyle.Top;note.Height=66;note.Padding=new(14,6,14,4);note.ForeColor=PremiumTheme.Muted;note.Font=new(UiText.FontFamily(language),9);
        count.Dock=DockStyle.Bottom;count.Height=30;count.Padding=new(14,5,8,0);count.ForeColor=RiftVisual.Cyan;
        details.Dock=DockStyle.Fill;details.Padding=new(6);details.BackColor=PremiumTheme.Background;
        empty.Dock=DockStyle.Fill;empty.TextAlign=ContentAlignment.MiddleCenter;empty.ForeColor=PremiumTheme.Muted;empty.Text=M("guideEmpty");
        list.Dock=DockStyle.Fill;list.BackgroundColor=PremiumTheme.Background;list.BorderStyle=BorderStyle.None;list.AutoGenerateColumns=false;list.ReadOnly=true;list.AllowUserToAddRows=false;list.AllowUserToDeleteRows=false;list.AllowUserToResizeRows=false;list.RowHeadersVisible=false;list.MultiSelect=false;list.SelectionMode=DataGridViewSelectionMode.FullRowSelect;list.VirtualMode=true;list.RowTemplate.Height=76;
        list.EnableHeadersVisualStyles=false;list.ColumnHeadersDefaultCellStyle=new(){BackColor=Color.FromArgb(25,31,41),ForeColor=RiftVisual.Cyan,Font=new(UiText.FontFamily(language),9,FontStyle.Bold)};
        list.DefaultCellStyle=new(){BackColor=PremiumTheme.Panel,ForeColor=PremiumTheme.Ink,SelectionBackColor=Color.FromArgb(38,51,65),SelectionForeColor=Color.White,Padding=new(5),WrapMode=DataGridViewTriState.True};
        list.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(20,25,33);list.GridColor=Color.FromArgb(38,44,55);list.ColumnHeadersHeight=32;
        list.Columns.Add(new DataGridViewImageColumn{Width=58,ImageLayout=DataGridViewImageCellLayout.Zoom,HeaderText="",DefaultCellStyle=new(){NullValue=null,Padding=new(5)}});
        list.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=M("title"),Width=182,SortMode=DataGridViewColumnSortMode.NotSortable});
        list.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=M("suggested"),AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill,SortMode=DataGridViewColumnSortMode.NotSortable});
        list.CellValueNeeded+=(_,e)=>
        {
            if(e.RowIndex<0||e.RowIndex>=matches.Length)return;var pet=matches[e.RowIndex];var info=sources.Get(pet.Id);
            var route=PetGuideQuery.Route(data,pet,info,SelectedWorld);var regionText=string.Join(", ",data.WorldsForPet(pet.Id).Select(id=>data.Worlds[id].Name));
            e.Value=e.ColumnIndex switch{0=>portraits.GetValueOrDefault(pet.Id),1=>pet.Name+"\n"+pet.Genus+(specialMode?" · "+M(PetGuideQuery.Kind(data,pet,info)):" · "+regionText),_=>M(route.Key)+"\n"+route.Detail};
        };
        list.CellClick+=(_,e)=>{if(e.RowIndex>=0&&e.RowIndex<matches.Length&&card is null)ShowCard(matches[e.RowIndex].Id);};
        list.CurrentCellChanged+=(_,_)=>{if(!building&&list.CurrentCell is {} cell&&cell.RowIndex<matches.Length)ShowCard(matches[cell.RowIndex].Id);};
        var body=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new(12,0,12,0),ColumnCount=2,RowCount=1};
        body.ColumnStyles.Add(new(SizeType.Percent,100));body.ColumnStyles.Add(new(SizeType.Absolute,326));body.RowStyles.Add(new(SizeType.Percent,100));
        var table=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};table.Controls.AddRange([list,empty]);body.Controls.Add(table,0,0);body.Controls.Add(details,1,0);
        Controls.AddRange([body,count,note,searchPanel,filters,tabs,heading]);
        building=false;Rebuild();refresh.Tick+=(_,_)=>{if(!Visible||WindowState==FormWindowState.Minimized)return;var key=(controller.Tracker.HasCurrent?controller.Tracker.Current.Scope.Key:"")+"|"+controller.Tracker.Quality+"|"+string.Join(",",controller.Catalog.Pets.Keys.Select(id=>Total(id)));if(key==fingerprint)return;fingerprint=key;Rebuild();};
        Shown+=(_,_)=>refresh.Start();FormClosed+=(_,_)=>refresh.Stop();PremiumDialogMotion.Attach(this);
    }
    private int SelectedWorld=>int.TryParse(Key(regions),out var w)?w:-1;
    private void Rebuild()
    {
        var previous=selectedId;building=true;
        try
        {
            kinds.Enabled=specialMode;single.ForeColor=specialMode?PremiumTheme.Muted:RiftVisual.Cyan;special.ForeColor=specialMode?RiftVisual.Violet:PremiumTheme.Muted;
            note.Text=M(specialMode?"routeNote":"guideNote");
            matches=PetGuideQuery.Filter(controller.Catalog,data,sources,SelectedWorld,Key(progress)=="max",Key(genera),specialMode,Key(kinds),search.Text,Total);
            list.RowCount=matches.Length;list.Invalidate();empty.Visible=matches.Length==0;empty.BringToFront();if(matches.Length>0)list.BringToFront();details.BringToFront();
            count.Text=matches.Length+" · "+(specialMode?M("specialList"):M("exclusiveList"));
            var index=Array.FindIndex(matches,p=>p.Id==previous);if(index<0&&matches.Length>0)index=0;
            if(index>=0){list.CurrentCell=list.Rows[index].Cells[1];ShowCard(matches[index].Id);}else{card?.Dispose();card=null;selectedId=0;}
        }
        finally{building=false;}
    }
    private void ShowCard(int id)
    {
        card?.Dispose();selectedId=id;var pet=controller.Catalog.Pets[id];
        card=new(pet,portraits.GetValueOrDefault(id),data,sources.Get(id),Total(id),controller.Tracker.Quality==DataQuality.Exact,language,()=>{card?.Dispose();card=null;},w=>focus(id,w),anchor=>
        {
            worldsMenu.Items.Clear();foreach(var world in data.WorldsForPet(id)){var w=world;worldsMenu.Items.Add(data.Worlds[w].Name,null,(_,_)=>focus(id,w));}worldsMenu.Show(anchor,new Point(0,anchor.Height));
        },controller.Tracker.HasCurrent?()=>add(id):null){Dock=DockStyle.Fill};
        details.Controls.Add(card);
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);var dark=1;DwmSetWindowAttribute(Handle,20,ref dark,sizeof(int));
    }
    [DllImport("dwmapi.dll")]private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    protected override void Dispose(bool disposing){if(disposing){refresh.Dispose();worldsMenu.Dispose();Icon?.Dispose();}base.Dispose(disposing);}
}
