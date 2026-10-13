using System.Diagnostics;
using System.Drawing.Drawing2D;
using PetTracker.Core;

namespace PetTracker.App;

public sealed class StartupForm : Form
{
    private readonly StartupGate gate=new();
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly System.Windows.Forms.Timer timer=new(){Interval=8};
    private readonly CrystalCursor cursor=new(),hoverCursor=new(true);
    private readonly bool demo;
    private readonly string? folder;
    private readonly List<(Rectangle Bounds,string Code)> languageBoxes=[];
    private readonly Dictionary<string,float> languageGlow=[];
    private string hovered="",language="en";
    private OverlayController? controller;
    private TermsView? terms;
    private CheckBox? consent;
    private PremiumButton? accept,retry;
    private Label? message;
    private bool handedOff,closing,mayClose,resourcesDisposed;
    private double stepStart,lastPump;private double? previewTime;
    private string? failure;
    private bool demoCheck;
    private bool updateDone,updateFailed;
    private UpdateProgress updateProgress=new(UpdatePhase.Checking);
    private readonly CancellationTokenSource updateCancellation=new();
    private double introStart;
    private float downloadShown;
    private string? updateError;
    private bool dragging;
    private Point dragOffset;
    private void BeginDrag(Point screenPoint){dragOffset=new(screenPoint.X-Left,screenPoint.Y-Top);dragging=true;Capture=true;Cursor=Cursors.SizeAll;}
    private void MoveDrag(Point screenPoint){if(dragging)Location=new(screenPoint.X-dragOffset.X,screenPoint.Y-dragOffset.Y);}
    public StartupForm(bool demo=false,string? stateFolder=null,bool installedOnboarding=false)
    {
        this.demo=demo;folder=stateFolder;
        if(installedOnboarding)
        {
            var preferencesPath=Path.Combine(stateFolder??Path.Combine(InstallationProfile.DataFolder,"Live"),"overlay-preferences.json");
            var failed=false;
            language=OverlayPreferences.Load(preferencesPath,_=>failed=true).Language;
            if((failed||!File.Exists(preferencesPath))&&InstallationProfile.Load(InstallationProfile.ReceiptPath) is { } installation)language=installation.Language;
            gate.ConfigureInstallation(language);
        }
        updateDone=demo;
        AutoScaleMode=AutoScaleMode.Dpi;FormBorderStyle=FormBorderStyle.None;ClientSize=new(700,610);MinimumSize=ClientSize;MaximumSize=ClientSize;KeyPreview=true;
        FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;
        BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;Cursor=cursor.Pointer;Text="Aion 2 Pet Tracker";Icon=AppBrand.CreateIcon();TopMost=true;
        for(var i=0;i<UiText.Languages.Length;i++)languageBoxes.Add((new(50+(i%2)*305,165+(i/2)*67,290,53),UiText.Languages[i].Code));
        timer.Tick+=(_,_)=>TickFrame();timer.Start();
        Shown+=async(_,_)=>{if(!updateDone)await CheckUpdates();};
        FormClosing+=async(_,e)=>
        {
            if(mayClose||handedOff)return;e.Cancel=true;if(closing)return;closing=true;gate.Close();timer.Stop();
            updateCancellation.Cancel();
            try{if(controller is not null)await controller.DisposeAsync();}catch(Exception ex){controller?.Log("Kapanış hatası: "+ex.Message);}finally{mayClose=true;BeginInvoke(()=>Close());}
        };
    }
    private string S(string key)=>StartupText.Get(language,key);
    private async Task CheckUpdates()
    {
        try
        {
            var progress=new Progress<UpdateProgress>(value=>{if(!closing&&!IsDisposed){updateProgress=value;Invalidate();}});
            var restarting=await ApplicationUpdate.RunAsync(new GitHubUpdater(),progress,updateCancellation.Token);
            if(closing||IsDisposed)return;
            if(restarting){updateProgress=new(UpdatePhase.Applying,100);return;}
            updateProgress=new(UpdatePhase.NoUpdate,100);await Task.Delay(500,updateCancellation.Token);CompleteUpdateStep();
        }
        catch(OperationCanceledException) when(closing){}
        catch(Exception ex)
        {
            if(closing||IsDisposed)return;updateFailed=true;updateProgress=new(UpdatePhase.Failed);updateError=ex is NotSupportedException?S("updatePackageNeeded"):S("updateError");
            var retry=new PremiumButton{Location=new(362,515),Size=new(300,37),Text=S("retry"),TextLanguage=language};
            var proceed=new PremiumButton{Location=new(38,515),Size=new(300,37),Text=S("continueCurrent"),TextLanguage=language};
            retry.Click+=async(_,_)=>{foreach(var c in Controls.Cast<Control>().ToArray()){Controls.Remove(c);c.Dispose();}updateFailed=false;updateError=null;updateProgress=new(UpdatePhase.Checking);await CheckUpdates();};
            proceed.Click+=(_,_)=>CompleteUpdateStep();Controls.AddRange([retry,proceed]);Invalidate();
        }
    }
    private void CompleteUpdateStep()
    {
        if(closing||IsDisposed)return;foreach(var c in Controls.Cast<Control>().ToArray()){Controls.Remove(c);c.Dispose();}
        updateDone=true;introStart=clock.Elapsed.TotalSeconds;stepStart=introStart;Invalidate();
    }
    private void TickFrame()
    {
        var now=previewTime??clock.Elapsed.TotalSeconds;
        if(!updateDone){downloadShown+=(updateProgress.Percent-downloadShown)*.14f;Invalidate();return;}
        if(gate.Stage==StartupStage.Intro)
        {
            gate.FinishIntro(now-introStart);
            if(gate.Stage==StartupStage.Language)stepStart=now;
            else if(gate.Stage==StartupStage.Synchronizing)ShowSynchronization();
        }
        foreach(var (_,code) in languageBoxes)languageGlow[code]=languageGlow.GetValueOrDefault(code)+((hovered==code?1:0)-languageGlow.GetValueOrDefault(code))*.13f;
        if(gate.Stage==StartupStage.Synchronizing && now-lastPump>=.08)
        {
            lastPump=now;
            try
            {
                if(demo && now-stepStart>.9 && controller is not null)controller.LoadPreview();
                controller?.Pump();if(!demo && controller is not null && !controller.Running)failure=S("failure");
                if(controller is not null && gate.Observe(!controller.LiveMode||controller.Session.IdentityKnown,controller.Tracker.Quality,controller.Tracker.HasCurrent&&controller.Tracker.Current.Snapshot is not null))
                {OpenTracker();return;}
                if(message is not null)message.Text=failure is null?S("waiting")+"\n"+S(demo||controller?.Listening==true?"automatic":"connecting"):S("failure")+"\n"+failure;
            }
            catch(Exception ex){failure=ex.Message;controller?.Log("Açılış senkronizasyon hatası: "+ex.Message);}
        }
        Invalidate();
    }
    private void SelectLanguage(string code)
    {
        if(!UiText.IsSupported(code))return;gate.SelectLanguage(code);language=code;stepStart=clock.Elapsed.TotalSeconds;
        terms=new TermsView {Location=new(35,126),Size=new(630,340),BackColor=PremiumTheme.Panel,ForeColor=PremiumTheme.Ink,Font=new(UiText.FontFamily(language),10),Text=TermsContent.Get(language),RightToLeft=language=="ar"?RightToLeft.Yes:RightToLeft.No};
        consent=new CheckBox { Location=new(38,482),Size=new(620,40),Text=S("consent"),Font=new(UiText.FontFamily(language),10),BackColor=PremiumTheme.Background,ForeColor=PremiumTheme.Ink,Cursor=cursor.Pointer };
        accept=new PremiumButton {Location=new(362,537),Size=new(300,36),Text=S("accept"),TextLanguage=language,Enabled=false,Cursor=cursor.Pointer};
        var close=new PremiumButton {Location=new(38,537),Size=new(300,36),Text=UiText.Get(language,"close"),TextLanguage=language,Cursor=cursor.Pointer};
        consent.CheckedChanged+=(_,_)=>{accept.Enabled=consent.Checked;accept.ForeColor=consent.Checked?PremiumTheme.Gold:PremiumTheme.Muted;accept.Invalidate();};
        close.Click+=(_,_)=>Close();accept.Click+=(_,_)=>{if(!gate.Accept(consent.Checked))return;ShowSynchronization();};
        Controls.AddRange([terms,consent,accept,close]);accept.ForeColor=PremiumTheme.Muted;
    }
    private void ShowSynchronization(bool startCapture=true)
    {
        foreach(var control in Controls.Cast<Control>().ToArray()){Controls.Remove(control);control.Dispose();}
        stepStart=clock.Elapsed.TotalSeconds;
        message=new Label{Location=new(45,352),Size=new(610,100),Font=new(UiText.FontFamily(language),11),TextAlign=ContentAlignment.MiddleCenter,ForeColor=PremiumTheme.Muted,Text=S("waiting")+"\n"+S("automatic")};
        message.Cursor=Cursors.SizeAll;message.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left)BeginDrag(message.PointToScreen(e.Location));};
        retry=new PremiumButton{Location=new(362,527),Size=new(300,37),Text=S("retry"),TextLanguage=language,Cursor=cursor.Pointer};
        var close=new PremiumButton{Location=new(38,527),Size=new(300,37),Text=UiText.Get(language,"close"),TextLanguage=language,Cursor=cursor.Pointer};
        close.Click+=(_,_)=>Close();retry.Click+=async(_,_)=>{retry.Enabled=false;try{if(controller is not null)await controller.Stop();if(closing||IsDisposed)return;failure=null;if(controller is null)InitializeController();else controller.Start();}catch(Exception ex){failure=ex.Message;}finally{if(!retry.IsDisposed)retry.Enabled=true;}};
        Controls.AddRange([message,retry,close]);
        if(!startCapture){retry.Enabled=false;return;}
        try
        {
            InitializeController();
        }
        catch(Exception ex){failure=ex.Message;controller?.Log("Okuma başlatılamadı: "+ex.Message);}
    }
    private void InitializeController()
    {
        controller=new(!demo,folder);var path=Path.Combine(controller.Folder,"overlay-preferences.json");var prefs=OverlayPreferences.Load(path,controller.Log);prefs.Language=language;prefs.Save(path);if(!demo)controller.Start();
    }
    private void OpenTracker()
    {
        var overlay=new OverlayForm(liveMode:!demo,stateFolder:folder,sharedController:controller,selectedLanguage:language);
        // Hiding a modal form ends ShowDialog. Keep the message loop alive until
        // the tracker actually closes, so hide/show hotkeys preserve capture.
        overlay.FormClosed+=(_,_)=>Close();
        timer.Stop();handedOff=true;Hide();
        if(demoCheck)
        {
            overlay.Shown+=(_,_)=>{File.WriteAllText(Path.Combine(folder!,"flow-result.txt"),"PASS: consent -> synthetic fresh snapshot -> same controller handed to overlay; no capture.");overlay.BeginInvoke(()=>overlay.Close());};
        }
        overlay.Show();
    }
    public void BeginDemoCheck()
    {
        if(!demo)throw new InvalidOperationException("Offline test only");demoCheck=true;
        gate.FinishIntro(4);SelectLanguage("en");consent!.Checked=true;if(!gate.Accept(consent.Checked))throw new InvalidOperationException("Consent failed");ShowSynchronization();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);var g=e.Graphics;g.ScaleTransform(DeviceDpi/96f,DeviceDpi/96f);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        var now=previewTime??clock.Elapsed.TotalSeconds;
        using var background=new LinearGradientBrush(new Rectangle(0,0,700,610),Color.FromArgb(24,21,34),Color.FromArgb(9,15,24),70);g.FillRectangle(background,0,0,700,610);
        // Deterministic floating motes and orbit highlights, no external assets or hooks.
        for(var i=0;i<35;i++)
        {
            var x=(i*127.4+Math.Sin(now*.32+i)*17)%690;var y=(i*83.7-now*(5+i%4)+6100)%610;
            var a=(int)(12+24*(.5+.5*Math.Sin(now*1.2+i)));using var mote=new SolidBrush(Color.FromArgb(a,i%5==0?PremiumTheme.Pink:PremiumTheme.Gold));g.FillEllipse(mote,(float)x,(float)y,i%3+1,i%3+1);
        }
        using var border=new Pen(Color.FromArgb(90,PremiumTheme.Gold));g.DrawRectangle(border,1,1,698,608);
        if(!updateDone)
        {
            var key=updateProgress.Phase switch{UpdatePhase.Downloading=>"downloading",UpdatePhase.Applying=>"applyingUpdate",UpdatePhase.NoUpdate=>"upToDate",UpdatePhase.Failed=>"updateError",_=>"checkingUpdates"};
            PremiumTheme.Text(g,"Aion 2 Pet Tracker",new(25,130,650,55),26,PremiumTheme.Gold,"en",FontStyle.Bold,StringAlignment.Center,heading:true);
            PremiumTheme.Text(g,S(key),new(30,222,640,70),21,PremiumTheme.Ink,language,FontStyle.Bold,StringAlignment.Center);
            var bar=new RectangleF(110,330,480,9);using var path=PremiumTheme.Round(bar,4);using var track=new SolidBrush(Color.FromArgb(37,39,48));g.FillPath(track,path);
            var state=g.Save();g.SetClip(path);
            if(updateProgress.Phase==UpdatePhase.Checking)
            {var x=bar.X-130+(float)((now*.38)%1)*(bar.Width+130);using var beam=new LinearGradientBrush(new RectangleF(x,bar.Y,130,bar.Height),Color.Transparent,PremiumTheme.Gold,0f);g.FillRectangle(beam,x,bar.Y,130,bar.Height);}
            else if(!updateFailed)
            {using var fill=new SolidBrush(PremiumTheme.Gold);g.FillRectangle(fill,bar.X,bar.Y,bar.Width*(updateProgress.Phase==UpdatePhase.Downloading?downloadShown/100f:1),bar.Height);}
            g.Restore(state);using var glow=new Pen(Color.FromArgb((int)(25+18*(.5+.5*Math.Sin(now*2))),PremiumTheme.Gold),4);g.DrawPath(glow,path);
            var detail=updateFailed?updateError:updateProgress.Phase==UpdatePhase.Downloading?$"{updateProgress.Percent}% · {updateProgress.Version}":"GitHub · keytftw/aion2pettracker";
            PremiumTheme.Text(g,detail??"",new(35,360,630,85),11,PremiumTheme.Muted,language,align:StringAlignment.Center);
            DrawStartupCredit(g);
            PremiumTheme.Text(g,"×",new(656,12,28,28),18,PremiumTheme.Muted,"en",align:StringAlignment.Center);return;
        }
        var intro=gate.Stage==StartupStage.Intro;var reveal=(float)Math.Clamp((now-introStart)/1.1,0,1);
        var cy=intro?205:48;var radius=intro?46f:19f;
        for(var ring=0;ring<4;ring++)
        {using var pen=new Pen(Color.FromArgb((int)((35-ring*7)*reveal),PremiumTheme.Gold),2+ring*3);g.DrawEllipse(pen,350-radius-ring*3,cy-radius-ring*3,2*(radius+ring*3),2*(radius+ring*3));}
        using(var pen=new Pen(Color.FromArgb((int)(190*reveal),PremiumTheme.Gold),1.4f))
        {g.DrawPolygon(pen,new PointF[]{new(350,cy-radius),new(350+radius,cy),new(350,cy+radius),new(350-radius,cy)});g.DrawLines(pen,new PointF[]{new(350-radius*.4f,cy+radius*.25f),new(350,cy-radius*.45f),new(350+radius*.4f,cy+radius*.25f)});g.DrawArc(pen,350-radius-12,cy-radius-12,2*radius+24,2*radius+24,(float)(now*48),70);}
        if(intro)
        {
            var y=285+(1-reveal)*22;for(var glow=3;glow>0;glow--)PremiumTheme.Text(g,"Aion 2 Pet Tracker",new(20-glow,y-glow,660+glow*2,65+glow*2),28,Color.FromArgb((int)(10*reveal),PremiumTheme.Gold),"en",FontStyle.Bold,StringAlignment.Center,heading:true);
            PremiumTheme.Text(g,"Aion 2 Pet Tracker",new(20,y,660,65),28,Color.FromArgb((int)(255*reveal),PremiumTheme.Gold),"en",FontStyle.Bold,StringAlignment.Center,heading:true);
            PremiumTheme.Text(g,"Designed & Developed by Keyt",new(20,359,660,38),11,Color.FromArgb((int)(210*reveal),PremiumTheme.Ink),"en",align:StringAlignment.Center);
            PremiumTheme.Text(g,"Support on Patreon",new(20,399,660,24),9,Color.FromArgb((int)(160*reveal),PremiumTheme.Gold),"en",align:StringAlignment.Center);
            using var track=new SolidBrush(Color.FromArgb(35,PremiumTheme.Gold));g.FillRectangle(track,190,438,320,2);using var progress=new SolidBrush(PremiumTheme.Gold);g.FillRectangle(progress,190,438,320*(float)Math.Min((now-introStart)/3.5,1),2);
        }
        else if(gate.Stage==StartupStage.Language)
        {
            PremiumTheme.Text(g,"Select Language",new(25,85,650,40),22,PremiumTheme.Gold,"en",FontStyle.Bold,StringAlignment.Center,heading:true);
            foreach(var (box,code) in languageBoxes)
            {var h=languageGlow.GetValueOrDefault(code);var entry=(float)Math.Clamp((now-stepStart-languageBoxes.FindIndex(b=>b.Code==code)*.025)/.35,0,1);entry=1-MathF.Pow(1-entry,3);var r=new RectangleF(box.X,box.Y-h*2+(1-entry)*17,box.Width,box.Height);using var shape=PremiumTheme.Round(r,9);using var fill=new SolidBrush(Color.FromArgb((int)(24+h*45),PremiumTheme.Gold));g.FillPath(fill,shape);using var edge=new Pen(Color.FromArgb((int)(65+h*150),PremiumTheme.Gold),1+h);g.DrawPath(edge,shape);PremiumTheme.Text(g,UiText.Languages.First(l=>l.Code==code).Name,r,13,Color.FromArgb(237,(int)(225+h*9),202),code,FontStyle.Bold,StringAlignment.Center);}
        }
        else if(gate.Stage==StartupStage.Terms)
            PremiumTheme.Text(g,S("terms"),new(35,79,630,36),20,PremiumTheme.Gold,language,FontStyle.Bold,StringAlignment.Center);
        else if(gate.Stage==StartupStage.Synchronizing)
        {
            PremiumTheme.Text(g,demo?S("demoRelog"):controller?.Listening==true||previewTime.HasValue?S("relog"):S("connecting"),new(35,150,630,140),23,PremiumTheme.Gold,language,FontStyle.Bold,StringAlignment.Center);
            if(!demo)PremiumTheme.Text(g,S("relogWhy"),new(55,455,590,55),10,PremiumTheme.Muted,language,align:StringAlignment.Center);
            using var pen=new Pen(Color.FromArgb(150,PremiumTheme.Gold),2.5f);g.DrawArc(pen,337,305,26,26,(float)(now*120),240);
        }
        if(!intro)DrawStartupCredit(g);
        if(gate.Stage is StartupStage.Intro or StartupStage.Language)PremiumTheme.Text(g,"×",new(656,12,28,28),18,PremiumTheme.Muted,"en",align:StringAlignment.Center);
    }
    private static void DrawStartupCredit(Graphics g)
    {
        PremiumTheme.Text(g,"Designed & Developed by Keyt",new(20,577,660,12),7,PremiumTheme.Muted,"en",align:StringAlignment.Center);
        PremiumTheme.Text(g,"Support on Patreon",new(20,593,660,12),7,Color.FromArgb(175,PremiumTheme.Gold),"en",align:StringAlignment.Center);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);if(dragging){MoveDrag(PointToScreen(e.Location));return;}var point=new Point((int)(e.X*96f/DeviceDpi),(int)(e.Y*96f/DeviceDpi));hovered=gate.Stage==StartupStage.Language?languageBoxes.FirstOrDefault(b=>b.Bounds.Contains(point)).Code??"":"";Cursor=hovered.Length>0?hoverCursor.Pointer:point.X>650&&point.Y<45?cursor.Pointer:Cursors.SizeAll;
    }
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hovered="";}
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;var point=new Point((int)(e.X*96f/DeviceDpi),(int)(e.Y*96f/DeviceDpi));
        if((gate.Stage is StartupStage.Intro or StartupStage.Language)&&point.X>650 && point.Y<45){Close();return;}
        if(gate.Stage==StartupStage.Language && languageBoxes.FirstOrDefault(b=>b.Bounds.Contains(point)).Code is { } code){SelectLanguage(code);return;}
        BeginDrag(PointToScreen(e.Location));
    }
    public void VerifyDragging()
    {
        PremiumDialogMotion.ShowForPreview(this);
        void Check(){var before=Location;OnMouseDown(new(MouseButtons.Left,1,10,80,0));OnMouseMove(new(MouseButtons.Left,0,60,110,0));OnMouseUp(new(MouseButtons.Left,1,60,110,0));if(Location!=new Point(before.X+50,before.Y+30))throw new InvalidOperationException("Startup drag failed: "+gate.Stage);}
        Check();gate.FinishIntro(4);Check();SelectLanguage("en");Check();gate.Accept(true);ShowSynchronization(startCapture:false);Check();
    }
    public void VerifyConsentControls(string code)
    {
        gate.FinishIntro(4);SelectLanguage(code);if(accept!.Enabled)throw new InvalidOperationException("Unchecked acceptance enabled");consent!.Checked=true;if(!accept.Enabled)throw new InvalidOperationException("Checked acceptance disabled");consent.Checked=false;if(accept.Enabled)throw new InvalidOperationException("Unchecked acceptance enabled");
    }
    public void ExportStep(string path,StartupStage stage,string code="en")
    {
        updateDone=true;
        previewTime=stage==StartupStage.Intro?2.4:4;
        if(stage!=StartupStage.Intro){gate.FinishIntro(4);if(stage is StartupStage.Terms or StartupStage.Synchronizing)SelectLanguage(code);}
        if(stage==StartupStage.Synchronizing){gate.Accept(true);ShowSynchronization(startCapture:false);}
        PremiumDialogMotion.ShowForPreview(this);using var bitmap=new Bitmap(Width,Height);DrawToBitmap(bitmap,ClientRectangle);bitmap.Save(path);Hide();timer.Stop();
    }
    protected override void Dispose(bool disposing)
    {
        // Close disposes the form; Program's using scope may dispose it again.
        if(disposing&&!resourcesDisposed)
        {
            resourcesDisposed=true;closing=true;
            updateCancellation.Cancel();updateCancellation.Dispose();timer.Dispose();cursor.Dispose();hoverCursor.Dispose();Icon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
