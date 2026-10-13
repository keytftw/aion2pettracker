using PetTracker.Core;
using Velopack;
using Velopack.Sources;

namespace PetTracker.App;
public sealed class GitHubUpdater:IApplicationUpdater
{
    public const string Repository="https://github.com/keytftw/aion2pettracker";
    private UpdateManager? manager;
    private UpdateInfo? available;
    public async Task<string?> CheckAsync(CancellationToken cancellation)
    {
        manager=new UpdateManager(new GithubSource(Repository,null,false,new TimedDownloader()));
        if(!manager.IsInstalled||manager.IsPortable)throw new NotSupportedException("Updates require the installed Setup distribution.");
        available=await manager.CheckForUpdatesAsync().WaitAsync(TimeSpan.FromSeconds(25),cancellation);
        return available?.TargetFullRelease.Version.ToString();
    }
    public async Task DownloadAsync(IProgress<int> progress,CancellationToken cancellation)
    {
        if(manager is null||available is null)throw new InvalidOperationException("No validated update selected.");
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);timeout.CancelAfter(TimeSpan.FromMinutes(15));
        await manager.DownloadUpdatesAsync(available,value=>progress.Report(value),cancelToken:timeout.Token);
    }
    public void ApplyAndRestart()
    {
        if(manager is null||available is null)throw new InvalidOperationException("No update selected.");
        manager.ApplyUpdatesAndRestart(available);
    }
    private sealed class TimedDownloader:HttpClientFileDownloader
    {
        public override Task<string> DownloadString(string url,IDictionary<string,string>? headers,double timeout)=>base.DownloadString(url,headers,Math.Min(timeout,.25));
        public override Task<byte[]> DownloadBytes(string url,IDictionary<string,string>? headers,double timeout)=>base.DownloadBytes(url,headers,Math.Min(timeout,.25));
    }
}
