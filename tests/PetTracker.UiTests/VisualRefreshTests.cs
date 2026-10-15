using System.Diagnostics;
using System.Reflection;
using PetTracker.App;
using PetTracker.Core;
using PetTracker.Capture;
using Xunit.Abstractions;
namespace PetTracker.UiTests;
public sealed class VisualRefreshTests(ITestOutputHelper output)
{
    [Fact] public void SettingsAndRiftAreModelessAndOpenBesideTracker()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2Windows-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                using var tracker=new OverlayForm(false,folder,preview:true,selectedLanguage:"en");
                tracker.StartPosition=FormStartPosition.Manual;tracker.Location=new(30,30);tracker.Show();
                ((System.Windows.Forms.Timer)Field(tracker,"timer")!).Stop();
                Call(tracker,"ShowSettings");var settings=(OverlaySettingsForm)Field(tracker,"settingsWindow")!;
                Assert.True(tracker.Enabled);Assert.False(settings.Modal);Assert.True(settings.TopMost);
                if(Screen.FromControl(tracker).WorkingArea.Width>=tracker.Width+settings.Width+38)Assert.True(settings.Left>=tracker.Right);
                Call(tracker,"OpenRiftSettings");var rift=(RiftSettingsForm)Field(tracker,"riftSettingsWindow")!;
                Assert.True(tracker.Enabled);Assert.True(settings.Enabled);Assert.False(rift.Modal);Assert.True(rift.TopMost);
                Call(tracker,"OpenRiftSettings");Assert.Same(rift,Field(tracker,"riftSettingsWindow"));
                rift.Close();settings.Close();
            }
            finally{Directory.Delete(folder,true);}
        });
    }
    [Fact] public void RiftMovesBetweenTrackerAndSingleTopLeftNoticeAndRendersNewSettings()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2VisualRefresh-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                var target=Environment.GetEnvironmentVariable("PETTRACKER_REVIEW_IMAGES")??folder;Directory.CreateDirectory(target);
                using var tracker=new OverlayForm(false,folder,preview:true,selectedLanguage:"en");
                var timer=(System.Windows.Forms.Timer)Field(tracker,"timer")!;timer.Stop();
                foreach(var id in tracker.Controller.Tracker.VisibleIds.ToArray())tracker.Controller.Tracker.Dismiss(id);
                tracker.Controller.Tracker.Synchronize(new(tracker.Controller.Tracker.Current.Scope,"visual",0,[new(1038,2,38),new(1125,2,46)]));
                tracker.Controller.Tracker.Pin(1038,true);tracker.Controller.Tracker.Pin(1125,true);Call(tracker,"SyncCards");
                var scope=tracker.Controller.Tracker.Current.Scope;
                tracker.Controller.Session.Accept(new StreamBegan("visual"));
                tracker.Controller.Session.Accept(new OwnIdentity("visual",1,150,scope));
                tracker.Controller.Session.Accept(new CollectionReceived("visual",2,[new(1038,2,38),new(1125,2,46)]));
                tracker.Controller.Session.Accept(new NpcObserved("visual",200,2400578));
                tracker.Controller.Session.Accept(new TargetObserved("visual",150,200));
                tracker.Controller.Session.Accept(new NpcObserved("visual",201,2400567));
                tracker.Controller.Session.Accept(new TargetObserved("visual",150,201));
                Assert.Equal(2400578,tracker.Controller.Session.ObservedPetNpcs[1125]);
                Assert.Equal(2400567,tracker.Controller.Session.ObservedPetNpcs[1038]);
                tracker.StartPosition=FormStartPosition.Manual;tracker.Location=new(-5000,-5000);tracker.Show();
                var baseHeight=tracker.Height;
                tracker.ExportRiftPreview(Path.Combine(target,"rift-reminder.png"));Assert.True(tracker.Height>baseHeight);
                Assert.Null(Field(tracker,"detachedRift"));
                tracker.ExportRiftPreview(Path.Combine(target,"rift-opened.png"),opened:true);
                var age=((RiftBannerFrame)Field(tracker,"riftFrame")!).Age;
                tracker.Hide();Call(tracker,"RouteRiftBanner");
                var detached=(RiftToast)Field(tracker,"detachedRift")!;Assert.True(detached.Visible);
                var area=Screen.FromControl(tracker).WorkingArea;Assert.Equal(new Point(area.Left+15,area.Top+15),detached.Location);
                Assert.Equal(0,(int)Field(tracker,"bannerPixels")!);Assert.False(detached.ShowInTaskbar);
                using(var shot=new Bitmap(detached.Width,detached.Height)){detached.DrawToBitmap(shot,detached.ClientRectangle);shot.Save(Path.Combine(target,"rift-detached.png"));}
                tracker.Show();Call(tracker,"RouteRiftBanner");Assert.True(detached.IsDisposed);Assert.Null(Field(tracker,"detachedRift"));
                Assert.Equal(age,((RiftBannerFrame)Field(tracker,"riftFrame")!).Age);
                tracker.WindowState=FormWindowState.Minimized;Call(tracker,"RouteRiftBanner");
                var minimizedNotice=(RiftToast)Field(tracker,"detachedRift")!;Assert.True(minimizedNotice.Visible);Assert.Equal(FormWindowState.Minimized,tracker.WindowState);
                Call(tracker,"RouteRiftBanner");Assert.Same(minimizedNotice,Field(tracker,"detachedRift"));
                tracker.WindowState=FormWindowState.Normal;Call(tracker,"RouteRiftBanner");Assert.True(minimizedNotice.IsDisposed);
                typeof(OverlayForm).GetField("riftFrame",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(tracker,default(RiftBannerFrame));Call(tracker,"RouteRiftBanner");Assert.Equal(baseHeight,tracker.Height);
                foreach(var (language,_) in UiText.Languages)
                {
                    var prefs=new OverlayPreferences{Language=language,RiftSoundEnabled=false};
                    using var settings=new OverlaySettingsForm(prefs,Cursors.Default,7,()=>{},()=>{},riftSettings:()=>{});
                    settings.ExportPreview(Path.Combine(target,"settings-"+language+".png"));
                    var buttons=settings.Controls.OfType<PremiumButton>().ToArray();
                    var support=buttons.Single(b=>b.Text==UiText.Get(language,"support"));var mail=buttons.Single(b=>b.Text=="support@keytdev.com");
                    var credit=settings.Controls.OfType<PremiumLabel>().Single(l=>l.Text.Contains("Designed"));
                    Assert.True(support.Top>credit.Bottom);Assert.True(support.Bottom<mail.Top);
                    Assert.Equal("Ctrl+P",((PremiumButton)Field(settings,"hideHotkeyButton")!).BadgeText);
                    using var riftSettings=new RiftSettingsForm(prefs,()=>RiftClock.Region("1301"),()=>{},(_,_)=>{});
                    riftSettings.StartPosition=FormStartPosition.Manual;riftSettings.Location=new(-5000,-5000);riftSettings.Show();
                    using var screenshot=new Bitmap(riftSettings.Width,riftSettings.Height);riftSettings.DrawToBitmap(screenshot,riftSettings.ClientRectangle);
                    if(language=="en")screenshot.Save(Path.Combine(target,"rift-settings.png"));
                }
                tracker.ExportPreview(Path.Combine(target,"monster-names.png"));
                foreach(var id in tracker.Controller.Catalog.Pets.Keys.Take(150))tracker.Controller.Tracker.Pin(id,true);Call(tracker,"SyncCards");
                typeof(OverlayForm).GetField("hoveredCard",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(tracker,tracker.Controller.Tracker.VisibleIds.First());
                var hover=(Dictionary<int,float>)Field(tracker,"hover")!;hover[tracker.Controller.Tracker.VisibleIds.First()]=1;
                tracker.ExportRiftPreview(Path.Combine(target,"performance-warmup.png"));
                using var bitmap=new Bitmap(tracker.Width,tracker.Height);var samples=new List<double>();
                for(var i=0;i<15;i++)tracker.DrawToBitmap(bitmap,tracker.ClientRectangle);
                var allocated=GC.GetAllocatedBytesForCurrentThread();
                for(var i=0;i<120;i++){var clock=Stopwatch.StartNew();tracker.DrawToBitmap(bitmap,tracker.ClientRectangle);samples.Add(clock.Elapsed.TotalMilliseconds);}
                allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
                samples.Sort();output.WriteLine($"Seven-card visual refresh with 150 tracked pets, hover sweep and Rift banner: median {samples[60]:F2} ms, p95 {samples[114]:F2} ms, allocated {allocated/120:N0} bytes/frame");
                Assert.False(tracker.Controller.Running);
            }
            finally{Directory.Delete(folder,true);}
        });
    }
    private static object? Field(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o);
    private static void Call(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,null);
    private static void OnSta(Action action)
    {Exception? error=null;var thread=new Thread(()=>{try{action();}catch(Exception ex){error=ex;}});thread.SetApartmentState(ApartmentState.STA);thread.Start();Assert.True(thread.Join(TimeSpan.FromSeconds(60)));if(error is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();}
}
