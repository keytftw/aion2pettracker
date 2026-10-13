using System.Media;

namespace PetTracker.App;
// Original, low-volume bell synthesized in memory. No external audio or disk recording.
public sealed class MaxChime : IDisposable
{
    private readonly MemoryStream stream;
    private readonly SoundPlayer player;
    public MaxChime()
    {
        const int rate=44100,count=19845;stream=new();using(var writer=new BinaryWriter(stream,System.Text.Encoding.ASCII,true))
        {
            writer.Write("RIFF"u8);writer.Write(36+count*2);writer.Write("WAVEfmt "u8);writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write("data"u8);writer.Write(count*2);
            for(var i=0;i<count;i++){var t=i/(double)rate;var envelope=Math.Min(1,t/.012)*Math.Exp(-t*10)*Math.Min(1,(.45-t)/.03);var sample=(Math.Sin(2*Math.PI*1318.51*t)+.32*Math.Sin(2*Math.PI*1977.77*t))*envelope*.075;writer.Write((short)(sample*short.MaxValue));}
        }
        stream.Position=0;player=new(stream);player.Load();
    }
    public void Play()=>player.Play();
    public void Dispose(){player.Stop();player.Dispose();stream.Dispose();}
}
