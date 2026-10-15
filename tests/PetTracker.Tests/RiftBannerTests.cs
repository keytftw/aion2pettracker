using PetTracker.Core;
namespace PetTracker.Tests;
public sealed class RiftBannerTests
{
    [Fact] public void HoldsForFiveSecondsBetweenSmoothEntryAndExit()
    {
        var banner=new RiftBannerTimeline();banner.Enqueue(false,5,10);
        Assert.Equal(0,banner.Tick(10).Reveal);
        Assert.InRange(banner.Tick(10.14).Reveal,.8f,.95f);
        Assert.Equal(1,banner.Tick(10+RiftBannerTimeline.SlideSeconds).Reveal);
        Assert.Equal(1,banner.Tick(10+RiftBannerTimeline.SlideSeconds+5).Reveal);
        Assert.InRange(banner.Tick(10+RiftBannerTimeline.TotalSeconds-.14).Reveal,.8f,.95f);
        Assert.False(banner.Tick(10+RiftBannerTimeline.TotalSeconds+.001).Visible);
    }
    [Fact] public void OpeningDoesNotOverwriteReminderAndQueueIsBounded()
    {
        var banner=new RiftBannerTimeline();banner.Enqueue(false,5,0);banner.Enqueue(true,0,.1);
        Assert.False(banner.Tick(1).Opened);
        Assert.True(banner.Tick(6).Opened);Assert.Equal(0,banner.Tick(6).Age);
        Assert.False(banner.Tick(12).Visible);
        banner.Enqueue(false,5,13);for(var i=0;i<100;i++)banner.Enqueue(false,i,13);
        Assert.Equal(97,banner.Tick(19).Minutes);Assert.Equal(98,banner.Tick(25).Minutes);Assert.Equal(99,banner.Tick(31).Minutes);
        Assert.False(banner.Tick(37).Visible);
    }
}
