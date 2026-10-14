using PetTracker.Core;
namespace PetTracker.App;

public sealed class TeleportTestForm:Form
{
    private readonly OverlayController controller;
    private readonly System.Windows.Forms.Timer refresh=new(){Interval=500};
    private readonly Label evidence;
    private readonly TextBox report;
    private long receivedAtStart,appliedAtStart;
    private DateTimeOffset started;
    private readonly List<string> baseline=[];
    private string? reportPath;
    private string fingerprint="";
    public TeleportTestForm(OverlayController controller)
    {
        this.controller=controller;Text="Soul / Collection Test · LOCAL 1.2.7";AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new(96,96);
        ClientSize=new(700,520);MinimumSize=Size;StartPosition=FormStartPosition.CenterParent;BackColor=PremiumTheme.Background;ForeColor=PremiumTheme.Ink;
        var instructions=new Label{Text="1. Önce Synced ol. Oyundaki pet level/soul değerinin fotoğrafını çek.\n2. Testi başlat → birkaç tekli, ardından toplu auto-loot kazanımı dene.\n3. Uyarı çıkarsa oyun + tracker fotoğrafını çek; raporu kaydet.\nTest verileri yerelde tutulur; sunucuya otomatik gönderilmez.",Dock=DockStyle.Top,Height=112,Padding=new(16),ForeColor=PremiumTheme.Ink,Font=new("Segoe UI",10)};
        evidence=new(){Dock=DockStyle.Top,Height=74,Padding=new(16,8,16,0),ForeColor=PremiumTheme.Gold,Font=new("Segoe UI",11)};
        report=new(){Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,BackColor=PremiumTheme.Panel,ForeColor=PremiumTheme.Ink,Font=new("Consolas",9)};
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=64,Padding=new(12),BackColor=PremiumTheme.Background};
        PremiumButton Button(string text,Action action){var button=new PremiumButton{Text=text,TextSize=10,DpiAwareText=true,Size=new(210,36)};button.Click+=(_,_)=>action();buttons.Controls.Add(button);return button;}
        Button("Testi başlat / yeniden başlat",StartTest);Button("Bitir ve raporu kaydet",SaveReport);Button("Raporu kopyala",()=>{try{Clipboard.SetText(BuildReport());}catch(System.Runtime.InteropServices.ExternalException){MessageBox.Show(this,"Panoya kopyalanamadı; raporu dosyaya kaydet.");}});
        Controls.AddRange([report,evidence,instructions,buttons]);refresh.Tick+=(_,_)=>RefreshEvidence();refresh.Start();StartTest();
    }
    private void StartTest()
    {
        receivedAtStart=controller.Session.CollectionsReceived;appliedAtStart=controller.Session.CollectionsApplied;started=DateTimeOffset.Now;reportPath=null;
        fingerprint="";baseline.Clear();baseline.AddRange(Counters());controller.Log("[TELEPORT TEST START] Kullanıcı test işareti. Kesinlik="+controller.Tracker.Quality);RefreshEvidence();
    }
    private IEnumerable<string> Counters()=>controller.Tracker.HasCurrent?controller.Tracker.Current.LastKnownTotals.OrderBy(p=>p.Key).Select(p=>$"Pet ID {p.Key}: {p.Value}"):new[]{"Collection başlangıç verisi yok."};
    private string BuildReport()
    {
        var lines=new List<string>{"Aion 2 Pet Tracker · Local soul / collection test 1.2.7",$"Başlangıç: {started:O}",$"Rapor zamanı: {DateTimeOffset.Now:O}",$"Alınan collection: {controller.Session.CollectionsReceived-receivedAtStart}",$"Uygulanan collection: {controller.Session.CollectionsApplied-appliedAtStart}",$"Karakter kimliği biliniyor: {controller.Session.IdentityKnown}",$"Kesinlik: {controller.Tracker.Quality}",controller.Session.CollectionStatus,"","BAŞLANGIÇ SAYACLARI"};
        lines.AddRange(baseline);lines.Add("\nSON SAYAÇLAR");lines.AddRange(Counters());lines.Add("\nSORUNLU SOUL MESAJLARI (son 128; her mesajdan en fazla 64 bayt)");lines.AddRange(controller.SoulDiagnostics);lines.Add("\nLOG (son 500 satır; uzun testlerde tracker.log dosyasını da ekle)");lines.AddRange(controller.Logs);return string.Join(Environment.NewLine,lines);
    }
    private void RefreshEvidence()
    {
        var rx=controller.Session.CollectionsReceived-receivedAtStart;var applied=controller.Session.CollectionsApplied-appliedAtStart;
        var current=$"{rx}/{applied}/{controller.Tracker.Quality}/{controller.Logs.LastOrDefault()}";if(current==fingerprint)return;fingerprint=current;
        evidence.Text=$"Alınan collection: {rx}     Uygulanan collection: {applied}     Kesinlik: {controller.Tracker.Quality}\n"+(applied>0?"Yeni collection uygulandı. Teleport saatini log ile karşılaştıracağız.":rx>0?"Collection geldi; henüz güvenle uygulanmadı. Kimlik veya sıralama beklenebilir.":"Yeni collection bekleniyor. Sadece teleport yapmak veri geldiğini kanıtlamaz.");
        var text=BuildReport();if(report.Text!=text){var position=report.SelectionStart;report.Text=text;report.SelectionStart=Math.Min(position,text.Length);}
    }
    private void SaveReport()
    {
        try
        {
            controller.Log("[TELEPORT TEST END] Kullanıcı test bitiş işareti.");
            var folder=Path.Combine(controller.Folder,"diagnostics");Directory.CreateDirectory(folder);
            reportPath=Path.Combine(folder,"teleport-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".txt");File.WriteAllText(reportPath,BuildReport());
            MessageBox.Show(this,"Rapor kaydedildi:\n"+reportPath+"\n\nBu dosyayı ve teleport saatini bana gönder.",Text,MessageBoxButtons.OK,MessageBoxIcon.Information);
            using var process=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe"){Arguments="/select,\""+reportPath+"\"",UseShellExecute=true});
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception){MessageBox.Show(this,"Rapor kaydedilemedi/açılamadı: "+ex.Message,Text);}
    }
    protected override void Dispose(bool disposing){if(disposing)refresh.Dispose();base.Dispose(disposing);}
}
