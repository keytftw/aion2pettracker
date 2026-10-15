namespace PetTracker.Core;
public readonly record struct RiftBannerFrame(bool Visible,bool Opened,int Minutes,double Age,float Reveal,float Pulse);
public sealed class RiftBannerTimeline
{
    public const double SlideSeconds=.28,HoldSeconds=5,TotalSeconds=HoldSeconds+2*SlideSeconds;
    private readonly Queue<(bool Opened,int Minutes)> pending=[];
    private (bool Opened,int Minutes)? current;
    private double started;
    public void Enqueue(bool opened,int minutes,double now)
    {if(current is null){current=(opened,minutes);started=now;return;}if(pending.Count==3)pending.Dequeue();pending.Enqueue((opened,minutes));}
    public RiftBannerFrame Tick(double now)
    {
        if(current is null)return default;
        var age=Math.Max(0,now-started);
        if(age>=TotalSeconds){if(pending.Count==0){current=null;return default;}current=pending.Dequeue();started=now;age=0;}
        var fraction=Math.Clamp(Math.Min(age/SlideSeconds,(TotalSeconds-age)/SlideSeconds),0,1);
        return new(true,current.Value.Opened,current.Value.Minutes,age,(float)(1-Math.Pow(1-fraction,3)),(float)(.5+.5*Math.Sin(age*4.4)));
    }
}
