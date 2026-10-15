using System.Reflection;
using PetTracker.App;
using PetTracker.Core;

namespace PetTracker.UiTests;

public sealed class StartupCompactTests
{
    [Fact]
    public void WaitingWindowCompactsRestoresAndMinimizesWithoutStartingCapture()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2CompactStartup-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            var output=Environment.GetEnvironmentVariable("PETTRACKER_REVIEW_IMAGES");
            try
            {
                Assert.True(StartupText.Complete);
                foreach(var (language,_) in UiText.Languages)
                {
                    using var form=new StartupForm(stateFolder:folder);
                    form.ExportStep(Path.Combine(folder,"compact.png"),StartupStage.Synchronizing,language,compact:true);
                    Assert.Equal((int)Math.Round(460*form.DeviceDpi/96f),form.ClientSize.Width);
                    Assert.Equal((int)Math.Round(64*form.DeviceDpi/96f),form.ClientSize.Height);
                    Assert.Null(typeof(StartupForm).GetField("controller",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(form));
                    if(output is not null&&language is "en" or "tr")
                    {Directory.CreateDirectory(output);File.Copy(Path.Combine(folder,"compact.png"),Path.Combine(output,"waiting-compact-"+language+".png"),true);}
                    typeof(StartupForm).GetMethod("ToggleSynchronizationCompact",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                    Assert.Equal((int)Math.Round(700*form.DeviceDpi/96f),form.ClientSize.Width);
                    var compact=form.Controls.OfType<PremiumButton>().Single(b=>b.Text==UiText.Get(language,"compact"));
                    var minimize=form.Controls.OfType<PremiumButton>().Single(b=>b.AccessibleName==UiText.Get(language,"minimize"));
                    Assert.True(compact.Left>form.Controls.OfType<PremiumButton>().Single(b=>b.Text==UiText.Get(language,"close")).Right);
                    Assert.True(compact.Right<form.Controls.OfType<PremiumButton>().Single(b=>b.Text==StartupText.Get(language,"retry")).Left);
                    typeof(Control).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(minimize,[EventArgs.Empty]);
                    Assert.Equal(FormWindowState.Minimized,form.WindowState);
                }
                if(output is not null)
                {
                    foreach(var language in new[]{"en","tr"})
                    {using var full=new StartupForm(stateFolder:folder);full.ExportStep(Path.Combine(output,"waiting-full-"+language+".png"),StartupStage.Synchronizing,language);}
                    using var tracker=new OverlayForm(false,folder,preview:true,selectedLanguage:"en");
                    foreach(var id in tracker.Controller.Tracker.VisibleIds.ToArray())tracker.Controller.Tracker.Dismiss(id);
                    typeof(OverlayForm).GetMethod("SyncCards",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(tracker,null);
                    tracker.ExportPreview(Path.Combine(output,"tracker-version.png"));
                }
                var version=typeof(OverlayForm).Assembly.GetName().Version!;
                Assert.Equal($"v{version.Major}.{version.Minor}.{version.Build}",OverlayForm.RunningVersion);
            }
            finally{Directory.Delete(folder,true);}
        });
    }

    private static void OnSta(Action action)
    {
        Exception? error=null;var thread=new Thread(()=>{try{action();}catch(Exception ex){error=ex;}});
        thread.SetApartmentState(ApartmentState.STA);thread.Start();Assert.True(thread.Join(TimeSpan.FromSeconds(60)));
        if(error is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
