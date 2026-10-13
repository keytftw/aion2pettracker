using PetTracker.Core;
namespace PetTracker.Tests;
public class ApplicationUpdateTests
{
    private sealed class Fake(string? version):IApplicationUpdater
    {
        public int Downloads,Applies;public bool FailDownload;
        public Task<string?> CheckAsync(CancellationToken token){token.ThrowIfCancellationRequested();return Task.FromResult(version);}
        public Task DownloadAsync(IProgress<int> progress,CancellationToken token){Downloads++;token.ThrowIfCancellationRequested();progress.Report(35);if(FailDownload)throw new IOException("Interrupted");progress.Report(100);return Task.CompletedTask;}
        public void ApplyAndRestart()=>Applies++;
    }
    [Fact] public async Task NoUpdateContinuesWithoutDownloadOrRestart()
    {var fake=new Fake(null);var events=new List<UpdateProgress>();Assert.False(await ApplicationUpdate.RunAsync(fake,new InlineProgress<UpdateProgress>(events.Add),default));Assert.Equal(0,fake.Downloads);Assert.Equal(0,fake.Applies);Assert.Equal([UpdatePhase.Checking,UpdatePhase.NoUpdate],events.Select(e=>e.Phase));}
    [Fact] public async Task UpdateDownloadsThenRequestsRestartAfterCompletion()
    {var fake=new Fake("0.7.1");var events=new List<UpdateProgress>();Assert.True(await ApplicationUpdate.RunAsync(fake,new InlineProgress<UpdateProgress>(events.Add),default));Assert.Equal(1,fake.Downloads);Assert.Equal(1,fake.Applies);Assert.Equal(UpdatePhase.Applying,events.Last().Phase);Assert.Contains(events,e=>e.Percent==35);}
    [Fact] public async Task FailedDownloadNeverAppliesAndDoesNotPretendNoUpdate()
    {var fake=new Fake("0.7.1"){FailDownload=true};var events=new List<UpdateProgress>();await Assert.ThrowsAsync<IOException>(()=>ApplicationUpdate.RunAsync(fake,new InlineProgress<UpdateProgress>(events.Add),default));Assert.Equal(0,fake.Applies);Assert.DoesNotContain(events,e=>e.Phase==UpdatePhase.NoUpdate);}
    [Fact] public async Task CancellationPreventsDownloadAndRestart()
    {var fake=new Fake("0.7.1");using var token=new CancellationTokenSource();token.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>ApplicationUpdate.RunAsync(fake,new InlineProgress<UpdateProgress>(_=>{}),token.Token));Assert.Equal(0,fake.Downloads);Assert.Equal(0,fake.Applies);}
}
