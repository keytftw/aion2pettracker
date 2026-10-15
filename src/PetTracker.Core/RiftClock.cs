namespace PetTracker.Core;

public sealed record RiftRegion(string Key,string Name,TimeZoneInfo Zone);
public sealed record RiftNotice(DateTimeOffset Opening,bool Opened,int Minutes,string Key);

public static class RiftClock
{
    // Server-region encoding corroborated by the supplied GPL-3.0 reference
    // Aion2Alerts.Logic/Timers.cs. Schedule: https://shugo.gg/timers (2026-10-14).
    public static RiftRegion? Region(string? server)
    {
        if(!int.TryParse(server,out var id)||id<1000||id>2999||id%100 is <1 or >50)return null;
        var info=(id/100%10) switch
        {
            1=>("nae","NA East","Eastern Standard Time"),
            2=>("naw","NA West","Pacific Standard Time"),
            3=>("eu","Europe","W. Europe Standard Time"),
            4=>("la","South America","E. South America Standard Time"),
            5=>("as","Asia","Korea Standard Time"),
            _=>("","","")
        };
        return info.Item1.Length==0?null:new(info.Item1,info.Item2,TimeZoneInfo.FindSystemTimeZoneById(info.Item3));
    }
    public static DateTimeOffset[] Openings(DateTimeOffset now,TimeZoneInfo zone)
    {
        var date=TimeZoneInfo.ConvertTime(now,zone).Date;var result=new List<DateTimeOffset>();
        for(var day=-1;day<=2;day++)for(var hour=2;hour<24;hour+=3)
        {
            var local=DateTime.SpecifyKind(date.AddDays(day).AddHours(hour),DateTimeKind.Unspecified);
            if(zone.IsInvalidTime(local))continue;
            var offsets=zone.IsAmbiguousTime(local)?zone.GetAmbiguousTimeOffsets(local):[zone.GetUtcOffset(local)];
            foreach(var offset in offsets)result.Add(new DateTimeOffset(local,offset).ToUniversalTime());
        }
        return result.Order().ToArray();
    }
    public static DateTimeOffset Next(DateTimeOffset now,RiftRegion region)=>Openings(now,region.Zone).First(t=>t>=now);
    public static string DailyTimes(DateTimeOffset now,RiftRegion region)
    {
        var date=TimeZoneInfo.ConvertTime(now,region.Zone).Date;
        return string.Join(" - ",Openings(now,region.Zone).Select(t=>TimeZoneInfo.ConvertTime(t,region.Zone)).Where(t=>t.Date==date).Select(t=>t.ToString("HH:mm")).Distinct());
    }
    public static RiftNotice? Poll(DateTimeOffset now,RiftRegion? region,bool enabled,int leadMinutes,ISet<string> delivered)
    {
        if(!enabled||region is null)return null;
        foreach(var opening in Openings(now,region.Zone))
        {
            var left=opening-now;var opened=left<=TimeSpan.Zero&&left>=TimeSpan.FromSeconds(-10);
            if(!opened&&(left<=TimeSpan.Zero||left>TimeSpan.FromMinutes(Math.Clamp(leadMinutes,1,120))))continue;
            var key=region.Key+":"+opening.ToUnixTimeSeconds()+":"+(opened?"open":"warning");
            if(!delivered.Add(key))continue;
            return new(opening,opened,Math.Max(1,(int)Math.Ceiling(left.TotalMinutes)),key);
        }
        return null;
    }
}
