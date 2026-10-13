using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace PetTracker.App;

internal static class FontAssets
{
    // Font buffers and registrations live for the process lifetime: native controls
    // and GDI+ Font objects may continue using them until the last window closes.
    private static readonly PrivateFontCollection Collection = new();
    private static readonly List<IntPtr> Buffers = [];
    private static readonly List<IntPtr> Registrations = [];
    private static readonly Lazy<bool> Loaded = new(Load);
    private static bool Load()
    {
        var assembly = typeof(FontAssets).Assembly;
        foreach (var name in new[]{"InstrumentSans-Regular.ttf","InstrumentSans-Bold.ttf","BebasNeue-Regular.ttf"})
        {
            using var stream = assembly.GetManifestResourceStream("Fonts."+name)
                ?? throw new InvalidDataException("Missing bundled font: "+name);
            using var copy = new MemoryStream(); stream.CopyTo(copy); var bytes = copy.ToArray();
            var memory = Marshal.AllocCoTaskMem(bytes.Length); Buffers.Add(memory);
            Marshal.Copy(bytes,0,memory,bytes.Length); Collection.AddMemoryFont(memory,bytes.Length);
            uint count=0;var registration=AddFontMemResourceEx(memory,(uint)bytes.Length,IntPtr.Zero,ref count);
            if(registration==IntPtr.Zero||count==0)throw new InvalidDataException("Could not register font: "+name);
            Registrations.Add(registration);
        }
        return true;
    }
    internal static string BodyName(string language)
    {
        _=Loaded.Value;
        return language switch {"zh"=>"Microsoft YaHei UI","hi"=>"Nirmala UI","ar" or "ru"=>"Segoe UI",_=>"Instrument Sans"};
    }
    internal static Font Create(string language,float size,FontStyle style, bool heading=false)
    {
        var name=heading&&language is not ("ar" or "hi" or "zh" or "ru")?"Bebas Neue":BodyName(language);
        _=Loaded.Value;
        var family=Collection.Families.FirstOrDefault(f=>f.Name==name);
        return family is null?new Font(name,size,style,GraphicsUnit.Pixel):new Font(family,size,family.IsStyleAvailable(style)?style:FontStyle.Regular,GraphicsUnit.Pixel);
    }
    [DllImport("gdi32.dll")] private static extern IntPtr AddFontMemResourceEx(IntPtr data,uint length,IntPtr reserved,ref uint count);
}
