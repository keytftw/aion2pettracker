using PetTracker.Core;
namespace PetTracker.Tests;

public sealed class OpacityRangeTests
{
    [Theory]
    [InlineData(0,85)][InlineData(30,85)][InlineData(84,85)][InlineData(85,85)]
    [InlineData(92,92)][InlineData(100,100)][InlineData(150,100)]
    public void SavedOpacityMigratesIntoReadableRange(int oldValue,int expected)
    {
        var path=Path.GetTempFileName();
        try
        {
            File.WriteAllText(path,$"{{\"LayoutVersion\":4,\"OpacityPercent\":{oldValue}}}");
            var preferences=OverlayPreferences.Load(path,_=>{});Assert.Equal(expected,preferences.OpacityPercent);
            preferences.Save(path);Assert.Equal(expected,OverlayPreferences.Load(path,_=>{}).OpacityPercent);
        }
        finally{File.Delete(path);}
    }
}
