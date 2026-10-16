using System.Reflection;
using PetTracker.App;
using PetTracker.Core;
namespace PetTracker.UiTests;
public sealed class PetGuideUiTests
{
    [Fact] public void ExclusiveFilterBookAndCardsWorkWithoutCaptureOrCounterChanges()
    {
        var folder=Path.Combine(Path.GetTempPath(),"A2PetGuide-"+Guid.NewGuid());Directory.CreateDirectory(folder);Exception? failure=null;
        var thread=new Thread(()=>
        {
            try
            {
                T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(obj)!;
                void Click(Control c)=>typeof(Control).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(c,[EventArgs.Empty]);
                void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(o,args);
                using var tracker=new OverlayForm(false,folder,preview:true);
                var portraits=Field<IReadOnlyDictionary<int,Image>>(tracker,"portraits");
                var output=Environment.GetEnvironmentVariable("PETTRACKER_REVIEW_IMAGES");
                void Save(Form form,string name)
                {if(output is null)return;Directory.CreateDirectory(output);using var image=new Bitmap(form.Width,form.Height);form.DrawToBitmap(image,new(Point.Empty,form.Size));
                    if(form is PetGuideForm guide)
                    {
                        var detail=Field<PetDetailCard>(guide,"card");if(detail is not null){using var rendered=new Bitmap(detail.Width,detail.Height);detail.DrawToBitmap(rendered,detail.ClientRectangle);using var g=Graphics.FromImage(image);g.DrawImageUnscaled(rendered,form.PointToClient(detail.PointToScreen(Point.Empty))+new Size((form.Width-form.ClientSize.Width)/2,form.Height-form.ClientSize.Height-(form.Width-form.ClientSize.Width)/2));}
                    }
                    image.Save(Path.Combine(output,name+".png"));}
                foreach(var (language,_) in UiText.Languages)
                {
                    var prefs=new OverlayPreferences{Language=language,MapWorldId=1010,MapExclusiveOnly=true,MapShowAllFarmable=true};
                    using var atlas=new PetMapForm(tracker.Controller,portraits,prefs,()=>{});atlas.Location=new(-5000,-5000);atlas.Show();Application.DoEvents();
                    foreach(var width in new[]{760,1100,1500})
                    {
                        atlas.ClientSize=new(width,800);Application.DoEvents();
                        var options=new Control[]{Field<CheckBox>(atlas,"showAll"),Field<CheckBox>(atlas,"portraitMarkers"),Field<CheckBox>(atlas,"trackerOnly"),Field<CheckBox>(atlas,"exclusiveOnly"),Field<PremiumButton>(atlas,"discoveryButton")};
                        foreach(var a in options){Assert.True(atlas.ClientRectangle.Contains(a.Bounds));foreach(var b in options.Where(b=>b!=a))Assert.False(a.Bounds.IntersectsWith(b.Bounds));}
                        Assert.Equal(atlas.ClientSize.Width-30,options[^1].Right);
                    }
                    var data=Field<MapData>(atlas,"data");var sources=PetSources.Load(Path.Combine(AppContext.BaseDirectory,"data/pet-sources.json"));var matches=Field<PetDefinition[]>(atlas,"matches");Assert.NotEmpty(matches);
                    Assert.All(matches,p=>Assert.True(PetGuideQuery.IsExclusive(data,p.Id,1010,sources.Get(p.Id))));
                    Assert.All(Field<MapLocation[]>(Field<PetMapCanvas>(atlas,"canvas"),"markers"),m=>Assert.Contains(matches,p=>p.Id==m.PetId));
                    Click(Field<PremiumButton>(atlas,"discoveryButton"));Application.DoEvents();var book=Field<PetGuideForm>(atlas,"discovery");book.Location=new(-5000,-5000);
                    Assert.NotEmpty(Field<PetDefinition[]>(book,"matches"));
                    var grid=Field<DataGridView>(book,"list");var detailPanel=Field<Panel>(book,"details");Assert.True(grid.Width>300);Assert.True(detailPanel.Width>300);
                    Assert.False(new Rectangle(grid.PointToScreen(Point.Empty),grid.Size).IntersectsWith(new Rectangle(detailPanel.PointToScreen(Point.Empty),detailPanel.Size)));
                    var target=Field<PetDefinition[]>(book,"matches")[0];Call(book,"ShowCard",target.Id);var card=Field<PetDetailCard>(book,"card");
                    var total=tracker.Controller.Tracker.KnownTotal(target.Id);var quality=tracker.Controller.Tracker.Quality;Click(Field<PremiumButton>(card,"add"));
                    Assert.Contains(target.Id,tracker.Controller.Tracker.VisibleIds);Assert.Equal(total,tracker.Controller.Tracker.KnownTotal(target.Id));Assert.Equal(quality,tracker.Controller.Tracker.Quality);
                    atlas.ClientSize=new(1100,800);
                    if(language is "tr" or "en"){Save(atlas,"atlas-exclusive-"+language);Save(book,"guide-exclusive-"+language);}
                    Click(Field<PremiumButton>(book,"special"));Field<ComboBox>(book,"regions").SelectedIndex=0;Application.DoEvents();
                    Assert.Contains(Field<PetDefinition[]>(book,"matches"),p=>p.Id==1080);
                    var bossIndex=Array.FindIndex(Field<PetDefinition[]>(book,"matches"),p=>p.Id==1080);Field<DataGridView>(book,"list").CurrentCell=Field<DataGridView>(book,"list").Rows[bossIndex].Cells[1];Application.DoEvents();Assert.Equal(1080,Field<PetDefinition>(Field<PetDetailCard>(book,"card"),"pet").Id);if(language is "tr" or "en")Save(book,"guide-special-"+language);
                    book.Close();
                }
                Assert.False(tracker.Controller.Running);
            }
            catch(Exception ex){failure=ex;}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();Assert.True(thread.Join(TimeSpan.FromSeconds(60)));
        try{if(failure is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();}finally{Directory.Delete(folder,true);}
    }
}
