namespace PetTracker.App;

public sealed class OverlaySettingsForm : Form
{
    private readonly System.ComponentModel.IContainer components=new System.ComponentModel.Container();
    private readonly ContextMenuStrip languageMenu;
    private readonly OverlayPreferences preferences;
    private readonly PremiumButton languageButton;
    private readonly PremiumButton hideHotkeyButton,compactHotkeyButton;
    private readonly Action changed;
    private readonly Func<bool> hotkeysChanged;
    private bool capturingHide;
    private bool dragging;
    private Point dragOffset;
    private void BeginDrag(Point screenPoint){dragOffset=new(screenPoint.X-Left,screenPoint.Y-Top);dragging=true;Capture=true;Cursor=Cursors.SizeAll;}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left)BeginDrag(PointToScreen(e.Location));}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){var point=PointToScreen(e.Location);Location=new(point.X-dragOffset.X,point.Y-dragOffset.Y);}else Cursor=Cursors.SizeAll;}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
    public OverlaySettingsForm(OverlayPreferences preferences,Cursor cursor,int maximumHeight,Action changed,Action logs,Func<bool>? hotkeysChanged=null,Action? riftSettings=null)
    {
        AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new(96,96);ClientSize=new(340,540);FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.CenterParent;TopMost=true;ShowInTaskbar=false;
        BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;Cursor=cursor;KeyPreview=true;this.preferences=preferences;this.changed=changed;this.hotkeysChanged=hotkeysChanged??(()=>true);
        var title=new PremiumLabel { Location=new(18,9),Size=new(268,31),AutoSize=false,ForeColor=PremiumTheme.Gold,Font=new(UiText.FontFamily(preferences.Language),13,FontStyle.Bold) };
        var opacityLabel=new PremiumLabel { Location=new(19,55),Size=new(226,24),AutoSize=false };
        var opacityValue=new PremiumLabel { Location=new(252,55),Size=new(70,24),AutoSize=false,TextAlign=ContentAlignment.MiddleRight,ForeColor=PremiumTheme.Gold,Text=preferences.OpacityPercent+"%" };
        var opacity=new PremiumSlider { Minimum=PetTracker.Core.OverlayPreferences.MinimumOpacityPercent,Maximum=PetTracker.Core.OverlayPreferences.MaximumOpacityPercent,Location=new(14,79),Size=new(308,34),Value=preferences.OpacityPercent,Cursor=cursor };
        var heightLabel=new PremiumLabel { Location=new(19,122),Size=new(226,24),AutoSize=false };
        var heightValue=new PremiumLabel { Location=new(252,122),Size=new(70,24),AutoSize=false,TextAlign=ContentAlignment.MiddleRight,ForeColor=PremiumTheme.Gold,Text="1–7" };
        var height=new PremiumLabel { Location=new(19,150),Size=new(303,34),AutoSize=false,ForeColor=PremiumTheme.Muted };
        var top=new PremiumToggle { Location=new(18,190),Size=new(305,29),Checked=preferences.AlwaysOnTop,Cursor=cursor };
        var locked=new PremiumToggle { Location=new(18,225),Size=new(305,29),Checked=preferences.Locked,Cursor=cursor };
        var languageLabel=new PremiumLabel { Location=new(19,269),Size=new(303,24),AutoSize=false };
        var languages=new PremiumButton { Location=new(18,299),Size=new(305,29),Cursor=cursor };
        languageButton=languages;
        var support=new PremiumButton { Location=new(18,339),Size=new(305,31),Cursor=cursor };
        support.Click+=(_,_)=>
        {
            try{using var browser=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.patreon.com/cw/keytftw"){UseShellExecute=true});}
            catch(System.ComponentModel.Win32Exception){MessageBox.Show(this,UiText.Get(preferences.Language,"browserFailed"),"Aion 2 Pet Tracker",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        };
        hideHotkeyButton=new PremiumButton{Location=new(18,382),Size=new(305,34),Cursor=cursor};
        compactHotkeyButton=new PremiumButton{Location=new(18,423),Size=new(305,34),Cursor=cursor};
        var diagnostic=new PremiumButton { Location=new(18,470),Size=new(145,29),Cursor=cursor };
        var done=new PremiumButton { Location=new(177,470),Size=new(146,29),Cursor=cursor };
        hideHotkeyButton.Click+=(_,_)=>StartHotkeyCapture(true);
        compactHotkeyButton.Click+=(_,_)=>StartHotkeyCapture(false);
        // The settings form owns this reusable menu. Closed fires inside WinForms' click
        // handling; disposing there invalidates the menu before that handling finishes.
        languageMenu=new ContextMenuStrip(components) { ShowImageMargin=false,BackColor=PremiumTheme.Panel,ForeColor=PremiumTheme.Ink,Cursor=cursor,Renderer=new DarkMenuRenderer() };
            foreach(var (code,name) in UiText.Languages)
            {
                var item=new ToolStripMenuItem(name) { Font=new(UiText.FontFamily(code),10.5f) };
                item.Click+=(_,_)=>{preferences.Language=code;RefreshStrings();changed();};languageMenu.Items.Add(item);
            }
        languages.Click+=(_,_)=>languageMenu.Show(languages,new Point(0,languages.Height));
        diagnostic.Click+=(_,_)=>logs();
        done.Click+=(_,_)=>Close();
        var close=new PremiumButton { Text="×",Location=new(302,12),Size=new(22,23),Cursor=cursor };close.Click+=(_,_)=>Close();
        var credit=new PremiumLabel { Text="Designed & Developed by Keyt",ForeColor=PremiumTheme.Muted,Font=new(UiText.FontFamily("en"),7),Location=new(19,512),Size=new(305,15),TextAlign=ContentAlignment.MiddleCenter };
        foreach(var label in new[]{title,opacityLabel,opacityValue,heightLabel,heightValue,languageLabel,credit})
        {label.Cursor=Cursors.SizeAll;label.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left)BeginDrag(label.PointToScreen(e.Location));};}
        foreach(var button in new[]{languages,support,diagnostic,done,hideHotkeyButton,compactHotkeyButton}){button.TextSize=10;button.DpiAwareText=true;}
        void RefreshStrings()
        {
            var lang=preferences.Language;Font=new(UiText.FontFamily(lang),10.5f);
            foreach(var label in new[]{title,opacityLabel,opacityValue,heightLabel,heightValue,languageLabel})label.TextLanguage=lang;
            title.Font=new(UiText.FontFamily(lang),13,FontStyle.Bold);
            title.Text=UiText.Get(lang,"settings");opacityLabel.Text=UiText.Get(lang,"opacity");heightLabel.Text=UiText.Get(lang,"visibleCards");
            height.TextLanguage=lang;height.Text=HelpText.Get(lang,"autoHeight");
            top.Text=UiText.Get(lang,"top");locked.Text=UiText.Get(lang,"lock");languageLabel.Text=UiText.Get(lang,"language");
            diagnostic.Text=UiText.Get(lang,"logs");done.Text=UiText.Get(lang,"done");RefreshHotkeyLabels();diagnostic.TextLanguage=lang;done.TextLanguage=lang;top.TextLanguage=lang;locked.TextLanguage=lang;Invalidate();
            languages.Text=UiText.Languages.First(l=>l.Code==lang).Name+"  ▾";languages.TextLanguage=lang;
            support.Text=UiText.Get(lang,"support");support.TextLanguage=lang;
        }
        opacity.ValueChanged+=(_,_)=>{preferences.OpacityPercent=opacity.Value;opacityValue.Text=opacity.Value+"%";changed();};
        top.CheckedChanged+=(_,_)=>{preferences.AlwaysOnTop=top.Checked;changed();};locked.CheckedChanged+=(_,_)=>{preferences.Locked=locked.Checked;changed();};
        if(riftSettings is not null)
        {
            ClientSize=new(340,585);diagnostic.Top+=42;done.Top+=42;credit.Top+=42;
            var rift=new PremiumButton{Text="Spacetime Rift",Location=new(18,470),Size=new(305,31),Cursor=cursor,TextSize=10,DpiAwareText=true};
            rift.Click+=(_,_)=>riftSettings();Controls.Add(rift);
        }
        Controls.AddRange([title,opacityLabel,opacityValue,opacity,heightLabel,heightValue,height,top,locked,languageLabel,languages,support,hideHotkeyButton,compactHotkeyButton,diagnostic,done,close,credit]);RefreshStrings();PremiumDialogMotion.Attach(this);
        var linkTop=credit.Bottom+10;
        var guideLink=new PremiumButton{Text="keytdev.com/pettracker",AccessibleName=HelpText.Get(preferences.Language,"guide"),Location=new(18,linkTop),Size=new(305,29),DpiAwareText=true,TextSize=10};
        var contactLink=new PremiumButton{Text=AppLinks.Email,AccessibleName=HelpText.Get(preferences.Language,"contact"),Location=new(18,linkTop+36),Size=new(305,29),DpiAwareText=true,TextSize=10};
        guideLink.Click+=(_,_)=>AppLinks.Open(this,AppLinks.Guide,preferences.Language);
        contactLink.Click+=(_,_)=>AppLinks.Open(this,"mailto:"+AppLinks.Email,preferences.Language);
        Controls.AddRange([guideLink,contactLink]);ClientSize=new(340,linkTop+78);
    }
    private static int Modifiers(Keys keys)=>((keys&Keys.Control)!=0?2:0)|((keys&Keys.Alt)!=0?1:0)|((keys&Keys.Shift)!=0?4:0);
    private static string Chord(int modifiers,int key)
    {
        var parts=new List<string>();if((modifiers&2)!=0)parts.Add("Ctrl");if((modifiers&1)!=0)parts.Add("Alt");if((modifiers&4)!=0)parts.Add("Shift");
        parts.Add(Enum.IsDefined(typeof(Keys),key)?new KeysConverter().ConvertToString((Keys)key)??key.ToString():key.ToString());return string.Join("+",parts);
    }
    private void RefreshHotkeyLabels()
    {
        var lang=preferences.Language;
        hideHotkeyButton.Text=(capturingHide?UiText.Get(lang,"pressKey"):UiText.Get(lang,"hideHotkey")+" · "+Chord(preferences.HideHotkeyModifiers,preferences.HideHotkeyKey));
        compactHotkeyButton.Text=(!capturingHide&&captureCompact?UiText.Get(lang,"pressKey"):UiText.Get(lang,"compactHotkey")+" · "+Chord(preferences.CompactHotkeyModifiers,preferences.CompactHotkeyKey));
        hideHotkeyButton.TextLanguage=compactHotkeyButton.TextLanguage=lang;
        hideHotkeyButton.Invalidate();compactHotkeyButton.Invalidate();
    }
    private bool captureCompact;
    private void StartHotkeyCapture(bool hide)
    {
        capturingHide=hide;captureCompact=!hide;RefreshHotkeyLabels();(hide?hideHotkeyButton:compactHotkeyButton).Focus();
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);if(!capturingHide&&!captureCompact)return;
        if(e.KeyCode==Keys.Escape){capturingHide=captureCompact=false;RefreshHotkeyLabels();e.Handled=e.SuppressKeyPress=true;return;}
        var key=(int)e.KeyCode;if(!ShortcutKey.IsValid(key,Modifiers(e.Modifiers)))return;
        var oldHide=(preferences.HideHotkeyKey,preferences.HideHotkeyModifiers);var oldCompact=(preferences.CompactHotkeyKey,preferences.CompactHotkeyModifiers);
        if(capturingHide){preferences.HideHotkeyKey=key;preferences.HideHotkeyModifiers=Modifiers(e.Modifiers);}
        else{preferences.CompactHotkeyKey=key;preferences.CompactHotkeyModifiers=Modifiers(e.Modifiers);}
        if(preferences.HideHotkeyKey==preferences.CompactHotkeyKey&&preferences.HideHotkeyModifiers==preferences.CompactHotkeyModifiers)
        {
            preferences.HideHotkeyKey=oldHide.Item1;preferences.HideHotkeyModifiers=oldHide.Item2;preferences.CompactHotkeyKey=oldCompact.Item1;preferences.CompactHotkeyModifiers=oldCompact.Item2;
            MessageBox.Show(this,UiText.Get(preferences.Language,"hotkeysDifferent"),Text,MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        else if(!hotkeysChanged())
        {
            preferences.HideHotkeyKey=oldHide.Item1;preferences.HideHotkeyModifiers=oldHide.Item2;preferences.CompactHotkeyKey=oldCompact.Item1;preferences.CompactHotkeyModifiers=oldCompact.Item2;
            MessageBox.Show(this,UiText.Get(preferences.Language,"hotkeyBusy"),Text,MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
        else changed();
        capturingHide=captureCompact=false;RefreshHotkeyLabels();e.Handled=e.SuppressKeyPress=true;
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var p=new Pen(Color.FromArgb(100,PremiumTheme.Gold));e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
    protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==Keys.Escape){if(capturingHide||captureCompact){capturingHide=captureCompact=false;RefreshHotkeyLabels();}else Close();return true;}return base.ProcessCmdKey(ref message,key);}
    public void ExportPreview(string path){PremiumDialogMotion.ShowForPreview(this);using var bitmap=new Bitmap(Width,Height);DrawToBitmap(bitmap,ClientRectangle);bitmap.Save(path);Hide();}
    public void CheckLanguageMenu(OverlayPreferences preferences)
    {
        PremiumDialogMotion.ShowForPreview(this);
        for(var round=0;round<2;round++)for(var i=0;i<UiText.Languages.Length;i++)
        {
            languageMenu.Show(languageButton,new Point(0,languageButton.Height));
            ((ToolStripMenuItem)languageMenu.Items[i]).PerformClick();
            languageMenu.Close(ToolStripDropDownCloseReason.ItemClicked);Application.DoEvents();
            if(languageMenu.IsDisposed||preferences.Language!=UiText.Languages[i].Code||languageButton.TextLanguage!=preferences.Language)
                throw new InvalidOperationException("Language selection/close/reopen failed");
        }
        languageMenu.Show(languageButton,new Point(0,languageButton.Height));languageMenu.Close(ToolStripDropDownCloseReason.Keyboard);Hide();
    }
    protected override void Dispose(bool disposing){if(disposing)components.Dispose();base.Dispose(disposing);}
}

internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {using var brush=new SolidBrush(e.Item.Selected?Color.FromArgb(55,43,59):PremiumTheme.Panel);e.Graphics.FillRectangle(brush,new Rectangle(Point.Empty,e.Item.Size));}
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {using var pen=new Pen(Color.FromArgb(110,PremiumTheme.Gold));e.Graphics.DrawRectangle(pen,0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1);}
}

public sealed class PremiumToggle : Control
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)] public string TextLanguage {get;set;}="tr";
    private readonly AnimationClock animationClock=new();
    private bool check;private float position;
    private readonly System.Windows.Forms.Timer timer=new(){Interval=8};
    public event EventHandler? CheckedChanged;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Checked { get=>check;set {if(check==value)return;check=value;timer.Start();CheckedChanged?.Invoke(this,EventArgs.Empty);} }
    public PremiumToggle()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=PremiumTheme.Background;TabStop=true;
        timer.Tick+=(_,_)=>{position=FrameSmoothing.Approach(position,check?1:0,animationClock.Step(),18);Invalidate();if(Math.Abs(position-(check?1:0))<.01)timer.Stop();};
    }
    protected override void OnClick(EventArgs e){Checked=!Checked;base.OnClick(e);}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Space)Checked=!Checked;}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        PremiumTheme.Text(g,Text,new(0,0,Width-42,Height),10.5f*DeviceDpi/96f,PremiumTheme.Ink,TextLanguage);
        var box=new RectangleF(Width-34,6,33,17);using var shape=PremiumTheme.Round(box,8);using var bg=new SolidBrush(Color.FromArgb(37,41,50));g.FillPath(bg,shape);
        using var stroke=new Pen(Color.FromArgb((int)(50+position*100),PremiumTheme.Gold));g.DrawPath(stroke,shape);
        using var thumb=new SolidBrush(check?PremiumTheme.Gold:PremiumTheme.Muted);g.FillEllipse(thumb,box.X+3+position*16,box.Y+3,11,11);
    }
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
