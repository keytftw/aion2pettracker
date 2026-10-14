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
    private readonly PremiumButton close,map,source;
    private float DpiFactor=>DeviceDpi/96f;
    public PetDetailCard(PetDefinition pet,Image? portrait,MapData data,PetSourceInfo? info,long? total,bool exact,string language,Action hide,Action<int> focusWorld,Action<Control> chooseWorld)
    {
        this.pet=pet;this.portrait=portrait;this.language=language;DoubleBuffered=true;BackColor=PremiumTheme.Panel;
        var mapped=data.WorldsForPet(pet.Id).Count>0;
        status=PetCardText.Get(language,mapped?"field":info?.HasMonsterSource==true?"monsters":pet.Genus=="Special"&&info?.Verified==true?"special":"unknown");
        progress=total is null?"—":(exact?"":PetCardText.Get(language,"last")+" · ")+total+" / 105";
        close=new(){Text="×",Size=new(25,25)};close.Click+=(_,_)=>hide();
        var worlds=data.WorldsForPet(pet.Id);
        map=new(){Text=PetCardText.Get(language,"map")+(worlds.Count>1?" ▾":""),TextLanguage=language,Enabled=mapped};
        map.Click+=(_,_)=>{if(worlds.Count>1)chooseWorld(map);else if(worlds.Count==1)focusWorld(worlds[0]);};
        source=new(){Text=PetCardText.Get(language,"wiki"),TextLanguage=language,Enabled=info?.Verified==true};
        source.Click+=(_,_)=>{if(info?.Source is {} url)Process.Start(new ProcessStartInfo(url){UseShellExecute=true});};
        Controls.AddRange([sources,close,map,source]);
        sources.HandleCreated+=(_,_)=>SetWindowTheme(sources.Handle,"DarkMode_Explorer",null);
        void Row(string text,Color color)
        {
            var label=new Label{Text=text,ForeColor=color,BackColor=PremiumTheme.Panel,Font=new(UiText.FontFamily(language),10),AutoSize=false,Margin=new(0,0,0,10)};
            label.Tag=text;sources.Controls.Add(label);
        }
        Row(pet.Genus+" · "+progress,PremiumTheme.Gold);Row(status,PremiumTheme.Ink);
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
        sources.Bounds=new((int)(14*s),(int)(76*s),Width-(int)(28*s),Math.Max((int)(20*s),Height-(int)(125*s)));
        foreach(Control row in sources.Controls)
        {
            row.Width=Math.Max(100,sources.Width-(int)(22*s));
            row.Height=TextRenderer.MeasureText(row.Text,row.Font,new(row.Width,0),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height+(int)(5*s);
        }
        var y=Height-(int)(40*s);map.Bounds=new((int)(14*s),y,(Width-(int)(36*s))/2,(int)(29*s));source.Bounds=new(map.Right+(int)(8*s),y,map.Width,map.Height);Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.ScaleTransform(DpiFactor,DpiFactor);g.SmoothingMode=SmoothingMode.AntiAlias;var w=Width/DpiFactor;var h=Height/DpiFactor;
        using var edge=new Pen(PremiumTheme.Gold,2);g.DrawRectangle(edge,1,1,w-3,h-3);
        using var inner=new Pen(Color.FromArgb(65,PremiumTheme.Gold));g.DrawRectangle(inner,6,6,w-13,h-13);
        using var header=new LinearGradientBrush(new RectangleF(7,7,w-14,67),Color.FromArgb(35,31,38),PremiumTheme.Panel,90f);g.FillRectangle(header,7,7,w-14,67);
        PremiumTheme.Text(g,"PET COLLECTION · "+pet.Id,new(14,10,w-65,17),7,PremiumTheme.Gold,"en");
        PremiumTheme.Text(g,pet.Name,new(14,29,w-82,40),13,PremiumTheme.Ink,language,FontStyle.Bold);
        if(portrait is not null){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(portrait,w-64,29,46,46);}
        using var line=new Pen(Color.FromArgb(65,PremiumTheme.Gold));g.DrawLine(line,14,74,w-14,74);
    }
    [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)] private static extern int SetWindowTheme(IntPtr window,string theme,string? subId);
}
