namespace PetTracker.Core;

/// <summary>Completed work, capped by a six-second presentation floor; never predicts remaining time.</summary>
public sealed class InstallationProgress
{
    public static readonly TimeSpan MinimumDuration=TimeSpan.FromSeconds(6);
    public double CompletedWork {get;private set;}
    public void Report(double completedWork)
    {
        if(!double.IsFinite(completedWork)||completedWork<0||completedWork>1)throw new ArgumentOutOfRangeException(nameof(completedWork));
        CompletedWork=Math.Max(CompletedWork,completedWork);
    }
    public double Displayed(TimeSpan elapsed)=>Math.Min(CompletedWork,Math.Clamp(elapsed.TotalSeconds/MinimumDuration.TotalSeconds,0,1));
    public bool CanFinish(TimeSpan elapsed)=>CompletedWork==1&&elapsed>=MinimumDuration;
}
