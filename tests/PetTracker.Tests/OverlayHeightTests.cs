using PetTracker.Core;
namespace PetTracker.Tests;

public class OverlayHeightTests
{
    [Fact] public void LegacyHeightLockDoesNotPreventAutomaticGrowth()
    {
        var path=Path.GetTempFileName();try
        {
            File.WriteAllText(path,"{\"LayoutVersion\":4,\"VisibleCards\":3,\"HeightLocked\":true,\"Locked\":true}");
            var settings=OverlayPreferences.Load(path,_=>{});
            settings.VisibleCards=OverlayHeight.Grow(settings.VisibleCards,4);settings.Save(path);
            Assert.Equal(4,settings.VisibleCards);Assert.True(settings.Locked);
            Assert.DoesNotContain("HeightLocked",File.ReadAllText(path));
        }finally{File.Delete(path);}
    }
    [Fact] public void DefaultsToThreeAndOnlyGrowsAsPetsAreAdded()
    {
        var settings=new OverlayPreferences();Assert.Equal(3,settings.VisibleCards);
        var cards=3;foreach(var count in new[]{4,5,6,7,8,12}){cards=OverlayHeight.Grow(cards,count);Assert.Equal(Math.Min(count,7),cards);}
        Assert.Equal(7,OverlayHeight.Grow(cards,2));
    }
    [Fact] public void AutomaticGrowthAlwaysReachesSevenRegardlessOfPositionLock()
    {
        for(var capacity=1;capacity<=7;capacity++)Assert.Equal(7,OverlayHeight.Grow(capacity,20));
        var settings=new OverlayPreferences{VisibleCards=3,Locked=false};settings.Validate();
        Assert.Equal(OverlayHeight.Pixels(3),settings.Height);Assert.False(settings.Locked);
    }
    [Theory][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)]
    public void CardCapacitiesRoundTripAndFitCompleteRows(int cards)
    {
        var pixels=OverlayHeight.Pixels(cards);Assert.Equal(cards,OverlayHeight.CardsFromPixels(pixels));
        // Last card ends at header + top padding + preceding rows + its own height.
        Assert.True(pixels*3d/4>=80+4+(cards-1)*71+65+42);
    }
    [Fact] public void PreferencesPersistCardCapacityAndMigrateLegacySettings()
    {
        var path=Path.GetTempFileName();try
        {
            new OverlayPreferences{VisibleCards=6}.Save(path);
            var saved=OverlayPreferences.Load(path,_=>{});Assert.Equal(6,saved.VisibleCards);
            saved.VisibleCards=100;saved.Validate();Assert.Equal(7,saved.VisibleCards);
            saved.VisibleCards=0;saved.Validate();Assert.Equal(1,saved.VisibleCards);
            File.WriteAllText(path,"{\"LayoutVersion\":3,\"Height\":1100,\"Language\":\"en\"}");
            var migrated=OverlayPreferences.Load(path,_=>{});Assert.Equal(3,migrated.VisibleCards);Assert.Equal("en",migrated.Language);
        }finally{File.Delete(path);}
    }
}
