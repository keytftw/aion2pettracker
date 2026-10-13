using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using PetTracker.App;
using PetTracker.Core;
using Xunit.Abstractions;

namespace PetTracker.UiTests;

public sealed class OverlayRenderTests(ITestOutputHelper output)
{
    private static void OnSta(Action action)
    {
        Exception? failure=null;
        var thread=new Thread(()=>{try{action();}catch(Exception ex){failure=ex;}});
        thread.SetApartmentState(ApartmentState.STA);thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(30)),"Offline UI test timed out");
        if(failure is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [Fact]
    public void SevenVisibleCardsRenderWithLargeTrackedCollection()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2TrackerUi-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                new OverlayPreferences{VisibleCards=7}.Save(Path.Combine(folder,"overlay-preferences.json"));
                using var form=new OverlayForm(false,folder,preview:true);_=form.Handle;
                foreach(var id in form.Controller.Catalog.Pets.Keys.Take(150))form.Controller.Tracker.Pin(id,true);
                typeof(OverlayForm).GetMethod("SyncCards",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                form.ExportPreview(Path.Combine(folder,"warmup.png"));
                using var image=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);
                for(var i=0;i<15;i++)form.DrawToBitmap(image,form.ClientRectangle);
                var samples=new List<double>();long allocations=GC.GetAllocatedBytesForCurrentThread();
                for(var i=0;i<90;i++){var clock=Stopwatch.StartNew();form.DrawToBitmap(image,form.ClientRectangle);samples.Add(clock.Elapsed.TotalMilliseconds);}
                allocations=GC.GetAllocatedBytesForCurrentThread()-allocations;samples.Sort();
                output.WriteLine($"Seven-card paint, 150 tracked pets: median={samples[45]:F2} ms; p95={samples[85]:F2} ms; allocations/frame={allocations/90:N0} bytes; DPI={form.DeviceDpi}");
                Assert.True(form.Controller.Tracker.VisibleIds.Count()>=150);Assert.False(form.Controller.Running);
            }
            finally{Directory.Delete(folder,true);}
        });
    }

    [Fact]
    public void CompactSizeSurvivesMoveCompletionAndNewPetGrowth()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2TrackerUi-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                using var form=new OverlayForm(false,folder,preview:true);_=form.Handle;
                typeof(OverlayForm).GetMethod("ToggleCompact",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                var compact=form.ClientSize;SendMessage(form.Handle,0x232,IntPtr.Zero,IntPtr.Zero);
                Assert.Equal(compact,form.ClientSize);
                foreach(var id in form.Controller.Catalog.Pets.Keys.Take(15))form.Controller.Tracker.Pin(id,true);
                typeof(OverlayForm).GetMethod("SyncCards",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                Assert.Equal(compact,form.ClientSize);
                typeof(OverlayForm).GetMethod("ToggleCompact",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(form,null);
                Assert.True(form.ClientSize.Height>compact.Height);Assert.False(form.Controller.Running);
            }
            finally{Directory.Delete(folder,true);}
        });
    }
    [DllImport("user32.dll")]private static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);

    [Fact]
    public void ScrolledPortraitsCannotOverwriteHeader()
    {
        OnSta(()=>
        {
            var folder=Path.Combine(Path.GetTempPath(),"A2TrackerUi-"+Guid.NewGuid());Directory.CreateDirectory(folder);
            try
            {
                using var form=new OverlayForm(false,folder,preview:true);_=form.Handle;form.ExportPreview(Path.Combine(folder,"warmup.png"));
                using var before=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);form.DrawToBitmap(before,form.ClientRectangle);
                typeof(OverlayForm).GetField("scroll",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(form,71f);
                using var after=new Bitmap(form.ClientSize.Width,form.ClientSize.Height);form.DrawToBitmap(after,form.ClientRectangle);
                var headerHeight=(int)(80*form.DeviceDpi/96f*4f/3f);
                for(var y=0;y<headerHeight;y++)for(var x=0;x<before.Width;x++)Assert.Equal(before.GetPixel(x,y),after.GetPixel(x,y));
            }
            finally{Directory.Delete(folder,true);}
        });
    }

    [Theory][InlineData(8)][InlineData(16)]
    public void MeasureNativeTimerCadence(int interval)
    {
        OnSta(()=>
        {
            using var loop=new ApplicationContext();using var timer=new System.Windows.Forms.Timer{Interval=interval};
            var samples=new List<double>();var clock=Stopwatch.StartNew();double previous=0;
            timer.Tick+=(_,_)=>{var now=clock.Elapsed.TotalMilliseconds;if(previous>0)samples.Add(now-previous);previous=now;if(samples.Count>=60){timer.Stop();loop.ExitThread();}};
            timer.Start();Application.Run(loop);samples.Sort();
            output.WriteLine($"Native timer interval {interval} ms: median tick={samples[30]:F2} ms; p95={samples[57]:F2} ms");
            Assert.Equal(60,samples.Count);
        });
    }
}
