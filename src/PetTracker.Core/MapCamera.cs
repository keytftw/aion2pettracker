namespace PetTracker.Core;

public readonly record struct CameraPose(double X,double Y,double Zoom);
public sealed class MapCamera
{
    public const double MaximumZoom=24;
    public CameraPose Pose { get; private set; }=new(.5,.5,1);
    public CameraPose Target { get; private set; }=new(.5,.5,1);
    private CameraPose start=new(.5,.5,1);
    private double startTime,duration;
    public double ViewWidth { get; private set; }=1;
    public double ViewHeight { get; private set; }=1;
    public double ImageWidth { get; private set; }=1;
    public double ImageHeight { get; private set; }=1;
    public bool IsAnimating { get; private set; }
    public double FitScale=>Math.Min(ViewWidth/ImageWidth,ViewHeight/ImageHeight)*.94;
    public void Resize(double viewWidth,double viewHeight,double imageWidth,double imageHeight)
    {
        if(new[]{viewWidth,viewHeight,imageWidth,imageHeight}.Any(value=>!double.IsFinite(value)||value<=0))throw new ArgumentOutOfRangeException(nameof(viewWidth));
        ViewWidth=viewWidth;ViewHeight=viewHeight;ImageWidth=imageWidth;ImageHeight=imageHeight;
    }
    public MapPoint ToScreen(MapPoint point)=>new(ViewWidth/2+(point.X-Pose.X)*ImageWidth*FitScale*Pose.Zoom,ViewHeight/2+(point.Y-Pose.Y)*ImageHeight*FitScale*Pose.Zoom);
    public MapPoint FromScreen(MapPoint point)=>new(Pose.X+(point.X-ViewWidth/2)/(ImageWidth*FitScale*Pose.Zoom),Pose.Y+(point.Y-ViewHeight/2)/(ImageHeight*FitScale*Pose.Zoom));
    public void SetTarget(CameraPose target,double now,double seconds=.55)
    {
        if(new[]{target.X,target.Y,target.Zoom,now,seconds}.Any(value=>!double.IsFinite(value))||seconds<0)throw new ArgumentOutOfRangeException(nameof(target));
        Tick(now);start=Pose;Target=new(Math.Clamp(target.X,0,1),Math.Clamp(target.Y,0,1),Math.Clamp(target.Zoom,1,MaximumZoom));startTime=now;duration=seconds;
        IsAnimating=seconds>0&&Pose!=Target;if(!IsAnimating)Pose=Target;
    }
    public void Tick(double now)
    {
        if(!IsAnimating)return;var t=Math.Clamp((now-startTime)/duration,0,1);var eased=t<.5?4*t*t*t:1-Math.Pow(-2*t+2,3)/2;
        Pose=new(start.X+(Target.X-start.X)*eased,start.Y+(Target.Y-start.Y)*eased,Math.Exp(Math.Log(start.Zoom)+(Math.Log(Target.Zoom)-Math.Log(start.Zoom))*eased));
        if(t>=1){Pose=Target;IsAnimating=false;}
    }
    public void ZoomAt(MapPoint screenPoint,double zoom,double now)
    {
        Tick(now);var world=FromScreen(screenPoint);zoom=Math.Clamp(zoom,1,MaximumZoom);
        SetTarget(new(world.X-(screenPoint.X-ViewWidth/2)/(ImageWidth*FitScale*zoom),world.Y-(screenPoint.Y-ViewHeight/2)/(ImageHeight*FitScale*zoom),zoom),now,.22);
    }
    public void Pan(double xPixels,double yPixels,double now)
    {Tick(now);SetTarget(new(Pose.X-xPixels/(ImageWidth*FitScale*Pose.Zoom),Pose.Y-yPixels/(ImageHeight*FitScale*Pose.Zoom),Pose.Zoom),now,0);}
    public bool Focus(IEnumerable<MapPoint> source,double now)
    {
        var points=source.ToArray();if(points.Length==0)return false;
        if(points.Any(point=>!double.IsFinite(point.X)||!double.IsFinite(point.Y)||point.X<0||point.X>1||point.Y<0||point.Y>1))throw new ArgumentOutOfRangeException(nameof(source));
        var minX=points.Min(point=>point.X);var maxX=points.Max(point=>point.X);var minY=points.Min(point=>point.Y);var maxY=points.Max(point=>point.Y);
        var zoom=Math.Min(ViewWidth*.76/(Math.Max(.07,maxX-minX)*ImageWidth*FitScale),ViewHeight*.76/(Math.Max(.07,maxY-minY)*ImageHeight*FitScale));
        SetTarget(new((minX+maxX)/2,(minY+maxY)/2,Math.Clamp(zoom,1,12)),now,.7);return true;
    }
}
