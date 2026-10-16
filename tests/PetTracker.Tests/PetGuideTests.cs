using PetTracker.Core;
namespace PetTracker.Tests;

public sealed class PetGuideTests
{
    private const string Snapshot="""
    {"fetched":"2026-10-10","worlds":{"1010":{"name":"Verteron","key":"A","sectors":[0,0,1,1,100]},"1110":{"name":"Altgard","key":"B","sectors":[0,0,1,1,100]}},"pets":{
    "1":[{"npc":10,"name":"Only A","level":1,"worlds":[{"world":1010,"area":null,"points":[[30,40],[35,40]]}]}],
    "2":[{"npc":20,"name":"Both","level":2,"worlds":[{"world":1010,"area":null,"points":[[30,40]]},{"world":1110,"area":null,"points":[[30,40]]}]}],
    "3":[]}}
    """;
    [Fact] public void ExclusiveFilterComposesAndAggregateMarkersCannotLeakSharedPets()
    {
        var data=MapData.Parse(Snapshot);var catalog=new PetCatalog([new(1,"Only","Cogni"),new(2,"Shared","Cogni"),new(3,"Unknown","Cogni")]);
        var matches=MapPetQuery.Filter(catalog,data,1010,"",false,false,_=>0,"Cogni",new HashSet<int>{1,2},exclusive:true);
        Assert.Equal(1,Assert.Single(matches).Id);Assert.All(MapPetQuery.TrackedLocations(data,1010,matches.Select(p=>p.Id)),p=>Assert.Equal(1,p.PetId));
        Assert.Empty(MapPetQuery.Filter(catalog,data,1110,"",false,false,_=>0,exclusive:true));
        Assert.Empty(MapPetQuery.Filter(catalog,data,1010,"",false,false,_=>0,"Natura",exclusive:true));
        Assert.Empty(MapPetQuery.Filter(catalog,data,1010,"Shared",false,false,_=>0,exclusive:true));
        Assert.Empty(MapPetQuery.Filter(catalog,data,1010,"",false,false,_=>105,exclusive:true));
        Assert.Equal(1,Assert.Single(MapPetQuery.Filter(catalog,data,1010,"",true,false,_=>105,exclusive:true)).Id);
        Assert.False(PetGuideQuery.IsExclusive(data,3,1010));Assert.False(PetGuideQuery.IsExclusive(data,1,-1));
    }
    [Fact] public void KnownSecondSourceMapPreventsFalseExclusivityAndIndexesAreReused()
    {
        var data=MapData.Parse(Snapshot);
        Assert.True(PetGuideQuery.IsExclusive(data,1,1010));
        Assert.False(PetGuideQuery.IsExclusive(data,1,1010,new(1,true,"https://wikily.gg/aion-2/pets/x","2026-10-16",["Other NPC · Monster drop, level 1, Altgard"])));
        Assert.True(PetGuideQuery.IsExclusive(data,1,1010,new(1,true,null,null,["Altgard Chest · Opened from this chest"])));
        Assert.Same(data.Locations(1,1010),data.Locations(1,1010));Assert.Same(data.WorldsForPet(1),data.WorldsForPet(1));
    }
    [Fact] public void RealGuideIncludesUnlocatedSourcesAndBossLabelsDoNotInventDrops()
    {
        var data=MapData.Load(Path.Combine(AppContext.BaseDirectory,"data/maps/spawns.json"));var catalog=PetCatalog.Load(Path.Combine(AppContext.BaseDirectory,"data/pet-catalog.json"));var sources=PetSources.Load(Path.Combine(AppContext.BaseDirectory,"data/pet-sources.json"));
        var all=PetGuideQuery.Filter(catalog,data,sources,-1,false,"",false,"","",_=>null);Assert.NotEmpty(all);
        Assert.All(all,p=>Assert.Single(data.WorldsForPet(p.Id)));
        var bosses=PetGuideQuery.Filter(catalog,data,sources,-1,false,"",true,"worldBoss","",_=>0);
        Assert.Contains(bosses,p=>p.Id==1080);Assert.DoesNotContain(bosses,p=>p.Id==1146);
        Assert.Equal("routeMerchant",PetGuideQuery.Route(data,catalog.Pets[1080],sources.Get(1080),1010).Key);
        Assert.Contains(PetGuideQuery.Filter(catalog,data,sources,-1,false,"",true,"dungeonBoss","",_=>0),p=>p.Id==1146);
        Assert.DoesNotContain(PetGuideQuery.Filter(catalog,data,sources,1110,false,"",true,"worldBoss","",_=>0),p=>p.Id==1080);
        Assert.Contains(PetGuideQuery.Filter(catalog,data,sources,-1,true,"",true,"","",_=>105),p=>p.Id==1080);
        Assert.Equal("routeUnknown",PetGuideQuery.Route(data,new(9999,"Unknown","Cogni"),null,1010).Key);
    }
}
