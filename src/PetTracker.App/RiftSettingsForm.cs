namespace PetTracker.App;

public sealed class RiftSettingsForm : Form
{
    private readonly System.Windows.Forms.Timer timer=new(){Interval=1000};
    public RiftSettingsForm(OverlayPreferences prefs,Func<RiftRegion?> region,Action save,Action<bool,int> preview)
    {
        var lang=prefs.Language;AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new(96,96);ClientSize=new(420,365);
        FormBorderStyle=FormBorderStyle.None;BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;TopMost=true;ShowInTaskbar=false;StartPosition=FormStartPosition.CenterParent;
        Font=new(UiText.FontFamily(lang),10);Text="Spacetime Rift";
        var title=new PremiumLabel{Text=Text,TextLanguage=lang,Font=new(UiText.FontFamily(lang),16),ForeColor=PremiumTheme.Gold,Location=new(18,12),Size=new(350,34)};
        var close=new PremiumButton{Text="×",Location=new(378,14),Size=new(25,25)};close.Click+=(_,_)=>Close();
        var enabled=new CheckBox{Text=RiftText.Get(lang,"alerts"),Checked=prefs.RiftAlertsEnabled,Location=new(20,62),Size=new(380,28)};
        var lead=new Label{Text=RiftText.Get(lang,"lead"),Location=new(20,105),Size=new(280,28)};
        var minutes=new NumericUpDown{Minimum=1,Maximum=120,Value=prefs.RiftLeadMinutes,Location=new(314,102),Size=new(82,29),BackColor=PremiumTheme.Panel,ForeColor=PremiumTheme.Gold};
        var sound=new CheckBox{Text=RiftText.Get(lang,"sound"),Checked=prefs.RiftSoundEnabled,Location=new(20,146),Size=new(380,28)};
        var next=new PremiumLabel{TextLanguage=lang,Location=new(20,190),Size=new(380,60),ForeColor=PremiumTheme.Gold};
        var note=new PremiumLabel{Text=RiftText.Get(lang,"reference"),TextLanguage=lang,Location=new(20,254),Size=new(380,40),ForeColor=PremiumTheme.Muted,Font=new(UiText.FontFamily(lang),8)};
        var test=new PremiumButton{Text=RiftText.Get(lang,"test"),TextLanguage=lang,Location=new(20,307),Size=new(180,35)};
        var testOpen=new PremiumButton{Text=RiftText.Get(lang,"testOpen"),TextLanguage=lang,Location=new(216,307),Size=new(180,35)};
        enabled.CheckedChanged+=(_,_)=>{prefs.RiftAlertsEnabled=enabled.Checked;save();};
        sound.CheckedChanged+=(_,_)=>{prefs.RiftSoundEnabled=sound.Checked;save();};
        minutes.ValueChanged+=(_,_)=>{prefs.RiftLeadMinutes=(int)minutes.Value;save();};
        test.Click+=(_,_)=>preview(false,prefs.RiftLeadMinutes);testOpen.Click+=(_,_)=>preview(true,0);
        void UpdateTime(){var r=region();var now=DateTimeOffset.UtcNow;var opening=r is null?(DateTimeOffset?)null:RiftClock.Next(now,r);next.Text=opening is null?RiftText.Get(lang,"waiting"):r!.Name+" · "+opening.Value.ToLocalTime().ToString("HH:mm")+" · "+(opening.Value-now).ToString(@"hh\:mm\:ss");}
        timer.Tick+=(_,_)=>UpdateTime();UpdateTime();timer.Start();
        Controls.AddRange([title,close,enabled,lead,minutes,sound,next,note,test,testOpen]);PremiumDialogMotion.Attach(this);
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var pen=new Pen(PremiumTheme.Gold);e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);}
    protected override bool ProcessCmdKey(ref Message msg,Keys key){if(key==Keys.Escape){Close();return true;}return base.ProcessCmdKey(ref msg,key);}
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
