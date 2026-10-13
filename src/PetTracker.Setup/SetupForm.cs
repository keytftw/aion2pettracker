using System.Diagnostics;
using System.Drawing.Drawing2D;
using PetTracker.App;
using PetTracker.Core;

namespace PetTracker.Setup;
public sealed class SetupForm : Form
{
    public const string Website="https://aion2-pet-tracker.lovelymacaw4.chatgpt.site";
    public const string Patreon="https://www.patreon.com/cw/keytftw";
    private readonly InstallationGate gate=new();
    private readonly OverlayPreferences preferences;
    private readonly Panel content=new(){Location=new(265,130),Size=new(510,405),BackColor=PremiumTheme.Background};
    private readonly System.Windows.Forms.Timer animation=new(){Interval=16};
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly CrystalCursor cursor=new();
    private PremiumButton? back,next;
    private readonly PremiumButton close;
    private string language="en";
    private bool dragging,busy;
    private string installDirectory=InstallEngine.InstalledRoot;
    private bool desktopShortcut=InstallationProfile.Load(InstallationProfile.ReceiptPath)?.DesktopShortcut??true;
    private Point dragOffset;
    private string PreferencesPath=>Path.Combine(InstallationProfile.DataFolder,"Live","overlay-preferences.json");
    private string T(string key)=>InstallerText.Get(language,key);
    private string U(string key)=>UiText.Get(language,key);
    private Rectangle Scaled(Rectangle rectangle)
    {
        var scale=DeviceDpi/96f;
        return new((int)Math.Round(rectangle.X*scale),(int)Math.Round(rectangle.Y*scale),(int)Math.Round(rectangle.Width*scale),(int)Math.Round(rectangle.Height*scale));
    }

