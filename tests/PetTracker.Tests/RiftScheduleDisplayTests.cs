using PetTracker.Core;
namespace PetTracker.Tests;
public sealed class RiftScheduleDisplayTests
{
    [Theory]
    [InlineData("1101")][InlineData("1201")][InlineData("1301")][InlineData("1401")][InlineData("1501")]
    public void ScheduleListsAllEightOpeningsInServerTime(string server)
    {
        var region=RiftClock.Region(server)!;
        Assert.Equal("02:00 - 05:00 - 08:00 - 11:00 - 14:00 - 17:00 - 20:00 - 23:00",RiftClock.DailyTimes(DateTimeOffset.Parse("2026-10-15T00:00:00Z"),region));
    }
    [Fact] public void DaylightSavingDoesNotChangeDisplayedWallClockSchedule()
    {
        var region=RiftClock.Region("1301")!;
        Assert.Equal(RiftClock.DailyTimes(DateTimeOffset.Parse("2026-10-24T12:00:00Z"),region),RiftClock.DailyTimes(DateTimeOffset.Parse("2026-10-26T12:00:00Z"),region));
    }
}
