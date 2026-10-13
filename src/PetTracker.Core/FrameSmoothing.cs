namespace PetTracker.Core;

// Exponential easing based on elapsed seconds, independent of timer/frame frequency.
public static class FrameSmoothing
{
    public static float Approach(float value,float target,double seconds,float response=18,float tolerance=.001f)
    {
        if(seconds<=0)return value;
        var next=target+(value-target)*(float)Math.Exp(-response*seconds);
        return Math.Abs(next-target)<=tolerance?target:next;
    }
}
