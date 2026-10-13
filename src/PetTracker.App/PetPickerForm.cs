using System.Drawing.Drawing2D;
using PetTracker.Core;

namespace PetTracker.App;

public sealed class PetPickerForm : Form
{
    private readonly PetPickerList list;
    private readonly Action<bool> selectTab;
    public PetPickerForm(PetCatalog catalog,IReadOnlyDictionary<int,Image> images,string language,Cursor cursor,Action<int> pin,Func<int,long?>? knownTotal=null)
    {
        Text="Aion 2 Pet Tracker · "+UiText.Get(language,"add");ClientSize=new(352,440);BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;
        FormBorderStyle=FormBorderStyle.None;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;TopMost=true;ShowInTaskbar=false;Cursor=cursor;
        Font=new(UiText.FontFamily(language),9);
        var title=new Label { Text=UiText.Get(language,"add"),Font=new(UiText.FontFamily(language),13,FontStyle.Bold),ForeColor=PremiumTheme.Gold,Location=new(17,14),AutoSize=true };
        var searchFrame=new Panel { Location=new(14,52),Size=new(324,36),BackColor=PremiumTheme.Panel };
        var search=new TextBox { BorderStyle=BorderStyle.None,BackColor=PremiumTheme.Panel,ForeColor=PremiumTheme.Ink,Font=new(UiText.FontFamily(language),9),Location=new(36,9),Width=275,PlaceholderText=UiText.Get(language,"search"),Cursor=cursor };
        searchFrame.Paint+=(_,e)=>{e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using var p=new Pen(PremiumTheme.Muted,1.3f);e.Graphics.DrawEllipse(p,12,11,9,9);e.Graphics.DrawLine(p,20,19,25,24);};
        if(language=="ar")search.RightToLeft=RightToLeft.Yes;
        searchFrame.Controls.Add(search);
        var maxed=false;
        var incompleteTab=new PremiumButton{Text=UiText.Get(language,"incompletePets"),TextLanguage=language,Location=new(14,99),Size=new(159,34),Cursor=cursor};
        var maxedTab=new PremiumButton{Text=UiText.Get(language,"maxedPets"),TextLanguage=language,Location=new(179,99),Size=new(159,34),Cursor=cursor};
        list=new(catalog,images,language,knownTotal){Location=new(14,145),Size=new(324,243),Cursor=cursor};
        var count=new Label { Location=new(17,405),AutoSize=true,ForeColor=PremiumTheme.Muted,Font=new(UiText.FontFamily(language),8) };
        var close=new PremiumButton { Text=UiText.Get(language,"done"),TextLanguage=language,Location=new(241,398),Size=new(96,29),Cursor=cursor };
        close.Click+=(_,_)=>Close();
        var x=new PremiumButton {Text="×",Location=new(316,12),Size=new(22,23),Cursor=cursor};x.Click+=(_,_)=>Close();
        list.PinRequested+=id=>{pin(id);Close();};
        void Filter(){list.Filter(search.Text,maxed);count.Text=UiText.Get(language,"pets",list.Count);incompleteTab.ForeColor=maxed?PremiumTheme.Muted:PremiumTheme.Gold;maxedTab.ForeColor=maxed?PremiumTheme.Gold:PremiumTheme.Muted;incompleteTab.Invalidate();maxedTab.Invalidate();Invalidate();}
        selectTab=value=>{maxed=value;Filter();};
        incompleteTab.Click+=(_,_)=>selectTab(false);maxedTab.Click+=(_,_)=>selectTab(true);
        Paint+=(_,e)=>{using var selected=new Pen(PremiumTheme.Gold,2);var tab=maxed?maxedTab:incompleteTab;e.Graphics.DrawLine(selected,tab.Left+12,tab.Bottom+3,tab.Right-12,tab.Bottom+3);};
        search.TextChanged+=(_,_)=>Filter();Controls.AddRange([title,searchFrame,incompleteTab,maxedTab,list,count,close,x]);Filter();PremiumDialogMotion.Attach(this);
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var p=new Pen(Color.FromArgb(100,PremiumTheme.Gold));e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
    protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==Keys.Escape){Close();return true;}return base.ProcessCmdKey(ref message,key);}
    public void ExportPreview(string path,bool maxed=false){selectTab(maxed);PremiumDialogMotion.ShowForPreview(this);using var bitmap=new Bitmap(Width,Height);DrawToBitmap(bitmap,new(0,0,Width,Height));bitmap.Save(path);Hide();}
}

