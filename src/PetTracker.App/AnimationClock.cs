using System.Diagnostics;

namespace PetTracker.App;

internal sealed class AnimationClock
{
    private long previous=Stopwatch.GetTimestamp();
    public double Step()
    {
        var now=Stopwatch.GetTimestamp();var elapsed=(now-previous)/(double)Stopwatch.Frequency;previous=now;
        return elapsed>.25?1d/60:elapsed;
    }
}
