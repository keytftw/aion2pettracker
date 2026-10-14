namespace PetTracker.App;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
#if !LOCAL_TEST
        Velopack.VelopackApp.Build().SetAutoApplyOnStartup(false).Run();
#endif
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.SetDefaultFont(new System.Drawing.Font(PetTracker.App.UiText.FontFamily("en"),9f));
        if(args.Contains("--startup-drag-check"))
        {try{using var startup=new StartupForm(demo:true);startup.VerifyDragging();}catch(Exception ex){Console.Error.WriteLine(ex);Environment.ExitCode=1;}return;}
        if(args.Contains("--language-check"))
        {
            var failures=new List<Exception>();Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=(_,e)=>failures.Add(e.Exception);
            try
            {
                var preferences=new OverlayPreferences();var changes=0;
                using(var settings=new OverlaySettingsForm(preferences,Cursors.Default,1200,()=>changes++,()=>{}))settings.CheckLanguageMenu(preferences);
                if(changes!=20||failures.Count!=0)throw new InvalidOperationException("Language menu callbacks/errors: "+changes+"/"+failures.Count);
            }
            catch{Environment.ExitCode=1;}
            return;
        }
        if(args.Contains("--startup-check"))
        {
            var folder=Path.Combine(AppContext.BaseDirectory,"smoke-check","Flow");Directory.CreateDirectory(folder);using var startup=new StartupForm(demo:true,stateFolder:folder);startup.Shown+=(_,_)=>startup.BeginDemoCheck();Application.Run(startup);if(!File.Exists(Path.Combine(folder,"flow-result.txt")))Environment.ExitCode=1;return;
        }
        if (args.Contains("--overlay-preview"))
        {
            var folder = Path.GetFullPath(args.Last()); Directory.CreateDirectory(folder);
            using var overlay = new OverlayForm(false, Path.Combine(folder,"demo-state"), preview:true);
            overlay.Shown += (_, _) =>
            {
                overlay.ExportPreview(Path.Combine(folder,"overlay.png"));overlay.ExportPreview(Path.Combine(folder,"overlay-clear.png"),clear:true);
                overlay.ExportDialogs(folder);AppBrand.SaveIcon(Path.Combine(folder,"app.ico"));using(var emblem=AppBrand.Draw(256))emblem.Save(Path.Combine(folder,"app-icon.png"));
                foreach(var stage in new[]{PetTracker.Core.StartupStage.Intro,PetTracker.Core.StartupStage.Language,PetTracker.Core.StartupStage.Terms,PetTracker.Core.StartupStage.Synchronizing}){using var startup=new StartupForm(demo:false,stateFolder:Path.Combine(folder,"startup-demo"));startup.ExportStep(Path.Combine(folder,"startup-"+stage+".png"),stage); }overlay.Close();
            };
            Application.Run(overlay); return;
        }
        if (args.Contains("--smoke-check"))
        {
            var folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "smoke-check"));
            try
            {
                using var live = new OverlayForm(true, Path.Combine(folder, "Live"));
                using var demo = new OverlayForm(false, Path.Combine(folder, "Demo"), preview:true);
                live.CreateControl(); demo.CreateControl();
                var initialWidth = live.Width; live.Width += 100;
                if (live.Width != initialWidth || live.Height < 220 || live.Width != (int)Math.Round(OverlayForm.FixedWidth*live.DeviceDpi/96f) || !live.TopMost || live.Opacity < .3 || live.Opacity > 1) throw new InvalidOperationException("Overlay genişlik/yükseklik/üstte kalma/saydamlık kontrolü başarısız");
                live.Height += 60;
                if (live.Width != initialWidth) throw new InvalidOperationException("Yükseklik değişimi genişliği etkiledi");
                var lockedFolder=Path.Combine(folder,"Locked"); Directory.CreateDirectory(lockedFolder);
                File.WriteAllText(Path.Combine(lockedFolder,"overlay-preferences.json"),"{\"Locked\":true,\"OpacityPercent\":20,\"Height\":610}");
                using var locked=new OverlayForm(true,lockedFolder);
                if (!locked.PositionLocked || Math.Abs(locked.Opacity-.3)>.001) throw new InvalidOperationException("Kilit/saydamlık alt sınırı başarısız");
                locked.CheckLockedMove();
                if(!UiText.Complete||!StartupText.Complete||!TermsContent.Complete||UiText.Languages.Length!=10)throw new InvalidOperationException("Dil sözlüğü eksik.");
                foreach(var (code,_) in UiText.Languages)
                {
                    var languageFolder=Path.Combine(folder,"Languages",code);Directory.CreateDirectory(languageFolder);
                    File.WriteAllText(Path.Combine(languageFolder,"overlay-preferences.json"),$"{{\"Language\":\"{code}\",\"LayoutVersion\":2,\"Height\":350}}");
                    using(var startup=new StartupForm(demo:true,stateFolder:languageFolder))startup.VerifyConsentControls(code);
                    using var localized=new OverlayForm(false,languageFolder,preview:true);localized.CreateControl();
                    localized.ExportPreview(Path.Combine(languageFolder,"overlay.png"));
                }
                var resetFolder=Path.Combine(folder,"Reset");Directory.CreateDirectory(resetFolder);var scope=new PetTracker.Core.CharacterScope("Test","2312");var saved=new PetTracker.Core.SavedState();var profile=new PetTracker.Core.ProfileState{Scope=scope,Snapshot=new(scope,"old",0,[new(1114,2,74)]),LastKnownTotals=new(){{1114,104}}};profile.Preferences.Pinned.Add(1114);saved.Profiles[scope.Key]=profile;new PetTracker.Core.StateStore(Path.Combine(resetFolder,"state.json"),_=>{}).Save(saved);var reset=new OverlayController(true,resetFolder);if(reset.Tracker.KnownTotal(1114) is not null||reset.Tracker.Current.Snapshot is not null||!reset.Tracker.Current.Preferences.Pinned.Contains(1114))throw new InvalidOperationException("Fresh launch reset/pin separation failed");reset.DisposeAsync().AsTask().GetAwaiter().GetResult();
                File.WriteAllText(Path.Combine(folder, "result.txt"), "PASS: size/lock/opacity; ten languages and unchecked/checked consent; cached counters reset, pins retained. No live capture started.");
            }
            catch (Exception ex)
            {
                Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "result.txt"), "FAIL: " + ex);
                Environment.ExitCode = 1;
            }
            return;
        }
        try
        {
#if !LOCAL_TEST
            if(!args.Contains("--demo"))
            {
                var manager=new Velopack.UpdateManager(GitHubUpdater.Repository);
                if(!manager.IsInstalled||manager.IsPortable||InstallationProfile.Load(InstallationProfile.ReceiptPath) is null)
                {
                    MessageBox.Show("Please install Aion 2 Pet Tracker using the new Setup. Language and terms are configured during installation.\n\nLütfen yeni Setup ile kurulum yapın. Dil ve kullanım koşulları kurulum sırasında seçilir.","Aion 2 Pet Tracker — Setup required",MessageBoxButtons.OK,MessageBoxIcon.Information);
                    return;
                }
            }
#endif
            using var instance=new Mutex(true,"Local\\Aion2PetTracker-"+(args.Contains("--demo")?"Demo":"Live"),out var first);
            if(!first){
#if LOCAL_TEST
                MessageBox.Show("Önce açık tracker’ı kapat, ardından test sürümünü çalıştır.","Pet Tracker · LOCAL TEST");
#else
                ExistingInstanceActivation.Notify(args.Contains("--demo"));
#endif
                return;}
            try
            {
#if LOCAL_TEST
                var testFolder=Path.Combine(InstallationProfile.DataFolder,"LocalTest");
                using var startup=new StartupForm(demo:args.Contains("--demo"),stateFolder:testFolder,localTest:true);
#else
                using var startup=new StartupForm(demo:args.Contains("--demo"),installedOnboarding:!args.Contains("--demo"));
#endif
                using var activation=new ExistingInstanceActivation(args.Contains("--demo"),()=>
                {
                    var overlay=Application.OpenForms.OfType<OverlayForm>().FirstOrDefault(form=>!form.IsDisposed);
                    if(overlay is not null)overlay.RestoreFromShortcut();
                    else if(!startup.IsDisposed){startup.Show();startup.WindowState=FormWindowState.Normal;startup.BringToFront();startup.Activate();}
                });
                Application.Run(startup);
            }
            finally{instance.ReleaseMutex();}
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Pet Tracker başlatılamadı", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }    
}