public sealed class PetPickerList : Control
{
    private readonly PetCatalog catalog;private readonly IReadOnlyDictionary<int,Image> images;private readonly string language;
    private readonly Func<int,long?> knownTotal;
    private PetDefinition[] matches=[];private readonly System.Windows.Forms.Timer timer=new(){Interval=16};
    private float scroll,target;private bool dragging;private int selected=-1;
    public int Count=>matches.Length;
    public event Action<int>? PinRequested;
    private const int Stride=56;
    public PetPickerList(PetCatalog catalog,IReadOnlyDictionary<int,Image> images,string language,Func<int,long?>? knownTotal=null)
    {
        this.catalog=catalog;this.images=images;this.language=language;
        this.knownTotal=knownTotal??(_=>null);
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=PremiumTheme.Background;
        timer.Tick+=(_,_)=>{scroll+=(target-scroll)*.23f;if(Math.Abs(target-scroll)<.1f){scroll=target;timer.Stop();}Invalidate();};
    }
    public void Filter(string query,bool maxed=false)
    {
        matches=PetPickerQuery.Filter(catalog,query,maxed,knownTotal);
        target=0;scroll=Math.Clamp(scroll,0,MaxScroll);selected=-1;timer.Start();Invalidate();
    }
    private float MaxScroll=>Math.Max(0,matches.Length*Stride-Height);
    protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);target=Math.Clamp(target-e.Delta/120f*82,0,MaxScroll);timer.Start();}
    private void DragScroll(int y){target=Math.Clamp(y/(float)Math.Max(1,Height),0,1)*MaxScroll;timer.Start();}
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;
        if(e.X>Width-18&&MaxScroll>0){dragging=true;Capture=true;DragScroll(e.Y);return;}
        var i=(int)((e.Y+scroll)/Stride);if(i>=0&&i<matches.Length)PinRequested?.Invoke(matches[i].Id);
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging)DragScroll(e.Y);var next=(int)((e.Y+scroll)/Stride);if(next!=selected){selected=next;Invalidate();}}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);selected=-1;Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        if(matches.Length==0){PremiumTheme.Text(g,UiText.Get(language,"noResults"),ClientRectangle,10,PremiumTheme.Muted,language,align:StringAlignment.Center);return;}
        for(var i=Math.Max(0,(int)(scroll/Stride));i<matches.Length;i++)
        {
            var y=i*Stride-scroll;if(y>Height)break;var pet=matches[i];var box=new RectangleF(1,y+1,Width-15,50);using var shape=PremiumTheme.Round(box,6);
            using var fill=new SolidBrush(i==selected?Color.FromArgb(38,34,43):PremiumTheme.Panel);g.FillPath(fill,shape);
            using var border=new Pen(i==selected?Color.FromArgb(140,PremiumTheme.Gold):Color.FromArgb(36,42,54));g.DrawPath(border,shape);
            if(images.TryGetValue(pet.Id,out var image))g.DrawImage(image,new RectangleF(9,y+8,35,35));
            PremiumTheme.Text(g,pet.Name,new(55,y+4,Width-94,27),9.2f,PremiumTheme.Ink,style:FontStyle.Bold);
            PremiumTheme.Text(g,pet.Genus,new(55,y+29,Width-95,16),8,PremiumTheme.Muted);
            PremiumTheme.Text(g,"+",new(Width-44,y+13,20,22),16,PremiumTheme.Gold,align:StringAlignment.Center);
        }
        if(MaxScroll>0)
        {
            using var track=new SolidBrush(Color.FromArgb(30,36,45));g.FillRectangle(track,Width-7,2,4,Height-4);
            var h=Math.Max(24,Height*(float)Height/(matches.Length*Stride));var y=scroll/MaxScroll*(Height-h);
            using var thumb=new LinearGradientBrush(new RectangleF(Width-8,y,5,h),PremiumTheme.Gold,Color.FromArgb(156,101,160),90f);g.FillRectangle(thumb,Width-8,y,5,h);
        }
    }
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
