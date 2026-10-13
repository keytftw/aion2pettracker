using System.Reflection;
using PetTracker.App;
using PetTracker.Core;

namespace PetTracker.UiTests;

public sealed class WebsitePreviews
{
    [Fact]
    public void ExportCurrentOfflineInterface()
    {
        var requested=Environment.GetEnvironmentVariable("PETTRACKER_SITE_PREVIEWS");
        var folder=Path.Combine(Path.GetTempPath(),"A2TrackerPreview-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        Exception? failure=null;
        var thread=new Thread(()=>
        {
            try
            {
                new OverlayPreferences{Language="en",VisibleCards=5}.Save(Path.Combine(folder,"overlay-preferences.json"));
                using var tracker=new OverlayForm(false,folder,preview:true,selectedLanguage:"en");_=tracker.Handle;
                tracker.ExportPreview(Path.Combine(folder,"tracker.png"));
                var preferences=(OverlayPreferences)typeof(OverlayForm).GetField("preferences",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(tracker)!;
                var portraits=(IReadOnlyDictionary<int,Image>)typeof(OverlayForm).GetField("portraits",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(tracker)!;
                using var settings=new OverlaySettingsForm(preferences,Cursors.Default,7,()=>{},()=>{});_=settings.Handle;Save(settings,"settings.png");
                using var picker=new PetPickerForm(tracker.Controller.Catalog,portraits,"en",Cursors.Default,_=>{},tracker.Controller.Tracker.KnownTotal);_=picker.Handle;Save(picker,"pet-library.png");
                using var map=new PetMapForm(tracker.Controller,portraits,preferences,()=>{});_=map.Handle;map.FocusPet(1114);
                var canvas=(PetMapCanvas)typeof(PetMapForm).GetField("canvas",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(map)!;
                var camera=(MapCamera)typeof(PetMapCanvas).GetField("camera",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(canvas)!;camera.Tick(1000);
                Save(map,"atlas.png");
                typeof(OverlayForm).GetMethod("ToggleCompact",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(tracker,null);Save(tracker,"compact.png");
                Assert.False(tracker.Controller.Running);
                if(requested is not null){Directory.CreateDirectory(requested);foreach(var name in new[]{"tracker.png","settings.png","pet-library.png","atlas.png","compact.png"})File.Copy(Path.Combine(folder,name),Path.Combine(requested,name),true);}
                void Save(Form form,string name)
                {
                    // Child controls are rendered by DrawToBitmap only when visible.
                    // Show this offline test window beyond the desktop, never the game.
                    form.StartPosition=FormStartPosition.Manual;form.Location=new(-5000,-5000);form.Show();Application.DoEvents();
                    using var bitmap=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);form.DrawToBitmap(bitmap,form.ClientRectangle);bitmap.Save(Path.Combine(folder,name));form.Hide();
                }
            }
            catch(Exception ex){failure=ex;}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();Assert.True(thread.Join(TimeSpan.FromSeconds(30)));
        try{if(failure is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();}
        finally{Directory.Delete(folder,true);}
    }
}
