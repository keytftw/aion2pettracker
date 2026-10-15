namespace PetTracker.App;

public sealed class RiftSettingsForm : Form
{
    private Point dragOffset;
    private bool dragging;
    private void BeginDrag(Point point){dragOffset=new(point.X-Left,point.Y-Top);dragging=true;Capture=true;}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left)BeginDrag(PointToScreen(e.Location));}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){var p=PointToScreen(e.Location);Location=new(p.X-dragOffset.X,p.Y-dragOffset.Y);}}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
    private readonly System.Windows.Forms.Timer timer=new(){Interval=1000};
    public RiftSettingsForm(OverlayPreferences prefs,Func<RiftRegion?> region,Action save,Action<bool,int> preview)
    {
        var lang=prefs.Language;AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new(96,96);ClientSize=new(420,441);
        FormBorderStyle=FormBorderStyle.None;BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;TopMost=true;ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;
        Font=new(UiText.FontFamily(lang),10);Text="Spacetime Rift";
        var title=new PremiumLabel{Text=Text,TextLanguage=lang,Font=new(UiText.FontFamily(lang),16),ForeColor=RiftVisual.Violet,Location=new(67,15),Size=new(300,34)};
        var close=new PremiumButton{Text="×",Location=new(378,14),Size=new(25,25)};close.Click+=(_,_)=>Close();
        var enabled=new PremiumToggle{Text=RiftText.Get(lang,"alerts"),Checked=prefs.RiftAlertsEnabled,TextLanguage=lang,Location=new(20,62),Size=new(380,28)};
        var lead=new PremiumLabel{Text=RiftText.Get(lang,"lead"),TextLanguage=lang,Location=new(20,105),Size=new(270,28)};
        prefs.RiftLeadMinutes=Math.Clamp(prefs.RiftLeadMinutes,1,10);
        var minutes=new PremiumLabel{Text=prefs.RiftLeadMinutes.ToString(),TextLanguage=lang,Location=new(300,101),Size=new(57,38),TextAlign=ContentAlignment.MiddleCenter,ForeColor=RiftVisual.Violet,BackColor=PremiumTheme.Panel,Font=new(UiText.FontFamily(lang),17,FontStyle.Bold)};
        var increase=new PremiumButton{Text="▴",AccessibleName="Increase minutes",Location=new(362,100),Size=new(34,20),AccentColor=RiftVisual.Violet,ForeColor=RiftVisual.Violet};
        var decrease=new PremiumButton{Text="▾",AccessibleName="Decrease minutes",Location=new(362,123),Size=new(34,20),AccentColor=RiftVisual.Violet,ForeColor=RiftVisual.Violet};
        void SetMinutes(int value){prefs.RiftLeadMinutes=Math.Clamp(value,1,10);minutes.Text=prefs.RiftLeadMinutes.ToString();increase.Enabled=prefs.RiftLeadMinutes<10;decrease.Enabled=prefs.RiftLeadMinutes>1;save();}
        increase.Click+=(_,_)=>SetMinutes(prefs.RiftLeadMinutes+1);decrease.Click+=(_,_)=>SetMinutes(prefs.RiftLeadMinutes-1);
        increase.Enabled=prefs.RiftLeadMinutes<10;decrease.Enabled=prefs.RiftLeadMinutes>1;
        var sound=new PremiumToggle{Text=RiftText.Get(lang,"sound"),Checked=prefs.RiftSoundEnabled,TextLanguage=lang,Location=new(20,146),Size=new(380,28)};
        var next=new PremiumLabel{TextLanguage=lang,Location=new(20,190),Size=new(380,60),ForeColor=RiftVisual.Violet};
        var scheduleTitle=new PremiumLabel{TextLanguage=lang,Location=new(20,254),Size=new(380,24),ForeColor=RiftVisual.Violet,Font=new(UiText.FontFamily(lang),9,FontStyle.Bold)};
        var scheduleTimes=new PremiumLabel{TextLanguage=lang,Location=new(20,283),Size=new(380,27),ForeColor=RiftVisual.Cyan,Font=new(UiText.FontFamily(lang),8.4f)};
        var note=new PremiumLabel{Text=RiftText.Get(lang,"reference"),TextLanguage=lang,Location=new(20,330),Size=new(380,40),ForeColor=PremiumTheme.Muted,Font=new(UiText.FontFamily(lang),8)};
        var test=new PremiumButton{AccentColor=RiftVisual.Violet,ForeColor=RiftVisual.Violet,Text=RiftText.Get(lang,"test"),TextLanguage=lang,Location=new(20,307),Size=new(180,35)};
        var testOpen=new PremiumButton{AccentColor=RiftVisual.Violet,ForeColor=RiftVisual.Violet,Text=RiftText.Get(lang,"testOpen"),TextLanguage=lang,Location=new(216,307),Size=new(180,35)};
        enabled.CheckedChanged+=(_,_)=>{prefs.RiftAlertsEnabled=enabled.Checked;save();};
        sound.CheckedChanged+=(_,_)=>{prefs.RiftSoundEnabled=sound.Checked;save();};
        test.Click+=(_,_)=>preview(false,prefs.RiftLeadMinutes);testOpen.Click+=(_,_)=>preview(true,0);
        test.Top+=76;testOpen.Top+=76;
        void UpdateTime()
        {
            var r=region();var now=DateTimeOffset.UtcNow;var opening=r is null?(DateTimeOffset?)null:RiftClock.Next(now,r);
            next.Text=opening is null?RiftText.Get(lang,"waiting"):r!.Name+" · "+TimeZoneInfo.ConvertTime(opening.Value,r.Zone).ToString("HH:mm")+" · "+(opening.Value-now).ToString(@"hh\:mm\:ss");
            scheduleTitle.Text=RiftText.Get(lang,"times")+(r is null?"":" · "+r.Name+" (UTC"+TimeZoneInfo.ConvertTime(now,r.Zone).ToString("zzz")+")");
            scheduleTimes.Text=r is null?"—":RiftClock.DailyTimes(now,r);
        }
        timer.Tick+=(_,_)=>UpdateTime();UpdateTime();timer.Start();
        var emblem=new PictureBox{Image=RiftVisual.Icon(48),SizeMode=PictureBoxSizeMode.Zoom,Location=new(17,10),Size=new(42,42)};
        foreach(Control control in new Control[]{title,emblem}){control.Cursor=Cursors.SizeAll;control.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left)BeginDrag(control.PointToScreen(e.Location));};}
        Controls.AddRange([emblem,title,close,enabled,lead,minutes,increase,decrease,sound,next,scheduleTitle,scheduleTimes,note,test,testOpen]);PremiumDialogMotion.Attach(this);
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var pen=new Pen(RiftVisual.Violet);e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);}
    protected override bool ProcessCmdKey(ref Message msg,Keys key){if(key==Keys.Escape){Close();return true;}return base.ProcessCmdKey(ref msg,key);}
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
