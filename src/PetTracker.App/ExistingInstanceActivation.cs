using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PetTracker.App;

// A second launch only signals the running instance. Its UI thread owns restoration.
internal sealed class ExistingInstanceActivation : IDisposable
{
    private readonly EventWaitHandle request;
    private readonly System.Windows.Forms.Timer timer=new(){Interval=150};
    private static string Name(bool demo)=>"Local\\Aion2PetTracker-Activate-"+(demo?"Demo":"Live");

    public ExistingInstanceActivation(bool demo,Action restore)
    {
        request=new(false,EventResetMode.AutoReset,Name(demo));
        timer.Tick+=(_,_)=>{if(request.WaitOne(0))restore();};timer.Start();
    }

    public static void Notify(bool demo)
    {
        using var current=Process.GetCurrentProcess();
        foreach(var candidate in Process.GetProcessesByName(current.ProcessName))
        {
            using(candidate)
            {
                try{if(candidate.Id!=current.Id&&candidate.SessionId==current.SessionId)AllowSetForegroundWindow(candidate.Id);}
                catch(InvalidOperationException){} // The previous instance may be exiting.
                catch(System.ComponentModel.Win32Exception){} // Another session may deny process inspection.
            }
        }
        // The mutex can be acquired just before the first process creates its event.
        for(var attempt=0;attempt<20;attempt++)
        {
            try{using var signal=EventWaitHandle.OpenExisting(Name(demo));signal.Set();return;}
            catch(WaitHandleCannotBeOpenedException){Thread.Sleep(100);}
        }
    }

    public void Dispose(){timer.Stop();timer.Dispose();request.Dispose();}
    [DllImport("user32.dll")]private static extern bool AllowSetForegroundWindow(int processId);
}
