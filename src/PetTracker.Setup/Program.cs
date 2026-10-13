namespace PetTracker.Setup;
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetDefaultFont(new System.Drawing.Font(PetTracker.App.UiText.FontFamily("en"),9f));
        using var instance = new Mutex(true, "Local\\Aion2PetTracker-Setup", out var first);
        if (!first) return;
        try { Application.Run(new SetupForm()); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Aion 2 Pet Tracker Setup", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { instance.ReleaseMutex(); }
    }
}
