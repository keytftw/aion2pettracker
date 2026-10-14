using System.Text.Json;
using PetTracker.App;
using PetTracker.Core;
namespace PetTracker.Tests;

public class MapTests
{
    private const string Synthetic="""
    {"fetched":"2026-10-10","worlds":{
      "1010":{"name":"A","key":"World_L_A","sectors":[-100,-100,2,2,100]},
      "1110":{"name":"B","key":"World_D_A","sectors":[-100,-100,2,2,100]}},
    "pets":{
      "1114":[{"npc":10,"name":"Same creature name","level":12,"worlds":[{"world":1010,"area":"First","points":[[-100,100],[100,-100]]},{"world":1110,"area":"Second","points":[[0,0]]}]}],
      "1124":[{"npc":11,"name":"Same creature name","level":20,"worlds":[{"world":1010,"area":"Third","points":[[50,50]]}]}],
      "1001":[]}}
    """;
    [Fact] public void SourceProjectionUsesSectorBoundsAndCorrectYDirection()
    {
        var data=MapData.Parse(Synthetic);var points=data.Locations(1114,1010);
        Assert.Equal(new MapPoint(0,0),points[0].Point);Assert.Equal(new MapPoint(1,1),points[1].Point);
        Assert.Equal(new MapPoint(.5,.5),data.Locations(1114,1110)[0].Point);
    }
    [Fact] public void RegionFilterIncludesOnlyLocalPetsAndOptionalUnlocatedPets()
    {
        var data=MapData.Parse(Synthetic);
        var catalog=new PetCatalog([new(1114,"A","Fera"),new(1124,"B","Fera"),new(1001,"Unknown","Fera")]);
        Assert.Equal(new[]{1114,1001},MapPetQuery.Filter(catalog,data,1110,"",false,false,_=>0).Select(p=>p.Id));
        Assert.Equal(1114,Assert.Single(MapPetQuery.Filter(catalog,data,1110,"",false,true,_=>0)).Id);
        Assert.Empty(MapPetQuery.Filter(catalog,data,1110,"B",false,false,_=>0));
    }
    [Fact] public void AllFarmableMarkersExcludeMaxedPetsAndOtherRegions()
    {
        var data=MapData.Parse(Synthetic);
        var catalog=new PetCatalog([new(1114,"A","Fera"),new(1124,"B","Fera"),new(1001,"Unknown","Fera")]);
        var markers=MapPetQuery.FarmableLocations(catalog,data,1010,id=>id==1114?105:0);
        Assert.Equal(1124,Assert.Single(markers).PetId);
        Assert.Empty(MapPetQuery.FarmableLocations(catalog,data,1110,id=>id==1114?105:0));
        Assert.Equal(3,MapPetQuery.FarmableLocations(catalog,data,1010,_=>null).Length);
    }
    [Fact] public void SelectionUsesNumericPetIdAndPreservesPreferredRegion()
    {
        var data=MapData.Parse(Synthetic);Assert.Equal(1110,data.BestWorld(1114,1110));Assert.Equal(1010,data.BestWorld(1114,999));
        Assert.Single(data.Locations(1124,1010));Assert.Equal(11,data.Locations(1124,1010)[0].NpcId);
        Assert.Equal(new MapPoint(.75,.25),data.Locations(1124,1010)[0].Point);
        Assert.Empty(data.Locations(1124,1110));
    }
    [Theory] [InlineData(9999)] [InlineData(1001)]
    public void MissingCoordinatesNeverFallBackToAnotherPet(int id)
    {var data=MapData.Parse(Synthetic);Assert.Null(data.BestWorld(id,1010));Assert.Empty(data.WorldsForPet(id));Assert.Empty(data.Locations(id,1010));}
    [Fact] public void OffMapPointsAreNotInventedOrClampedOntoMapEdges()
    {
        var data=MapData.Parse(Synthetic.Replace("[[50,50]]","[[50,50],[300,300]]"));Assert.Single(data.Locations(1124,1010));Assert.Equal(1,data.IgnoredPoints);
    }
    [Fact] public void UnknownMapDoesNotBorrowKnownMapCoordinates()
    {var data=MapData.Parse(Synthetic.Replace("\"world\":1110","\"world\":999"));Assert.Empty(data.Locations(1114,1110));Assert.Equal(1,data.IgnoredPoints);}
    [Fact] public void ImagePathTraversalAndInvalidMapBoundsAreRejected()
    {
        Assert.Throws<InvalidDataException>(()=>MapData.Parse(Synthetic.Replace("World_L_A","../../reference")));
        Assert.Throws<InvalidDataException>(()=>MapData.Parse(Synthetic.Replace("2,2,100","0,2,100")));
        Assert.Throws<InvalidDataException>(()=>MapData.Parse(Synthetic.Replace("2,2,100","2,2,0")));
        Assert.Throws<InvalidDataException>(()=>MapData.Parse(Synthetic.Replace("\"key\":\"World_L_A\"","\"key\":null")));
        Assert.Throws<InvalidDataException>(()=>MapData.Parse(Synthetic.Replace("2026-10-10","invalid")));
    }
    [Fact] public void BundledSnapshotMatchesCatalogAndHasSixMapsAnd160MappedPets()
    {
        var data=MapData.Load(Path.Combine(AppContext.BaseDirectory,"data","maps","spawns.json"));
        var catalog=PetCatalog.Load(Path.Combine(AppContext.BaseDirectory,"data","pet-catalog.json"));
        Assert.Equal(6,data.Worlds.Count);Assert.Equal(new DateOnly(2026,10,10),data.SnapshotDate);
        Assert.Equal(160,catalog.Pets.Keys.Count(id=>data.WorldsForPet(id).Count>0));
        var points=catalog.Pets.Keys.SelectMany(id=>data.Worlds.Keys.SelectMany(world=>data.Locations(id,world))).ToArray();
        Assert.Equal(14634,points.Length+data.IgnoredPoints);
        Assert.All(points,point=>{Assert.Contains(point.PetId,catalog.Pets.Keys);Assert.InRange(point.Point.X,0,1);Assert.InRange(point.Point.Y,0,1);});
    }
    [Fact] public void MapSelectionAndFiltersDoNotChangeProgressPinsOrSession()
    {
        var catalog=new PetCatalog([new(1114,"Same","Fera"),new(1124,"Same","Fera")]);var tracker=new Tracker(catalog,new SavedState(),_=>{});var scope=new CharacterScope("Keyt","2312");
        tracker.Select(scope);tracker.Synchronize(new(scope,"one",1,[new(1114,2,74),new(1124,3,0)]));tracker.Pin(1114,true);
        var before=JsonSerializer.Serialize(tracker.Saved);var data=MapData.Parse(Synthetic);
        Assert.Equal(1114,Assert.Single(PetPickerQuery.Filter(catalog,"",false,tracker.KnownTotal)).Id);
        Assert.Equal(1124,Assert.Single(PetPickerQuery.Filter(catalog,"",true,tracker.KnownTotal)).Id);
        data.BestWorld(1114,1010);var camera=Camera();camera.Focus(data.Locations(1114,1010).Select(point=>point.Point),0);camera.Tick(1);
        Assert.Equal(before,JsonSerializer.Serialize(tracker.Saved));Assert.Empty(tracker.Session.Events);Assert.Empty(tracker.Session.Active);
        tracker.Gain(new(scope,"one",2,1114,1));Assert.Contains(1114,PetPickerQuery.Filter(catalog,"",true,tracker.KnownTotal).Select(pet=>pet.Id));
    }
    [Fact] public void MapTextsAreCompleteInAllTenLanguages()
    {Assert.True(MapText.Complete);foreach(var (code,_) in UiText.Languages){Assert.NotEmpty(MapText.Get(code,"farmable"));Assert.NotEmpty(MapText.Get(code,"noLocations"));Assert.DoesNotContain("{0}",MapText.Get(code,"locations",17));}}
    [Fact] public void MapWindowPreferencesAreStoredSeparatelyFromCounters()
    {
        var path=Path.GetTempFileName();try{new OverlayPreferences{Language="de",MapWorldId=1110,MapWidth=1300,MapHeight=900,MapLeft=240,MapTop=190}.Save(path);var loaded=OverlayPreferences.Load(path,_=>{});Assert.Equal(1110,loaded.MapWorldId);Assert.Equal(1300,loaded.MapWidth);Assert.Equal(900,loaded.MapHeight);Assert.Equal(240,loaded.MapLeft);Assert.Equal("de",loaded.Language);}finally{File.Delete(path);}
    }
    private static MapCamera Camera(){var camera=new MapCamera();camera.Resize(800,600,3072,3072);return camera;}
    [Fact] public void CameraProjectionRoundTripsWithZoomAndPan()
    {
        var camera=Camera();camera.SetTarget(new(.3,.7,4),0,0);var point=new MapPoint(.2,.9);var recovered=camera.FromScreen(camera.ToScreen(point));
        Assert.Equal(point.X,recovered.X,10);Assert.Equal(point.Y,recovered.Y,10);
    }
    [Fact] public void MouseZoomKeepsThePointUnderTheCursor()
    {
        var camera=Camera();camera.SetTarget(new(.5,.5,4),0,0);var cursor=new MapPoint(515,237);var before=camera.FromScreen(cursor);
        camera.ZoomAt(cursor,8,0);Assert.True(camera.IsAnimating);camera.Tick(.3);var after=camera.FromScreen(cursor);
        Assert.Equal(before.X,after.X,10);Assert.Equal(before.Y,after.Y,10);Assert.Equal(8,camera.Pose.Zoom);
    }
    [Fact] public void FocusAnimationSettlesOnLocationsAndRetargetsWithoutJumping()
    {
        var camera=Camera();Assert.True(camera.Focus([new(.7,.7),new(.8,.8)],0));camera.Tick(.3);var midway=camera.Pose;
        Assert.True(midway.X>.5&&midway.X<.75);Assert.True(midway.Zoom>1);
        camera.Focus([new(.25,.25)],.3);Assert.Equal(midway,camera.Pose);camera.Tick(1.1);
        Assert.Equal(.25,camera.Pose.X);Assert.Equal(.25,camera.Pose.Y);Assert.False(camera.IsAnimating);
    }
    [Fact] public void EmptyFocusCannotMoveToAGuessedLocation()
    {var camera=Camera();camera.SetTarget(new(.7,.3,5),0,0);Assert.False(camera.Focus([],1));Assert.Equal(new CameraPose(.7,.3,5),camera.Pose);}
    [Fact] public void ZoomAndPanAreBoundedAndRejectNonFiniteValues()
    {
        var camera=Camera();camera.SetTarget(new(-100,100,1000),0,0);Assert.Equal(new CameraPose(0,1,24),camera.Pose);
        camera.ZoomAt(new(400,300),.01,0);camera.Tick(.3);Assert.Equal(1,camera.Pose.Zoom);
        Assert.Throws<ArgumentOutOfRangeException>(()=>camera.SetTarget(new(double.NaN,0,2),0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>camera.Resize(0,10,3072,3072));
    }
}
