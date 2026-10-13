using PetTracker.Core;

namespace PetTracker.Tests;

public sealed class CardMotionTests
{
    [Fact]
    public void NewCardSlidesInAndFadesToFullyVisible()
    {
        var motion=new CardMotion(71);motion.Sync([1114],0);var row=Assert.Single(motion.Rows);
        Assert.Equal(28,row.X);Assert.Equal(0,row.Alpha);
        motion.Tick(.13);Assert.InRange(row.X,.01f,27.99f);Assert.InRange(row.Alpha,.01f,.99f);
        motion.Tick(.3);Assert.Equal(0,row.X);Assert.Equal(1,row.Alpha);
    }
    [Fact]
    public void DeletionSlidesOutAndRemainingRowsMoveUpSmoothly()
    {
        var motion=new CardMotion(71);motion.Sync([1002,1114,1124],0);motion.Tick(.3);
        motion.Sync([1114,1124],.4);Assert.Equal(3,motion.Rows.Count);
        var second=motion.Rows.Single(r=>r.Id==1114);Assert.Equal(71,second.Y);
        motion.Tick(.53);Assert.InRange(second.Y,.01f,70.99f);Assert.True(motion.Rows.Single(r=>r.Id==1002).Removing);
        motion.Tick(.7);Assert.Equal(2,motion.Rows.Count);Assert.Equal(0,second.Y);Assert.Equal(71,motion.Rows.Single(r=>r.Id==1124).Y);
    }
    [Fact]
    public void ReactivationDuringExitReusesOneCardAndMaxCardsAreNotAutomaticallyRemoved()
    {
        var motion=new CardMotion(71);motion.Sync([1114],0);motion.Tick(.3);motion.Sync([],.4);motion.Tick(.5);
        motion.Sync([1114],.51);motion.Tick(.9);var row=Assert.Single(motion.Rows);Assert.False(row.Removing);Assert.Equal(1,row.Alpha);
        motion.Sync([1114],1);motion.Tick(2);Assert.Single(motion.Rows);
    }
}
