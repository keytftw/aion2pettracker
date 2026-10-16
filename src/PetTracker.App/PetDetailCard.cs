using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
namespace PetTracker.App;

public sealed class PetDetailCard : Panel
{
    private readonly PetDefinition pet;
    private readonly Image? portrait;
    private readonly string language,status,progress;
    private readonly FlowLayoutPanel sources=new(){AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=PremiumTheme.Panel};
    private readonly PremiumButton close,map,source,add;
    private readonly System.Windows.Forms.Timer entrance=new(){Interval=16};
    private readonly Stopwatch entranceClock=new();
    private readonly Color accent=Color.FromArgb(136,202,220);
    private Bitmap? portraitTile;
    private float reveal=1;
    private float DpiFactor=>DeviceDpi/96f;
    public PetDetailCard(PetDefinition pet,Image? portrait,MapData data,PetSourceInfo? info,long? total,bool exact,string language,Action hide,Action<int> focusWorld,Action<Control> chooseWorld,Func<bool>? addToTracker=null)
    {
        this.pet=pet;this.portrait=portrait;this.language=language;DoubleBuffered=true;BackColor=PremiumTheme.Panel;
        if(portrait is not null)
        {
            portraitTile=new Bitmap(96,96,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using var graphics=Graphics.FromImage(portraitTile);graphics.SmoothingMode=SmoothingMode.AntiAlias;graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
            using var circle=new GraphicsPath();circle.AddEllipse(2,2,92,92);graphics.SetClip(circle);graphics.DrawImage(portrait,new Rectangle(2,2,92,92));
        }
        entrance.Tick+=(_,_)=>{var t=Math.Clamp(entranceClock.Elapsed.TotalSeconds/.36,0,1);reveal=(float)(1-Math.Pow(1-t,3));Invalidate(new Rectangle(0,0,Width,(int)(80*DpiFactor)));if(t>=1)entrance.Stop();};
        var mapped=data.WorldsForPet(pet.Id).Count>0;
        status=PetCardText.Get(language,mapped?"field":info?.HasMonsterSource==true?"monsters":pet.Genus=="Special"&&info?.Verified==true?"special":"unknown");
        progress=total is null?"—":(exact?"":PetCardText.Get(language,"last")+" · ")+total+" / 105";
        close=new(){Text="×",Size=new(25,25)};close.Click+=(_,_)=>hide();
        add=new(){Text="+",TextSize=17,DpiAwareText=true,BackColor=Color.FromArgb(22,81,60),ForeColor=Color.FromArgb(134,255,196),AccentColor=Color.FromArgb(75,242,159),AccessibleName=UiText.Get(language,"add")+" · "+pet.Name,Cursor=Cursors.Hand,Enabled=addToTracker is not null};
        add.Click+=(_,_)=>{if(addToTracker?.Invoke()==true)add.Text="✓";};
        var worlds=data.WorldsForPet(pet.Id);
        map=new(){Text=PetCardText.Get(language,"map")+(worlds.Count>1?" ▾":""),TextLanguage=language,Enabled=mapped};
        map.Click+=(_,_)=>{if(worlds.Count>1)chooseWorld(map);else if(worlds.Count==1)focusWorld(worlds[0]);};
        source=new(){Text=PetCardText.Get(language,"wiki"),TextLanguage=language,Enabled=info?.Verified==true};
        source.Click+=(_,_)=>{if(info?.Source is {} url)Process.Start(new ProcessStartInfo(url){UseShellExecute=true});};
        Controls.AddRange([sources,close,add,map,source]);
        sources.HandleCreated+=(_,_)=>SetWindowTheme(sources.Handle,"DarkMode_Explorer",null);
        void Row(string text,Color color)
        {
            var section=text==PetCardText.Get(language,"sources");
            var label=new Label{Text=text,ForeColor=section?accent:color,BackColor=text.StartsWith("•")?Color.FromArgb(27,32,41):PremiumTheme.Panel,Font=new(UiText.FontFamily(language),section?9:10,section?FontStyle.Bold:FontStyle.Regular),AutoSize=false,Padding=new(6,4,5,4),Margin=new(0,0,0,6)};
            label.Tag=text;sources.Controls.Add(label);
        }
        Row(pet.Genus+" · "+progress,PremiumTheme.Gold);Row(status,PremiumTheme.Ink);
        if(info?.Context is {} context){Row(MapText.Get(language,info.Kind!)+" · "+context,accent);Row(MapText.Get(language,"routeNote"),PremiumTheme.Muted);}
        Row(PetCardText.Get(language,"sources"),PremiumTheme.Gold);
        if(mapped)
        {
            Row(PetCardText.Get(language,"regions")+": "+string.Join(", ",data.WorldsForPet(pet.Id).Select(id=>data.Worlds[id].Name)),PremiumTheme.Gold);
            foreach(var monster in data.Worlds.Keys.SelectMany(w=>data.Locations(pet.Id,w)).Select(p=>p.NpcName).Distinct().Take(6))Row("• "+monster,PremiumTheme.Ink);
        }
        if(info?.Verified==true)
        {foreach(var fact in info.Facts??[])Row("• "+fact,PremiumTheme.Ink);Row("Wiki · "+info.Checked,PremiumTheme.Muted);}
        else Row(PetCardText.Get(language,"missing"),PremiumTheme.Muted);
        Resize+=(_,_)=>LayoutCard();
    }
    private void LayoutCard()
    {
        var s=DpiFactor;close.Bounds=new(Width-(int)(36*s),(int)(8*s),(int)(22*s),(int)(22*s));
        add.Bounds=new(Width-(int)(65*s),(int)(8*s),(int)(25*s),(int)(22*s));
        sources.Bounds=new((int)(14*s),(int)(76*s),Width-(int)(28*s),Math.Max((int)(20*s),Height-(int)(125*s)));
        foreach(Control row in sources.Controls)
        {
            row.Width=Math.Max(100,sources.Width-(int)(22*s));
            row.Height=TextRenderer.MeasureText(row.Text,row.Font,new(Math.Max(30,row.Width-row.Padding.Horizontal),0),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height+row.Padding.Vertical+(int)(3*s);
        }
        var y=Height-(int)(40*s);map.Bounds=new((int)(14*s),y,(Width-(int)(36*s))/2,(int)(29*s));source.Bounds=new(map.Right+(int)(8*s),y,map.Width,map.Height);Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.ScaleTransform(DpiFactor,DpiFactor);g.SmoothingMode=SmoothingMode.AntiAlias;var w=Width/DpiFactor;var h=Height/DpiFactor;
        using var edge=new Pen(Color.FromArgb(100,accent),1.2f);g.DrawRectangle(edge,1,1,w-3,h-3);
        using var header=new LinearGradientBrush(new RectangleF(2,2,w-4,72),Color.FromArgb(32,40,54),PremiumTheme.Panel,20f);g.FillRectangle(header,2,2,w-4,72);
        using var accentLine=new Pen(Color.FromArgb((int)(90+reveal*130),accent),2);g.DrawLine(accentLine,2,2,2+(w-4)*reveal,2);
        if(reveal<1){using var sheen=new SolidBrush(Color.FromArgb((int)(35*(1-reveal)),accent));g.FillRectangle(sheen,2,2,(w-4)*reveal,72);}
        PremiumTheme.Text(g,"PET COLLECTION · "+pet.Id,new(14,10,w-85,17),7,PremiumTheme.Gold,"en");
        PremiumTheme.Text(g,pet.Name,new(14,29,w-96,43),12.5f,PremiumTheme.Ink,language,FontStyle.Bold);
        if(portraitTile is not null){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(portraitTile,w-65,29,46,46);using var portraitEdge=new Pen(Color.FromArgb(125,accent));g.DrawEllipse(portraitEdge,w-64,30,44,44);}
        using var line=new Pen(Color.FromArgb(65,PremiumTheme.Gold));g.DrawLine(line,14,74,w-14,74);
    }
    protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);if(Visible){reveal=0;entranceClock.Restart();entrance.Start();}else entrance.Stop();}
    protected override void Dispose(bool disposing){if(disposing){entrance.Dispose();portraitTile?.Dispose();}base.Dispose(disposing);}
    [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)] private static extern int SetWindowTheme(IntPtr window,string theme,string? subId);
}
