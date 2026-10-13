using System.Text.Json;
namespace PetTracker.App;
public sealed class ServerNames
{
    private readonly Dictionary<string,Dictionary<string,string>> servers=[];
    public ServerNames(string path)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(path));
        foreach(var property in document.RootElement.GetProperty("servers").EnumerateObject())
            servers[property.Name]=property.Value.EnumerateObject().Where(p=>p.Value.ValueKind==JsonValueKind.String).ToDictionary(p=>p.Name,p=>p.Value.GetString()!);
    }
    public string Name(string id,string language)
    {if(!servers.TryGetValue(id,out var names))return id;return names.GetValueOrDefault(language,names.GetValueOrDefault("en",id));}
}
