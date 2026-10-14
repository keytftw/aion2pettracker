using System.Drawing.Drawing2D;
namespace PetTracker.App;

public sealed class HandbookForm:Form
{
    private readonly string language;
    private readonly string[] sections=["identity","cards","atlas","keys","rift"];
    private readonly Dictionary<string,Image> photos=[];
    private readonly PremiumButton previous,next;
    private int page;
    public HandbookForm(string language)
    {
        this.language=language;AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new(96,96);
        ClientSize=new(580,600);FormBorderStyle=FormBorderStyle.SizableToolWindow;MinimumSize=new(520,540);
        Text=HelpText.Get(language,"handbook");BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;StartPosition=FormStartPosition.CenterParent;
        DoubleBuffered=true;Icon=AppBrand.CreateIcon();
        foreach(var name in new[]{"tracker","atlas","settings","compact","uncertain"})
        {
            using var stream=typeof(HandbookForm).Assembly.GetManifestResourceStream("Handbook."+name+".png");
            if(stream is not null){using var loaded=Image.FromStream(stream);photos[name]=new Bitmap(loaded);}
        }
        previous=new(){Text="‹",TextSize=18,DpiAwareText=true,AccessibleName="Previous"};next=new(){Text="›",TextSize=18,DpiAwareText=true,AccessibleName="Next"};
        previous.Click+=(_,_)=>SelectPage(page-1);next.Click+=(_,_)=>SelectPage(page+1);
        var guide=new PremiumButton{Text=HelpText.Get(language,"guide"),DpiAwareText=true,TextSize=10,TextLanguage=language};
        var contact=new PremiumButton{Text=AppLinks.Email,DpiAwareText=true,TextSize=10,TextLanguage=language};
        guide.Click+=(_,_)=>AppLinks.Open(this,AppLinks.Guide,language);contact.Click+=(_,_)=>AppLinks.Open(this,"mailto:"+AppLinks.Email,language);
        Controls.AddRange([previous,next,guide,contact]);
        void LayoutButtons(){var s=DeviceDpi/96f;var w=ClientSize.Width;var h=ClientSize.Height;previous.Bounds=new((int)(20*s),h-(int)(49*s),(int)(35*s),(int)(30*s));next.Bounds=new(w-(int)(55*s),previous.Top,previous.Width,previous.Height);guide.Bounds=new((int)(65*s),previous.Top,(w-(int)(140*s))/2,previous.Height);contact.Bounds=new(guide.Right+(int)(10*s),previous.Top,guide.Width,previous.Height);Invalidate();}
        Resize+=(_,_)=>LayoutButtons();LayoutButtons();SelectPage(0);
    }
    public void SelectPage(int index){page=Math.Clamp(index,0,sections.Length-1);previous.Enabled=page>0;next.Enabled=page<sections.Length-1;Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;var scale=DeviceDpi/96f;g.ScaleTransform(scale,scale);g.InterpolationMode=InterpolationMode.HighQualityBicubic;
        var w=ClientSize.Width/scale;var h=ClientSize.Height/scale;var key=sections[page];
        PremiumTheme.Text(g,HelpText.Get(language,key+"Title"),new(24,20,w-100,35),20,PremiumTheme.Gold,language);
        PremiumTheme.Text(g,$"{page+1} / {sections.Length}",new(w-75,25,50,25),10,PremiumTheme.Muted,language);
        var photo=page==0?"uncertain":page==2?"atlas":page==3?"compact":page==4?"settings":"tracker";
        var area=new RectangleF(24,75,w-48,h-275);
        if(photos.TryGetValue(photo,out var image))
        {var fit=Math.Min(area.Width/image.Width,area.Height/image.Height);var iw=image.Width*fit;var ih=image.Height*fit;g.DrawImage(image,area.X+(area.Width-iw)/2,area.Y+(area.Height-ih)/2,iw,ih);}
        PremiumTheme.Text(g,HelpText.Get(language,key),new(24,h-177,w-48,112),11,PremiumTheme.Ink,language);
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData)
    {if(keyData==Keys.Escape){Close();return true;}if(keyData==Keys.Right){SelectPage(page+1);return true;}if(keyData==Keys.Left){SelectPage(page-1);return true;}return base.ProcessCmdKey(ref msg,keyData);}
    public void ExportPreview(string path){PremiumDialogMotion.ShowForPreview(this);using var bitmap=new Bitmap(Width,Height);DrawToBitmap(bitmap,new(0,0,Width,Height));bitmap.Save(path);Hide();}
    protected override void Dispose(bool disposing){if(disposing){foreach(var image in photos.Values)image.Dispose();Icon?.Dispose();}base.Dispose(disposing);}
}
