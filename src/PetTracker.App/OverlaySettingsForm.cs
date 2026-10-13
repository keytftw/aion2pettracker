namespace PetTracker.App;

public sealed class OverlaySettingsForm : Form
{
    private readonly System.ComponentModel.IContainer components=new System.ComponentModel.Container();
    private readonly ContextMenuStrip languageMenu;
    private readonly PremiumButton languageButton;
    private bool dragging;
    private Point dragOffset;
    private void BeginDrag(Point screenPoint){dragOffset=new(screenPoint.X-Left,screenPoint.Y-Top);dragging=true;Capture=true;Cursor=Cursors.SizeAll;}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left)BeginDrag(PointToScreen(e.Location));}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging){var point=PointToScreen(e.Location);Location=new(point.X-dragOffset.X,point.Y-dragOffset.Y);}else Cursor=Cursors.SizeAll;}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);dragging=false;Capture=false;}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
    public OverlaySettingsForm(OverlayPreferences preferences,Cursor cursor,int maximumHeight,Action changed,Action logs)
    {
        ClientSize=new(340,461);FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.CenterParent;TopMost=true;ShowInTaskbar=false;
        BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;Cursor=cursor;
        var title=new Label { Location=new(18,15),AutoSize=true,ForeColor=PremiumTheme.Gold,Font=new("Segoe UI",13,FontStyle.Bold) };
        var opacityLabel=new Label { Location=new(19,60),AutoSize=true };
        var opacityValue=new Label { Location=new(260,60),AutoSize=true,ForeColor=PremiumTheme.Gold,Text=preferences.OpacityPercent+"%" };
        var opacity=new PremiumSlider { Location=new(14,79),Size=new(308,34),Value=preferences.OpacityPercent,Cursor=cursor };
        var heightLabel=new Label { Location=new(19,127),AutoSize=true };
        var heightValue=new Label { Location=new(260,127),AutoSize=true,ForeColor=PremiumTheme.Gold,Text=preferences.Height.ToString() };
        var height=new PremiumSlider { Minimum=293,Maximum=maximumHeight,Location=new(14,146),Size=new(308,34),Value=Math.Clamp(preferences.Height,293,maximumHeight),Cursor=cursor };
        var top=new PremiumToggle { Location=new(18,190),Size=new(305,29),Checked=preferences.AlwaysOnTop,Cursor=cursor };
        var locked=new PremiumToggle { Location=new(18,225),Size=new(305,29),Checked=preferences.Locked,Cursor=cursor };
        var languageLabel=new Label { Location=new(19,274),AutoSize=true };
        var languages=new PremiumButton { Location=new(18,299),Size=new(305,29),Cursor=cursor };
        languageButton=languages;
        var support=new PremiumButton { Location=new(18,339),Size=new(305,31),Cursor=cursor };
        support.Click+=(_,_)=>
        {
            try{using var browser=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.patreon.com/cw/keytftw"){UseShellExecute=true});}
            catch(System.ComponentModel.Win32Exception){MessageBox.Show(this,UiText.Get(preferences.Language,"browserFailed"),"Aion 2 Pet Tracker",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        };
        var diagnostic=new PremiumButton { Location=new(18,395),Size=new(145,29),Cursor=cursor };
        var done=new PremiumButton { Location=new(177,395),Size=new(146,29),Cursor=cursor };
        // The settings form owns this reusable menu. Closed fires inside WinForms' click
        // handling; disposing there invalidates the menu before that handling finishes.
        languageMenu=new ContextMenuStrip(components) { ShowImageMargin=false,BackColor=PremiumTheme.Panel,ForeColor=PremiumTheme.Ink,Cursor=cursor,Renderer=new DarkMenuRenderer() };
            foreach(var (code,name) in UiText.Languages)
            {
                var item=new ToolStripMenuItem(name) { Font=new(UiText.FontFamily(code),9) };
                item.Click+=(_,_)=>{preferences.Language=code;RefreshStrings();changed();};languageMenu.Items.Add(item);
            }
        languages.Click+=(_,_)=>languageMenu.Show(languages,new Point(0,languages.Height));
        diagnostic.Click+=(_,_)=>logs();
        done.Click+=(_,_)=>Close();
        var close=new PremiumButton { Text="×",Location=new(302,12),Size=new(22,23),Cursor=cursor };close.Click+=(_,_)=>Close();
        var credit=new Label { Text="Designed & Developed by Keyt",ForeColor=PremiumTheme.Muted,Font=new("Segoe UI",6),Location=new(19,436),Size=new(305,15),TextAlign=ContentAlignment.MiddleCenter };
        foreach(var label in new[]{title,opacityLabel,opacityValue,heightLabel,heightValue,languageLabel,credit})
        {label.Cursor=Cursors.SizeAll;label.MouseDown+=(_,e)=>{if(e.Button==MouseButtons.Left)BeginDrag(label.PointToScreen(e.Location));};}
        void RefreshStrings()
        {
            var lang=preferences.Language;Font=new(UiText.FontFamily(lang),9);
            title.Text=UiText.Get(lang,"settings");opacityLabel.Text=UiText.Get(lang,"opacity");heightLabel.Text=UiText.Get(lang,"height");
            top.Text=UiText.Get(lang,"top");locked.Text=UiText.Get(lang,"lock");languageLabel.Text=UiText.Get(lang,"language");
            diagnostic.Text=UiText.Get(lang,"logs");done.Text=UiText.Get(lang,"done");diagnostic.TextLanguage=lang;done.TextLanguage=lang;top.TextLanguage=lang;locked.TextLanguage=lang;Invalidate();
            languages.Text=UiText.Languages.First(l=>l.Code==lang).Name+"  ▾";languages.TextLanguage=lang;
            support.Text=UiText.Get(lang,"support");support.TextLanguage=lang;
        }
        opacity.ValueChanged+=(_,_)=>{preferences.OpacityPercent=opacity.Value;opacityValue.Text=opacity.Value+"%";changed();};
        height.ValueChanged+=(_,_)=>{preferences.Height=height.Value;heightValue.Text=height.Value.ToString();changed();};
        top.CheckedChanged+=(_,_)=>{preferences.AlwaysOnTop=top.Checked;changed();};locked.CheckedChanged+=(_,_)=>{preferences.Locked=locked.Checked;changed();};
        Controls.AddRange([title,opacityLabel,opacityValue,opacity,heightLabel,heightValue,height,top,locked,languageLabel,languages,support,diagnostic,done,close,credit]);RefreshStrings();PremiumDialogMotion.Attach(this);
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using var p=new Pen(Color.FromArgb(100,PremiumTheme.Gold));e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1);}
    protected override bool ProcessCmdKey(ref Message message,Keys key){if(key==Keys.Escape){Close();return true;}return base.ProcessCmdKey(ref message,key);}
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
    private bool check;private float position;
    private readonly System.Windows.Forms.Timer timer=new(){Interval=16};
    public event EventHandler? CheckedChanged;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Checked { get=>check;set {if(check==value)return;check=value;timer.Start();CheckedChanged?.Invoke(this,EventArgs.Empty);} }
    public PremiumToggle()
    {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);BackColor=PremiumTheme.Background;TabStop=true;
        timer.Tick+=(_,_)=>{position+=((check?1:0)-position)*.25f;Invalidate();if(Math.Abs(position-(check?1:0))<.01)timer.Stop();};
    }
    protected override void OnClick(EventArgs e){Checked=!Checked;base.OnClick(e);}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Space)Checked=!Checked;}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        PremiumTheme.Text(g,Text,new(0,0,Width-42,Height),8.5f,PremiumTheme.Ink,TextLanguage);
        var box=new RectangleF(Width-34,6,33,17);using var shape=PremiumTheme.Round(box,8);using var bg=new SolidBrush(Color.FromArgb(37,41,50));g.FillPath(bg,shape);
        using var stroke=new Pen(Color.FromArgb((int)(50+position*100),PremiumTheme.Gold));g.DrawPath(stroke,shape);
        using var thumb=new SolidBrush(check?PremiumTheme.Gold:PremiumTheme.Muted);g.FillEllipse(thumb,box.X+3+position*16,box.Y+3,11,11);
    }
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
