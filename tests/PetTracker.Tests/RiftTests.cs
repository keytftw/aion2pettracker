using PetTracker.Core;
namespace PetTracker.Tests;
public sealed class RiftTests
{
    private static DateTimeOffset U(string time)=>DateTimeOffset.Parse(time);
    [Theory]
    [InlineData("2112","nae")][InlineData("2212","naw")][InlineData("2312","eu")][InlineData("2412","la")][InlineData("2512","as")]
    [InlineData("1112","nae")][InlineData("1312","eu")]
    public void SupportedServerIdentitySelectsRegion(string server,string expected)=>Assert.Equal(expected,RiftClock.Region(server)!.Key);
    [Theory][InlineData("1001")][InlineData("2001")][InlineData("9999")][InlineData("abc")]
    public void UnknownServerCannotProduceAnAlert(string server)=>Assert.Null(RiftClock.Poll(U("2026-10-14T02:00:00Z"),RiftClock.Region(server),true,5,new HashSet<string>()));
    [Fact] public void WarningAndOpeningAreDistinctAndDeliveredOnlyOnce()
    {
        var region=RiftClock.Region("2312")!;var delivered=new HashSet<string>();
        Assert.Null(RiftClock.Poll(U("2026-10-14T02:54:59Z"),region,true,5,delivered));
        Assert.False(RiftClock.Poll(U("2026-10-14T02:55:00Z"),region,true,5,delivered)!.Opened);
        Assert.Null(RiftClock.Poll(U("2026-10-14T02:55:01Z"),region,true,5,delivered));
        Assert.True(RiftClock.Poll(U("2026-10-14T03:00:01Z"),region,true,5,delivered)!.Opened);
        Assert.Null(RiftClock.Poll(U("2026-10-14T03:00:02Z"),region,true,5,delivered));
        Assert.Equal(2,delivered.Count);
    }
    [Fact] public void DisabledAlertsAndStaleOpeningsAreSilent()
    {
        var region=RiftClock.Region("2312")!;var delivered=new HashSet<string>();
        Assert.Null(RiftClock.Poll(U("2026-10-14T02:55:00Z"),region,false,5,delivered));
        Assert.Null(RiftClock.Poll(U("2026-10-14T03:00:11Z"),region,true,5,delivered));Assert.Empty(delivered);
    }
    [Fact] public void LeadTimeChangesWithoutChangingOpeningAndRestoredLedgerSuppressesDuplicates()
    {
        var region=RiftClock.Region("2312")!;var ledger=new HashSet<string>();var now=U("2026-10-14T02:45:00Z");
        Assert.Null(RiftClock.Poll(now,region,true,5,ledger));var notice=RiftClock.Poll(now,region,true,15,ledger)!;
        Assert.Equal(15,notice.Minutes);Assert.Null(RiftClock.Poll(now,region,true,15,new HashSet<string>(ledger)));
    }
    [Fact] public void DstMissingHourIsSkippedAndRepeatedHourHasDistinctInstants()
    {
        var zone=RiftClock.Region("2312")!.Zone;
        Assert.DoesNotContain(RiftClock.Openings(U("2026-03-29T00:00:00Z"),zone),t=>TimeZoneInfo.ConvertTime(t,zone).Date==new DateTime(2026,3,29)&&TimeZoneInfo.ConvertTime(t,zone).Hour==2);
        var repeated=RiftClock.Openings(U("2026-10-25T00:00:00Z"),zone).Where(t=>TimeZoneInfo.ConvertTime(t,zone).Date==new DateTime(2026,10,25)&&TimeZoneInfo.ConvertTime(t,zone).Hour==2).ToArray();
        Assert.Equal(2,repeated.Length);Assert.Equal(TimeSpan.FromHours(1),repeated[1]-repeated[0]);
    }
}
