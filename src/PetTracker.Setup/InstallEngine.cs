using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using PetTracker.Core;

namespace PetTracker.Setup;
internal static class InstallEngine
{
    internal static string InstalledRoot => InstallationProfile.Load(InstallationProfile.ReceiptPath)?.InstallDirectory
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "A2PetTracker");
    internal static void ValidateDestination(string root)
    {
        root=InstallationLocation.Normalize(root);
        if(File.Exists(root))throw new IOException("The selected path is a file.");
        if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any()&&!File.Exists(Path.Combine(root,"Update.exe")))
            throw new IOException("Choose an empty application folder.");
        var previous=InstalledRoot;
        if(File.Exists(Path.Combine(previous,"Update.exe"))&&!string.Equals(root,previous,StringComparison.OrdinalIgnoreCase))
            throw new IOException("The tracker is already installed in "+previous+". Uninstall it before moving to another folder.");
    }
    internal static async Task InstallAsync(string root,bool desktopShortcut,IProgress<double>? progress=null)
    {
        ValidateDestination(root);root=InstallationLocation.Normalize(root);
        Directory.CreateDirectory(root);
        var probe=Path.Combine(root,".setup-write-"+Guid.NewGuid().ToString("N"));
        using(var writable=new FileStream(probe,FileMode.CreateNew,FileAccess.Write,FileShare.None,1,FileOptions.DeleteOnClose)){writable.WriteByte(0);}
        var assembly=Assembly.GetExecutingAssembly();
        using var payload=assembly.GetManifestResourceStream("InstallPayload") ?? throw new InvalidDataException("Setup payload is missing. Rebuild using Build-Setup.ps1.");
        using var metadata=assembly.GetManifestResourceStream("InstallPayloadHash") ?? throw new InvalidDataException("Setup integrity metadata is missing.");
        using var reader=new StreamReader(metadata);var expected=(await reader.ReadToEndAsync()).Trim();
        var folder=Path.Combine(PetTracker.Core.InstallationProfile.DataFolder,"SetupCache",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);var executable=Path.Combine(folder,"Install.exe");
        try
        {
            await using(var destination=new FileStream(executable,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                var buffer=new byte[131072];long copied=0;int length;
                while((length=await payload.ReadAsync(buffer))>0)
                {await destination.WriteAsync(buffer.AsMemory(0,length));copied+=length;progress?.Report(.3*copied/payload.Length);}
            }
            await using(var stream=File.OpenRead(executable))
                if(!string.Equals(Convert.ToHexString(await SHA256.HashDataAsync(stream)),expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Setup integrity check failed.");
            progress?.Report(.4);
            // Only the embedded, hash-verified Velopack installer is run. No downloads, game access, or Npcap installation.
            var start=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true};start.ArgumentList.Add("--silent");
            start.ArgumentList.Add("--installto");start.ArgumentList.Add(root);
            using var process=Process.Start(start)??throw new IOException("Could not start the installer.");
            await process.WaitForExitAsync();if(process.ExitCode!=0)throw new IOException("Installer exit code: "+process.ExitCode);
            progress?.Report(.85);
            var installedExe=Path.Combine(root,"current","A2PetTracker.exe");
            if(!File.Exists(installedExe)||!File.Exists(Path.Combine(root,"Update.exe")))throw new IOException("Installed application was not found.");
            var version=FileVersionInfo.GetVersionInfo(installedExe).ProductVersion;
            var expectedVersion=assembly.GetName().Version!.ToString(3);
            if(version is null||!(version==expectedVersion||version.StartsWith(expectedVersion+"+",StringComparison.Ordinal)))throw new IOException("Installed application version does not match this Setup.");
            ConfigureDesktopShortcut(root,desktopShortcut);progress?.Report(.95);
        }
        finally
        {
            try { File.Delete(executable);Directory.Delete(folder); } catch(IOException) { } catch(UnauthorizedAccessException) { }
        }
    }
    private static void ConfigureDesktopShortcut(string root,bool enabled)
    {
        var path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"A2PetTracker.lnk");
        if(!enabled&&!File.Exists(path))return;
        object? shell=null,link=null;
        try
        {
            shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")??throw new IOException("Windows shortcut support is unavailable."));
            dynamic automation=shell!;link=automation.CreateShortcut(path);dynamic shortcut=link;
            var stub=Path.Combine(root,"A2PetTracker.exe");var updater=Path.Combine(root,"Update.exe");
            if(File.Exists(path)&&!string.Equals((string)shortcut.TargetPath,stub,StringComparison.OrdinalIgnoreCase)&&!string.Equals((string)shortcut.TargetPath,updater,StringComparison.OrdinalIgnoreCase))
                throw new IOException("An unrelated desktop shortcut already uses the name A2PetTracker.");
            if(!enabled){if(File.Exists(path))File.Delete(path);return;}
            // The stable updater remains at the same path when the current version is replaced.
            shortcut.TargetPath=updater;shortcut.Arguments="start A2PetTracker.exe";shortcut.WorkingDirectory=root;
            shortcut.Description="Aion 2 Pet Tracker";shortcut.IconLocation=stub+",0";shortcut.Save();
        }
        finally
        {
            if(link is not null&&Marshal.IsComObject(link))Marshal.FinalReleaseComObject(link);
            if(shell is not null&&Marshal.IsComObject(shell))Marshal.FinalReleaseComObject(shell);
        }
    }
    internal static void Launch()
    {
        var start=new ProcessStartInfo(Path.Combine(InstalledRoot,"Update.exe")){UseShellExecute=false,CreateNoWindow=true};
        start.ArgumentList.Add("start");start.ArgumentList.Add("A2PetTracker.exe");
        using var process=Process.Start(start)??throw new IOException("Could not launch the installed tracker.");
    }
}
