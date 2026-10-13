namespace PetTracker.Core;

public static class InstallationLocation
{
    public static string Normalize(string path)
    {
        if(string.IsNullOrWhiteSpace(path)||!Path.IsPathFullyQualified(path))throw new ArgumentException("Choose a full installation folder path.");
        var full=Path.TrimEndingDirectorySeparator(Path.GetFullPath(path.Trim()));
        if(string.Equals(full,Path.TrimEndingDirectorySeparator(Path.GetPathRoot(full)!),StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Choose an application folder, not the drive root.");
        if(Path.GetExtension(full).Equals(".exe",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Choose a folder, not an executable.");
        return full;
    }
}
