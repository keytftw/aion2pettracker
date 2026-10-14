using PetTracker.Core;
using Xunit;

namespace PetTracker.Tests;

public sealed class RiftDefaultTests
{
    [Fact]
    public void NewPreferencesEnableRiftButExplicitOptOutSurvivesSaveAndReload()
    {
        var folder=Path.Combine(Path.GetTempPath(),"A2RiftDefaults-"+Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            var path=Path.Combine(folder,"preferences.json");
            Assert.True(OverlayPreferences.Load(path,_=>{}).RiftAlertsEnabled);
            File.WriteAllText(path,"{\"LayoutVersion\":4}");
            Assert.True(OverlayPreferences.Load(path,_=>{}).RiftAlertsEnabled);
            var preferences=new OverlayPreferences{RiftAlertsEnabled=false};preferences.Save(path);
            Assert.False(OverlayPreferences.Load(path,_=>{}).RiftAlertsEnabled);
        }
        finally{Directory.Delete(folder,true);}
    }
}
