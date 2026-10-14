extern alias setup;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using PetTracker.App;
using PetTracker.Core;
using Xunit.Abstractions;

namespace PetTracker.UiTests;

public sealed class OverlayRenderTests(ITestOutputHelper output)
{
    [Fact]
    public void TrackerAndSetupOpenAndReuseIllustratedHandbook()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2BookAccess-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                using var tracker=new OverlayForm(false,folder,preview:true,selectedLanguage:"en");
                var open=typeof(OverlayForm).GetMethod("ShowHandbook",BindingFlags.Instance|BindingFlags.NonPublic)!;
                open.Invoke(tracker,null);
                var book=tracker.OwnedForms.OfType<HandbookForm>().Single();Assert.True(book.Visible);Assert.Equal(8,book.PageCount);
                open.Invoke(tracker,null);Assert.Single(tracker.OwnedForms.OfType<HandbookForm>());book.Close();
                using var installer=new setup::PetTracker.Setup.SetupForm();
                installer.ExportInstallingPreview(Path.Combine(folder,"setup.png"),"en",3,1);
                var buttons=installer.Controls.Cast<Control>().SelectMany(c=>c.Controls.Cast<Control>()).OfType<setup::PetTracker.App.PremiumButton>();
                var button=Assert.Single(buttons,b=>b.Text==HelpText.Get("en","handbook"));
                typeof(Control).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(button,[EventArgs.Empty]);
                var setupBook=Assert.Single(installer.OwnedForms);Assert.True(setupBook.Visible);
                typeof(Control).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(button,[EventArgs.Empty]);
                Assert.Same(setupBook,Assert.Single(installer.OwnedForms));setupBook.Close();
            }
            finally{Directory.Delete(folder,true);}
        });
    }

    [Fact]
    public void LocalTestStartupSkipsUpdaterAndReusesOnlyItsOwnConsent()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2DirectUi-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                using(var first=new StartupForm(stateFolder:folder,localTest:true))
                {
                    var gate=(StartupGate)typeof(StartupForm).GetField("gate",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(first)!;
                    Assert.True((bool)typeof(StartupForm).GetField("updateDone",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(first)!);
                    gate.FinishIntro(4);Assert.Equal(StartupStage.Language,gate.Stage);
                    Assert.Null(typeof(StartupForm).GetField("controller",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(first));
                }
                new InstallationProfile("tr",TermsContent.Version,DateTimeOffset.UtcNow).Save(Path.Combine(folder,"test-consent.json"));
                new OverlayPreferences{Language="tr"}.Save(Path.Combine(folder,"overlay-preferences.json"));
                using var next=new StartupForm(stateFolder:folder,localTest:true);
                var reused=(StartupGate)typeof(StartupForm).GetField("gate",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(next)!;
                reused.FinishIntro(4);Assert.Equal(StartupStage.Synchronizing,reused.Stage);Assert.Equal("tr",reused.Language);
            }
            finally{Directory.Delete(folder,true);}
        });
    }

    [Fact]
    public void TeleportDiagnosticWindowRendersWithoutChangingQualityOrStartingCapture()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2TeleportUi-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                using var overlay=new OverlayForm(false,folder,preview:true);var before=overlay.Controller.Tracker.Quality;
                using var test=new TeleportTestForm(overlay.Controller);test.Show();Application.DoEvents();
                Assert.Equal(before,overlay.Controller.Tracker.Quality);Assert.False(overlay.Controller.Running);
                using var image=new Bitmap(test.Width,test.Height);test.DrawToBitmap(image,new(0,0,test.Width,test.Height));
                var outputFolder=Environment.GetEnvironmentVariable("PETTRACKER_TELEPORT_PREVIEW");
                if(outputFolder is not null){Directory.CreateDirectory(outputFolder);image.Save(Path.Combine(outputFolder,"teleport-test.png"));}
                Assert.Contains(test.Controls.OfType<Label>(),l=>l.Text.Contains("Alınan collection"));
            }
            finally{Directory.Delete(folder,true);}
        });
    }

    [Fact]
    public void HandbookAndInstallingScreenRenderInEveryLanguageWithoutRunningCaptureOrInstaller()
    {
        OnSta(()=>
        {
            Assert.True(HelpText.Complete);
            var target=Environment.GetEnvironmentVariable("PETTRACKER_REVIEW_IMAGES");
            var temp=Path.Combine(Path.GetTempPath(),"A2HelpUi-"+Guid.NewGuid());Directory.CreateDirectory(temp);
            try
            {
                foreach(var (lang,_) in UiText.Languages)
                {
                    using var handbook=new HandbookForm(lang);
                    for(var page=0;page<handbook.PageCount;page++)
                    {handbook.SelectPage(page);handbook.ExportPreview(Path.Combine(temp,"handbook.png"));}
                    using var installer=new setup::PetTracker.Setup.SetupForm();
                    installer.ExportInstallingPreview(Path.Combine(temp,"installing.png"),lang,3,1);
                    if(target is not null&&lang is "en" or "tr")
                    {Directory.CreateDirectory(target);File.Copy(Path.Combine(temp,"installing.png"),Path.Combine(target,"installing-"+lang+".png"),true);handbook.SelectPage(2);handbook.ExportPreview(Path.Combine(target,"handbook-"+lang+".png"));}
                }
                if(target is not null)
                {
                    using var form=new OverlayForm(false,temp,preview:true,selectedLanguage:"en");form.Show();form.ExportPreview(Path.Combine(target,"tracker.png"));
                    form.ExportDialogs(target);
                    var portraits=(IReadOnlyDictionary<int,Image>)typeof(OverlayForm).GetField("portraits",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(form)!;
                    using var atlas=new PetMapForm(form.Controller,portraits,new(){Language="en",MapPortraitMarkers=true,MapShowAllFarmable=true},()=>{});
                    atlas.Show();using var bitmap=new Bitmap(atlas.Width,atlas.Height);atlas.DrawToBitmap(bitmap,atlas.ClientRectangle);bitmap.Save(Path.Combine(target,"atlas.png"));
                    form.Controller.Tracker.Gap("synthetic unsupported soul");form.Refresh();form.ExportPreview(Path.Combine(target,"uncertain.png"));
                }
            }
            finally{Directory.Delete(temp,true);}
        });
    }

    [Fact]
    public void UncertainCounterShowsRedFooterAndSnapshotClearsIt()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2WarningUi-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                using var form=new OverlayForm(false,folder,preview:true,selectedLanguage:"en");
                form.Show();var tracker=form.Controller.Tracker;var snapshot=tracker.Current.Snapshot!;
                int RedPixels()
                {
                    using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new(0,0,form.Width,form.Height));
                    var count=0;for(var y=Math.Max(0,bitmap.Height-60);y<bitmap.Height;y++)for(var x=15;x<bitmap.Width-30;x++)
                    {var c=bitmap.GetPixel(x,y);if(c.R>180&&c.G<140&&c.B<150)count++;}return count;
                }
                tracker.Gap("synthetic unsupported soul");form.Refresh();Assert.True(RedPixels()>20);
                tracker.Select(snapshot.Scope);tracker.Current.Snapshot=null;
                tracker.Select(snapshot.Scope);form.Refresh();Assert.Equal(DataQuality.WaitingForSnapshot,tracker.Quality);Assert.True(RedPixels()>20);
                tracker.Synchronize(snapshot with {FrameId=snapshot.FrameId+100});form.Refresh();
                Assert.Equal(DataQuality.Exact,tracker.Quality);Assert.Equal(0,RedPixels());
            }
            finally{Directory.Delete(folder,true);}
        });
    }

    [Fact]
    public void AtlasRegionAndFarmableTogglesUpdateMarkersAndPersistPreferences()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2AtlasUi-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                using var tracker=new OverlayForm(false,folder,preview:true);
                var prefs=new OverlayPreferences{Language="tr"};var saves=0;
                using var map=new PetMapForm(tracker.Controller,new Dictionary<int,Image>(),prefs,()=>saves++);
                T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(obj)!;
                var data=Field<MapData>(map,"data");var world=Field<int>(map,"worldId");
                Assert.DoesNotContain(map.Controls.Cast<Control>(),control=>control.Text=="Konumu olmayan petleri gizle");
                var portrait=Field<CheckBox>(map,"portraitMarkers");portrait.Checked=false;Assert.False(prefs.MapPortraitMarkers);
                Assert.All(Field<PetDefinition[]>(map,"matches"),pet=>Assert.NotEmpty(data.Locations(pet.Id,world)));
                var all=Field<CheckBox>(map,"showAll");all.Checked=true;Assert.True(prefs.MapShowAllFarmable);
                var canvas=Field<PetMapCanvas>(map,"canvas");
                Assert.Equal(MapPetQuery.FarmableLocations(tracker.Controller.Catalog,data,world,tracker.Controller.Tracker.KnownTotal).Length,Field<MapLocation[]>(canvas,"markers").Length);
                map.FocusPet(1114);all.Checked=false;
                Assert.Equal(data.Locations(1114,Field<int>(map,"worldId")).Count,Field<MapLocation[]>(canvas,"markers").Length);
                Assert.True(saves>=3);
            }
            finally{Directory.Delete(folder,true);}
        });
    }

    [Fact]
    public void StartupCloseAndScopeCleanupCanDisposeTheFormAgain()
    {
        OnSta(()=>
        {
            using var form=new StartupForm(demo:true);
            form.StartPosition=FormStartPosition.Manual;form.Location=new(-5000,-5000);
            form.Shown+=(_,_)=>form.BeginInvoke(()=>form.Close());
            Application.Run(form);
            Assert.True(form.IsDisposed);
            form.Dispose();
        });
    }

    private static void OnSta(Action action)
    {
        Exception? failure=null;
        var thread=new Thread(()=>{try{action();}catch(Exception ex){failure=ex;}});
        thread.SetApartmentState(ApartmentState.STA);thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(60)),"Offline UI test timed out");
        if(failure is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [Fact]
    public void SevenVisibleCardsRenderWithLargeTrackedCollection()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2TrackerUi-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                new OverlayPreferences{VisibleCards=7}.Save(Path.Combine(folder,"overlay-preferences.json"));
                using var form=new OverlayForm(false,folder,preview:true);_=form.Handle;
                foreach(var id in form.Controller.Catalog.Pets.Keys.Take(150))form.Controller.Tracker.Pin(id,true);
                typeof(OverlayForm).GetMethod("SyncCards",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                form.ExportPreview(Path.Combine(folder,"warmup.png"));
                using var image=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);
                for(var i=0;i<15;i++)form.DrawToBitmap(image,form.ClientRectangle);
                var samples=new List<double>();long allocations=GC.GetAllocatedBytesForCurrentThread();
                for(var i=0;i<90;i++){var clock=Stopwatch.StartNew();form.DrawToBitmap(image,form.ClientRectangle);samples.Add(clock.Elapsed.TotalMilliseconds);}
                allocations=GC.GetAllocatedBytesForCurrentThread()-allocations;samples.Sort();
                output.WriteLine($"Seven-card paint, 150 tracked pets: median={samples[45]:F2} ms; p95={samples[85]:F2} ms; allocations/frame={allocations/90:N0} bytes; DPI={form.DeviceDpi}");
                Assert.True(form.Controller.Tracker.VisibleIds.Count()>=150);Assert.False(form.Controller.Running);
            }
            finally{Directory.Delete(folder,true);}
        });
    }

    [Fact]
    public void CompactSizeSurvivesMoveCompletionAndNewPetGrowth()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2TrackerUi-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                using var form=new OverlayForm(false,folder,preview:true);_=form.Handle;
                typeof(OverlayForm).GetMethod("ToggleCompact",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                var compact=form.ClientSize;SendMessage(form.Handle,0x232,IntPtr.Zero,IntPtr.Zero);
                Assert.Equal(compact,form.ClientSize);
                foreach(var id in form.Controller.Catalog.Pets.Keys.Take(15))form.Controller.Tracker.Pin(id,true);
                typeof(OverlayForm).GetMethod("SyncCards",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                Assert.Equal(compact,form.ClientSize);
                typeof(OverlayForm).GetMethod("ToggleCompact",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                Assert.True(form.ClientSize.Height>compact.Height);Assert.False(form.Controller.Running);
            }
            finally{Directory.Delete(folder,true);}
        });
    }
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);

    [Fact]
    public void ScrolledPortraitsCannotOverwriteHeader()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2TrackerUi-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                using var form=new OverlayForm(false,folder,preview:true);_=form.Handle;form.ExportPreview(Path.Combine(folder,"warmup.png"));
                using var before=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);form.DrawToBitmap(before,form.ClientRectangle);
                typeof(OverlayForm).GetField("scroll",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(form,71f);
                using var after=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);form.DrawToBitmap(after,form.ClientRectangle);
                var headerHeight=(int)(80*form.DeviceDpi/96f*4f/3f);
                for(var y=0;y<headerHeight;y++)for(var x=0;x<before.Width;x++)Assert.Equal(before.GetPixel(x,y),after.GetPixel(x,y));
            }
            finally{Directory.Delete(folder,true);}
        });
    }

    [Theory][InlineData(8)][InlineData(16)]
    public void MeasureNativeTimerCadence(int interval)
    {
        OnSta(()=>
        {
            using var loop=new ApplicationContext();using var timer=new System.Windows.Forms.Timer{Interval=interval};
            var samples=new List<double>();var clock=Stopwatch.StartNew();double previous=0;
            timer.Tick+=(_,_)=>{var now=clock.Elapsed.TotalMilliseconds;if(previous>0)samples.Add(now-previous);previous=now;if(samples.Count>=60){timer.Stop();loop.ExitThread();}};
            timer.Start();Application.Run(loop);samples.Sort();
            output.WriteLine($"Native timer interval {interval} ms: median tick={samples[30]:F2} ms; p95={samples[57]:F2} ms");
            Assert.Equal(60,samples.Count);
        });
    }
}
