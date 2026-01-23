using System.Text.Json;
using Match3.Editor.Interfaces;

namespace Match3.Web.Client.Services.Api;

public class ClientJsonService : IJsonService
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _options);
    }

    public T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, _options)!;
    }
}
