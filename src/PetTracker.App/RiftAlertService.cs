namespace PetTracker.App;

public sealed class RiftAlertService : IDisposable
{
    private readonly OverlayController controller;
    private readonly OverlayPreferences preferences;
    private readonly Action save;
    private readonly System.Windows.Forms.Timer timer=new(){Interval=1000};
    private readonly HashSet<string> delivered;
    private readonly MaxChime chime=new();
    private RiftToast? toast;
    private bool disposed;
    public RiftRegion? Region=>controller.Tracker.HasCurrent&&controller.Tracker.Current.Snapshot is not null?RiftClock.Region(controller.Tracker.Current.Scope.Server):null;
    public RiftAlertService(OverlayController controller,OverlayPreferences preferences,Action save,bool start=true)
    {
        this.controller=controller;this.preferences=preferences;this.save=save;delivered=new(preferences.RiftDelivered);
        timer.Tick+=(_,_)=>
        {
            try
            {
                var notice=RiftClock.Poll(DateTimeOffset.UtcNow,Region,preferences.RiftAlertsEnabled,preferences.RiftLeadMinutes,delivered);
                if(notice is null)return;
                preferences.RiftDelivered=delivered.TakeLast(32).ToList();save();ShowNotice(notice.Opened,notice.Minutes);
            }
            catch(Exception ex){controller.Log("Spacerift bildirimi: "+ex.Message);}
        };
        if(start)timer.Start();
    }
    public void ShowNotice(bool opened,int minutes)
    {
        toast?.Close();toast=new(RiftText.Get(preferences.Language,opened?"opened":"left",minutes),preferences.Language);
        toast.Show();if(preferences.RiftSoundEnabled)try{chime.Play();}catch(Exception ex){controller.Log("Spacerift sesi: "+ex.Message);}
    }
    public void Dispose(){if(disposed)return;disposed=true;timer.Dispose();toast?.Dispose();chime.Dispose();}
}
