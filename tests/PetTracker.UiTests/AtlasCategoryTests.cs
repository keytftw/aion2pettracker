using System.Reflection;
using PetTracker.App;
using PetTracker.Core;
namespace PetTracker.UiTests;
public sealed class AtlasCategoryTests
{
    [Fact] public void GenusAndTrackedFiltersComposeAndCardAddsWithoutChangingProgress()
    {
        Exception? failure=null;var folder=Path.Combine(Path.GetTempPath(),"A2AtlasCategories-"+Guid.NewGuid());Directory.CreateDirectory(folder);
        var thread=new Thread(()=>
        {
            try
            {
                T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(obj)!;
                void Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(obj,args);
                void Click(Control control)=>typeof(Control).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(control,[EventArgs.Empty]);
                using var tracker=new OverlayForm(false,folder,preview:true,selectedLanguage:"en");
                var state=tracker.Controller.Tracker;foreach(var id in state.VisibleIds.ToArray())state.Dismiss(id);
                var prefs=new OverlayPreferences{Language="en",MapAllPetsCategory=true};
                using var map=new PetMapForm(tracker.Controller,Field<IReadOnlyDictionary<int,Image>>(tracker,"portraits"),prefs,()=>{});
                map.StartPosition=FormStartPosition.Manual;map.Location=new(-5000,-5000);map.Show();Application.DoEvents();
                var trackedOption=Field<CheckBox>(map,"trackerOnly");Assert.False(trackedOption.Enabled);
                Call(map,"SelectGenus","Cogni");Assert.NotEmpty(Field<PetDefinition[]>(map,"matches"));Assert.All(Field<PetDefinition[]>(map,"matches"),p=>Assert.Equal("Cogni",p.Genus));
                var target=Field<PetDefinition[]>(map,"matches").First(p=>Field<MapData>(map,"data").WorldsForPet(p.Id).Count>0);
                var total=state.KnownTotal(target.Id);var quality=state.Quality;var events=state.Session.Events.Count;var added=0;map.PetAdded+=_=>added++;
                map.FocusPet(target.Id);Call(map,"ShowPetCard",target.Id);var card=Field<PetDetailCard>(map,"detailCard");var add=Field<PremiumButton>(card,"add");
                Assert.False(add.Bounds.IntersectsWith(Field<PremiumButton>(card,"close").Bounds));Click(add);Click(add);
                Assert.Contains(target.Id,state.VisibleIds);Assert.Contains(target.Id,state.Current.Preferences.Pinned);Assert.Equal(1,state.VisibleIds.Count(id=>id==target.Id));
                Assert.Equal(total,state.KnownTotal(target.Id));Assert.Equal(quality,state.Quality);Assert.Equal(events,state.Session.Events.Count);Assert.Equal(2,added);
                Call(map,"RefreshProgress");Assert.True(trackedOption.Enabled);trackedOption.Checked=true;
                Assert.True(((AtlasCheckBox)trackedOption).WarningAccent);Assert.Contains(MapText.Get("en","trackerOnlyNotice"),Field<Label>(map,"selectionLabel").Text);
                Assert.Equal(target.Id,Assert.Single(Field<PetDefinition[]>(map,"matches")).Id);
                var markers=Field<MapLocation[]>(Field<PetMapCanvas>(map,"canvas"),"markers");Assert.NotEmpty(markers);Assert.All(markers,p=>Assert.Equal(target.Id,p.PetId));
                foreach(var (testLanguage,_) in UiText.Languages)
                foreach(var width in new[]{760,1100})
                {
                    prefs.Language=testLanguage;map.SetLanguage(testLanguage);map.ClientSize=new(width,800);Application.DoEvents();
                    var options=new[]{Field<CheckBox>(map,"showAll"),Field<CheckBox>(map,"portraitMarkers"),Field<CheckBox>(map,"trackerOnly")};
                    Assert.All(options,c=>Assert.True(map.ClientRectangle.Contains(c.Bounds)));
                    Assert.False(options[0].Bounds.IntersectsWith(options[1].Bounds));Assert.False(options[1].Bounds.IntersectsWith(options[2].Bounds));
                    Assert.False(Field<PremiumButton>(map,"regions").Bounds.IntersectsWith(Field<PremiumButton>(map,"genera").Bounds));
                }
                var output=Environment.GetEnvironmentVariable("PETTRACKER_REVIEW_IMAGES");
                if(output is not null)
                {
                    Directory.CreateDirectory(output);
                    foreach(var language in new[]{"en","tr"})
                    {
                        prefs.Language=language;map.SetLanguage(language);Call(map,"ShowPetCard",target.Id);Application.DoEvents();
                        using var image=new Bitmap(map.Width,map.Height);map.DrawToBitmap(image,map.ClientRectangle);
                        var detail=Field<PetDetailCard>(map,"detailCard");using var detailImage=new Bitmap(detail.Width,detail.Height);detail.DrawToBitmap(detailImage,detail.ClientRectangle);using(var composite=Graphics.FromImage(image))composite.DrawImageUnscaled(detailImage,detail.Location);
                        image.Save(Path.Combine(output,"atlas-category-tracked-"+language+".png"));
                    }
                }
                state.Dismiss(target.Id);Call(map,"RefreshProgress");Assert.False(Field<CheckBox>(map,"trackerOnly").Enabled);Assert.False(prefs.MapTrackerOnly);
                Assert.False(tracker.Controller.Running);
            }
            catch(Exception ex){failure=ex;}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();Assert.True(thread.Join(TimeSpan.FromSeconds(45)));
        try{if(failure is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();}
        finally{Directory.Delete(folder,true);}
    }
}
