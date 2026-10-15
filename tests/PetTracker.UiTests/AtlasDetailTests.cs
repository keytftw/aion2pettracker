using System.Reflection;
using PetTracker.App;
using PetTracker.Core;
namespace PetTracker.UiTests;
public sealed class AtlasDetailTests
{
    [Fact] public void UnlocatedCategoryCardAndRiftPreviewRenderWithoutStartingCapture()
    {
        Exception? failure=null;var folder=Path.Combine(Path.GetTempPath(),"A2AtlasDetail-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        var thread=new Thread(()=>
        {
            try
            {
                using var tracker=new OverlayForm(false,folder,preview:true);
                var prefs=new OverlayPreferences{Language="en",MapUnlocatedCategory=true,RiftSoundEnabled=false};
                var portraits=(IReadOnlyDictionary<int,Image>)typeof(OverlayForm).GetField("portraits",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(tracker)!;
                using var map=new PetMapForm(tracker.Controller,portraits,prefs,()=>{});
                T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(obj)!;
                void Click(Control control)=>typeof(Control).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(control,[EventArgs.Empty]);
                var data=Field<MapData>(map,"data");
                var handbookButton=Field<PremiumButton>(map,"handbookButton");Assert.True(handbookButton.BookIcon);
                Click(handbookButton);var handbook=Field<HandbookForm>(map,"handbook");Assert.True(handbook.Visible);
                Click(handbookButton);Assert.Same(handbook,Field<HandbookForm>(map,"handbook"));handbook.Close();
                Assert.All(Field<PetDefinition[]>(map,"matches"),p=>Assert.Empty(data.WorldsForPet(p.Id)));
                Assert.False(Field<CheckBox>(map,"showAll").Enabled);
                typeof(PetMapForm).GetMethod("ShowPetCard",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(map,[1001]);
                var card=Field<PetDetailCard>(map,"detailCard");Assert.NotNull(card);
                map.ClientSize=new(760,550);map.StartPosition=FormStartPosition.Manual;map.Location=new(-5000,-5000);map.Show();Application.DoEvents();
                Assert.True(map.ClientRectangle.Contains(card.Bounds));
                Assert.True(card.Top>Field<PetMapCarousel>(map,"carousel").Bottom);
                Assert.True(card.Bottom<=Field<PremiumButton>(map,"focus").Top);
                Assert.False(card.Bounds.IntersectsWith(Field<PremiumButton>(map,"next").Bounds));
                var nextButton=Field<PremiumButton>(map,"next");Assert.Same(nextButton,map.GetChildAtPoint(new(nextButton.Left+nextButton.Width/2,nextButton.Top+nextButton.Height/2)));
                Assert.Equal(0,map.Controls.GetChildIndex(card));
                var rows=Field<FlowLayoutPanel>(card,"sources");Assert.True(rows.Bottom<Field<PremiumButton>(card,"map").Top);
                map.ClientSize=new(1100,800);Application.DoEvents();
                using var image=new Bitmap(map.Width,map.Height);map.DrawToBitmap(image,map.ClientRectangle);
                var output=Environment.GetEnvironmentVariable("PETTRACKER_SITE_PREVIEWS");if(output is not null){Directory.CreateDirectory(output);using var cardImage=new Bitmap(card.Width,card.Height);card.DrawToBitmap(cardImage,card.ClientRectangle);using var composite=Graphics.FromImage(image);composite.DrawImageUnscaled(cardImage,card.Location);image.Save(Path.Combine(output,"pet-card.png"));}
                typeof(PetMapForm).GetMethod("HidePetCard",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(map,null);map.FocusPet(1114);
                Field<CheckBox>(map,"showAll").Checked=true;var canvas=Field<PetMapCanvas>(map,"canvas");
                var camera=Field<MapCamera>(canvas,"camera");camera.SetTarget(new(.5,.5,1),0,0);Application.DoEvents();
                using var aggregate=new Bitmap(map.Width,map.Height);map.DrawToBitmap(aggregate,map.ClientRectangle);
                Assert.NotEmpty(Field<List<MapLocation>>(canvas,"drawnMarkers"));
                Assert.NotEmpty(Field<Dictionary<(int Id,int Size),Bitmap>>(canvas,"portraitMarkers"));
                var portraitOption=Field<CheckBox>(map,"portraitMarkers");portraitOption.Checked=false;
                Assert.False(prefs.MapPortraitMarkers);Assert.False(Field<bool>(canvas,"showPortraits"));portraitOption.Checked=true;
                Assert.True(prefs.MapPortraitMarkers);Assert.True(Field<bool>(canvas,"showPortraits"));
                var multi=tracker.Controller.Catalog.Pets.Values.First(p=>data.WorldsForPet(p.Id).Count>1);
                map.FocusPet(multi.Id);typeof(PetMapForm).GetMethod("ShowPetCard",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(map,[multi.Id]);
                var multiCard=Field<PetDetailCard>(map,"detailCard");var mapButton=Field<PremiumButton>(multiCard,"map");
                Assert.Contains("▾",mapButton.Text);Click(mapButton);
                var regionMenu=Field<Panel>(map,"petWorldMenu");Assert.Equal(data.WorldsForPet(multi.Id).Count,regionMenu.Controls.Count);
                var otherWorld=data.WorldsForPet(multi.Id).Last();Click(regionMenu.Controls[^1]);
                Assert.Equal(otherWorld,Field<int>(map,"worldId"));
                Assert.Equal(multi.Id,Field<int>(map,"selectedId"));
                Assert.All(Field<MapLocation[]>(canvas,"markers"),m=>Assert.Equal(otherWorld,m.WorldId));
                if(output is not null)aggregate.Save(Path.Combine(output,"atlas-portraits.png"));
                using var rift=new RiftAlertService(tracker.Controller,prefs,()=>{},start:false);
                using var settings=new RiftSettingsForm(prefs,()=>rift.Region,()=>{},(_,_)=>{});_=settings.Handle;
                using var toast=new RiftToast(()=>new(true,false,5,1,1,.5f),"en");Assert.False(toast.ShowInTaskbar);Assert.True(toast.TopMost);
                if(output is not null){toast.Location=new(-5000,-5000);toast.Show();Application.DoEvents();using var notice=new Bitmap(toast.Width,toast.Height);toast.DrawToBitmap(notice,toast.ClientRectangle);notice.Save(Path.Combine(output,"rift-toast.png"));toast.Close();}
                Assert.False(tracker.Controller.Running);
            }
            catch(Exception ex){failure=ex;}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();Assert.True(thread.Join(TimeSpan.FromSeconds(30)));
        try{if(failure is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();}
        finally{Directory.Delete(folder,true);}
    }
}
