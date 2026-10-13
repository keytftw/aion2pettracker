using PetTracker.Core;
namespace PetTracker.Tests;

public sealed class FrameSmoothingTests
{
    [Fact]
    public void ScrollTimingMatchesAtThirtySixtyAndOneHundredTwentyFrames()
    {
        float At(int fps){float value=0;for(var frame=0;frame<fps/4;frame++)value=FrameSmoothing.Approach(value,71,1d/fps,18,0);return value;}
        // Use a duration representable at all three rates (one half-second).
        float HalfSecond(int fps){float value=0;for(var frame=0;frame<fps/2;frame++)value=FrameSmoothing.Approach(value,71,1d/fps,18,0);return value;}
        Assert.InRange(Math.Abs(HalfSecond(30)-HalfSecond(60)),0,.0001f);
        Assert.InRange(Math.Abs(HalfSecond(60)-HalfSecond(120)),0,.0001f);
        Assert.InRange(At(60),0,71);
    }
    [Fact]
    public void DelayedFrameApproachesTargetWithoutOvershootAndSettlesExactly()
    {
        Assert.InRange(FrameSmoothing.Approach(0,71,.12),0,71);
        Assert.Equal(71,FrameSmoothing.Approach(0,71,2));
        Assert.Equal(0,FrameSmoothing.Approach(0,71,0));
        Assert.InRange(FrameSmoothing.Approach(71,0,.12),0,71);
    }
}