    public SetupForm()
    {
        AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new(96,96);FormBorderStyle=FormBorderStyle.None;ClientSize=new(820,640);
        StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;
        Text="Aion 2 Pet Tracker Setup";Icon=AppBrand.CreateIcon();Cursor=cursor.Pointer;
        preferences=OverlayPreferences.Load(PreferencesPath,_=>{});
        Controls.Add(content);
        close=Button("×",new(770,18,28,28),()=>Close());close.ForeColor=PremiumTheme.Muted;
        animation.Tick+=(_,_)=>Invalidate();animation.Start();
        FormClosing+=(_,e)=>{if(busy)e.Cancel=true;};
        PremiumDialogMotion.Attach(this);RenderStep();
    }
    private PremiumButton Button(string text,Rectangle bounds,Action action,Control? parent=null)
    {
        var button=new PremiumButton{Text=text,TextLanguage=language,Bounds=Scaled(bounds),Cursor=cursor.Pointer,AccessibleName=text};
        button.Click+=(_,_)=>{if(button.Enabled)action();};(parent??this).Controls.Add(button);return button;
    }
    private Label Label(string text,Rectangle bounds,float size=10)
    {
        var label=new Label{Text=text,Bounds=Scaled(bounds),Font=new(UiText.FontFamily(language),size),ForeColor=PremiumTheme.Muted,BackColor=PremiumTheme.Background,AutoSize=false,RightToLeft=language=="ar"?RightToLeft.Yes:RightToLeft.No};
        content.Controls.Add(label);label.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left)BeginDrag(label.PointToScreen(e.Location));};return label;
    }
    private void RenderStep()
    {
        content.Bounds=Scaled(new(265,130,510,405));
        foreach(var item in content.Controls.Cast<Control>().ToArray()){content.Controls.Remove(item);item.Dispose();}
        foreach(var item in Controls.OfType<PremiumButton>().Where(b=>b!=close).ToArray()){Controls.Remove(item);item.Dispose();}
        back=next=null;
        Button(T("website"),new(26,505,180,33),()=>OpenLink(Website));
        Button(U("support"),new(26,547,180,33),()=>OpenLink(Patreon));
        close.Enabled=!busy;
        if(gate.Stage==InstallationStage.Language)
        {
            for(var i=0;i<UiText.Languages.Length;i++)
            {
                var entry=UiText.Languages[i];
                var button=Button(entry.Name,new((i%2)*260,(i/2)*73,248,59),()=>{gate.SelectLanguage(entry.Code);language=entry.Code;preferences.Language=language;RenderStep();},content);
                button.TextLanguage=entry.Code;
            }
        }
        else if(gate.Stage==InstallationStage.Terms)
        {
            var document=new TermsView{Bounds=Scaled(new(0,0,510,323)),Text=TermsContent.Get(language),Font=new(UiText.FontFamily(language),10),RightToLeft=language=="ar"?RightToLeft.Yes:RightToLeft.No,AccessibleName=StartupText.Get(language,"terms")};
            var consent=new CheckBox{Bounds=Scaled(new(0,340,505,54)),Text=StartupText.Get(language,"consent"),Font=new(UiText.FontFamily(language),10),ForeColor=PremiumTheme.Ink,BackColor=PremiumTheme.Background,Cursor=cursor.Pointer,Checked=false,RightToLeft=language=="ar"?RightToLeft.Yes:RightToLeft.No};
            content.Controls.AddRange([document,consent]);
            back=Button(T("back"),new(265,550,145,38),()=>{gate.Back();RenderStep();});
            next=Button(StartupText.Get(language,"accept"),new(426,550,349,38),()=>{if(gate.Accept(consent.Checked))RenderStep();});
            next.Enabled=false;next.ForeColor=PremiumTheme.Muted;
            consent.CheckedChanged+=(_,_)=>{next.Enabled=consent.Checked;next.ForeColor=consent.Checked?PremiumTheme.Gold:PremiumTheme.Muted;next.Invalidate();};
        }
        else if(gate.Stage==InstallationStage.Location)
        {
            Label(T("locationHint"),new(0,0,505,70));
            Label(T("folder"),new(0,95,505,30));
            var path=new TextBox{Text=installDirectory,Bounds=Scaled(new(0,133,505,34)),Font=new(UiText.FontFamily(language),11),BackColor=PremiumTheme.Panel,ForeColor=PremiumTheme.Ink,BorderStyle=BorderStyle.FixedSingle};
            path.TextChanged+=(_,_)=>installDirectory=path.Text;content.Controls.Add(path);
            Button(T("browse"),new(0,183,185,36),()=>
            {
                using var picker=new FolderBrowserDialog{Description=T("folder"),UseDescriptionForTitle=true,SelectedPath=Directory.Exists(installDirectory)?installDirectory:Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)};
                if(picker.ShowDialog(this)==DialogResult.OK)path.Text=Path.GetFileName(picker.SelectedPath).Equals("A2PetTracker",StringComparison.OrdinalIgnoreCase)?picker.SelectedPath:Path.Combine(picker.SelectedPath,"A2PetTracker");
            },content);
            var shortcut=Option(T("desktop"),new(0,255,505,50),desktopShortcut);shortcut.CheckedChanged+=(_,_)=>desktopShortcut=shortcut.Checked;
            Label(T("npcap"),new(0,345,505,53),9);
            back=Button(T("back"),new(265,550,145,38),()=>{gate.Back();RenderStep();});
            next=Button(T("install"),new(426,550,349,38),()=>_ = Install());
        }
        else if(gate.Stage==InstallationStage.Installing)
            Label(T("locationHint"),new(0,200,505,110),11);
        else
        {
            Label(T("readyHint"),new(0,95,505,140),12);
            Label(T("npcap"),new(0,280,505,65),9);
            Button(U("close"),new(265,550,145,38),()=>Close());
            Button(T("launch"),new(426,550,349,38),()=>{try{InstallEngine.Launch();Close();}catch(Exception ex){MessageBox.Show(this,ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);}});
        }
        Invalidate();
    }
    private CheckBox Option(string text,Rectangle bounds,bool value)
    {
        var checkbox=new CheckBox{Text=text,Bounds=Scaled(bounds),Checked=value,Font=new(UiText.FontFamily(language),10),ForeColor=PremiumTheme.Ink,BackColor=PremiumTheme.Background,Cursor=cursor.Pointer};content.Controls.Add(checkbox);return checkbox;
    }
    private async Task Install()
    {
        if(busy)return;
        var running=Process.GetProcessesByName("A2PetTracker");var open=running.Length>0;foreach(var process in running)process.Dispose();
        if(open){MessageBox.Show(this,T("closeApp"),Text,MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        try{installDirectory=InstallationLocation.Normalize(installDirectory);InstallEngine.ValidateDestination(installDirectory);}
        catch(Exception ex){MessageBox.Show(this,T("failure")+"\n"+ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);return;}
        gate.BeginInstall();busy=true;RenderStep();
        try
        {
            await InstallEngine.InstallAsync(installDirectory,desktopShortcut);
            preferences.Language=language;preferences.Save(PreferencesPath);
            gate.Complete(true);(gate.CreateReceipt(TermsContent.Version,DateTimeOffset.UtcNow) with {InstallDirectory=installDirectory,DesktopShortcut=desktopShortcut}).Save(InstallationProfile.ReceiptPath);
            busy=false;RenderStep();
        }
        catch(Exception ex)
        {
            if(gate.Stage==InstallationStage.Installing)gate.Complete(false);
            busy=false;
            // If preference/receipt persistence failed after installing, require a new explicit setup attempt.
            if(gate.Stage==InstallationStage.Finished){MessageBox.Show(this,T("failure")+"\n"+ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);Close();return;}
            RenderStep();MessageBox.Show(this,T("failure")+"\n"+ex.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
    }
    private void OpenLink(string url)
    {try{using var browser=Process.Start(new ProcessStartInfo(url){UseShellExecute=true});}catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException){MessageBox.Show(this,U("browserFailed"),Text,MessageBoxButtons.OK,MessageBoxIcon.Information);}}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.ScaleTransform(DeviceDpi/96f,DeviceDpi/96f);g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var background=new LinearGradientBrush(new Rectangle(0,0,232,640),Color.FromArgb(32,25,38),Color.FromArgb(12,18,25),65))g.FillRectangle(background,0,0,232,640);
        using(var line=new Pen(Color.FromArgb(70,PremiumTheme.Gold))){g.DrawLine(line,232,0,232,640);g.DrawRectangle(line,1,1,818,638);}
        using(var emblem=AppBrand.Draw(90))g.DrawImage(emblem,68,40,90,90);
        PremiumTheme.Text(g,"Aion 2\nPet Tracker",new(20,142,190,76),19,PremiumTheme.Gold,"en",FontStyle.Bold,StringAlignment.Center,heading:true);
        var stages=new[]{"Select Language",StartupText.Get(language,"terms"),T("location"),T("finished")};
        var step=gate.Stage switch{InstallationStage.Language=>0,InstallationStage.Terms=>1,InstallationStage.Location=>2,_=>3};
        for(var i=0;i<4;i++)
        {
            var y=275+i*47;using var edge=new Pen(Color.FromArgb(i==step?180:45,PremiumTheme.Gold));g.DrawEllipse(edge,25,y,23,23);
            PremiumTheme.Text(g,(i+1).ToString(),new(25,y,23,23),8,PremiumTheme.Gold,"en",align:StringAlignment.Center);
            PremiumTheme.Text(g,stages[i],new(60,y-2,151,32),9,i==step?PremiumTheme.Ink:PremiumTheme.Muted,language);
        }
        var title=gate.Stage switch{InstallationStage.Language=>"Select Language",InstallationStage.Terms=>StartupText.Get(language,"terms"),InstallationStage.Location=>T("location"),InstallationStage.Installing=>T("installing"),_=>T("finished")};
        PremiumTheme.Text(g,title,new(265,57,505,59),22,PremiumTheme.Gold,language,FontStyle.Bold);
        if(gate.Stage==InstallationStage.Installing)
        {
            var seconds=clock.Elapsed.TotalSeconds;
            using var pen=new Pen(Color.FromArgb(155,PremiumTheme.Gold),3);g.DrawArc(pen,472,190,76,76,(float)(seconds*100),245);
            using var track=new SolidBrush(Color.FromArgb(37,39,48));g.FillRectangle(track,280,485,470,6);
            var state=g.Save();g.SetClip(new Rectangle(280,485,470,6));var x=160+(float)((seconds*.4)%1)*670;
            using var shine=new LinearGradientBrush(new RectangleF(x,485,140,6),Color.Transparent,PremiumTheme.Gold,0f);g.FillRectangle(shine,x,485,140,6);g.Restore(state);
        }
        PremiumTheme.Text(g,"Designed & Developed by Keyt",new(245,610,555,15),7,PremiumTheme.Muted,"en",align:StringAlignment.Center);
    }
    private void BeginDrag(Point point){dragOffset=new(point.X-Left,point.Y-Top);dragging=true;Capture=true;Cursor=Cursors.SizeAll;}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left)BeginDrag(PointToScreen(e.Location));}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){var point=PointToScreen(e.Location);Location=new(point.X-dragOffset.X,point.Y-dragOffset.Y);}}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;Cursor=cursor.Pointer;}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
    protected override void Dispose(bool disposing){if(disposing){animation.Dispose();cursor.Dispose();Icon?.Dispose();}base.Dispose(disposing);}
}
