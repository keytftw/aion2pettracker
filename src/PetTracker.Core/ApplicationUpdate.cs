namespace PetTracker.Core;

public enum UpdatePhase { Checking, Downloading, Applying, NoUpdate, Failed }
public sealed record UpdateProgress(UpdatePhase Phase,int Percent=0,string? Version=null);
public interface IApplicationUpdater
{
    Task<string?> CheckAsync(CancellationToken cancellation);
    Task DownloadAsync(IProgress<int> progress,CancellationToken cancellation);
    void ApplyAndRestart();
}
public static class ApplicationUpdate
{
    public static async Task<bool> RunAsync(IApplicationUpdater updater,IProgress<UpdateProgress> progress,CancellationToken cancellation)
    {
        progress.Report(new(UpdatePhase.Checking));
        var version=await updater.CheckAsync(cancellation);cancellation.ThrowIfCancellationRequested();
        if(version is null){progress.Report(new(UpdatePhase.NoUpdate,100));return false;}
        progress.Report(new(UpdatePhase.Downloading,0,version));
        await updater.DownloadAsync(new InlineProgress<int>(value=>progress.Report(new(UpdatePhase.Downloading,Math.Clamp(value,0,100),version))),cancellation);
        cancellation.ThrowIfCancellationRequested();progress.Report(new(UpdatePhase.Applying,100,version));await Task.Delay(250,cancellation);updater.ApplyAndRestart();return true;
    }
}
public sealed class InlineProgress<T>(Action<T> action):IProgress<T>{public void Report(T value)=>action(value);}
