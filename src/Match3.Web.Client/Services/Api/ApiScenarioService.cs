using System.Net.Http.Json;
using System.Text.Json;
using Match3.Editor.Interfaces;
using Match3.Editor.ViewModels;

namespace Match3.Web.Client.Services.Api;

public class ApiScenarioService : IScenarioService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiScenarioService(HttpClient http)
    {
        _http = http;
    }

    public ScenarioFolderNode BuildTree()
    {
        var response = _http.GetAsync("api/scenarios/tree").Result;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadFromJsonAsync<ScenarioFolderNode>(_jsonOptions).Result
            ?? new ScenarioFolderNode("/", "", new List<ScenarioFolderNode>(), new List<ScenarioFileEntry>());
    }

    public string ReadScenarioJson(string relativePath)
    {
        var response = _http.GetAsync($"api/scenarios/read?path={Uri.EscapeDataString(relativePath)}").Result;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().Result;
    }

    public void WriteScenarioJson(string relativePath, string json)
    {
        var response = _http.PostAsJsonAsync($"api/scenarios/write?path={Uri.EscapeDataString(relativePath)}", json).Result;
        response.EnsureSuccessStatusCode();
    }

    public string CreateNewScenario(string folderRelativePath, string scenarioName, string json)
    {
        var response = _http.PostAsJsonAsync(
            $"api/scenarios/create?folder={Uri.EscapeDataString(folderRelativePath)}&name={Uri.EscapeDataString(scenarioName)}",
            json).Result;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().Result;
    }

    public string CreateFolder(string parentFolderRelativePath, string folderName)
    {
        var response = _http.PostAsync(
            $"api/scenarios/create-folder?parent={Uri.EscapeDataString(parentFolderRelativePath)}&name={Uri.EscapeDataString(folderName)}",
            null).Result;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().Result;
    }

    public string DuplicateScenario(string sourceRelativePath, string newScenarioName)
    {
        var response = _http.PostAsync(
            $"api/scenarios/duplicate?source={Uri.EscapeDataString(sourceRelativePath)}&newName={Uri.EscapeDataString(newScenarioName)}",
            null).Result;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().Result;
    }

    public void DeleteScenario(string relativePath)
    {
        var response = _http.DeleteAsync($"api/scenarios/delete?path={Uri.EscapeDataString(relativePath)}").Result;
        response.EnsureSuccessStatusCode();
    }

    public void DeleteFolder(string relativePath)
    {
        var response = _http.DeleteAsync($"api/scenarios/delete-folder?path={Uri.EscapeDataString(relativePath)}").Result;
        response.EnsureSuccessStatusCode();
    }

    public void RenameScenario(string relativePath, string newName)
    {
        var response = _http.PostAsync(
            $"api/scenarios/rename?path={Uri.EscapeDataString(relativePath)}&newName={Uri.EscapeDataString(newName)}",
            null).Result;
        response.EnsureSuccessStatusCode();
    }

    public void RenameFolder(string relativePath, string newName)
    {
        var response = _http.PostAsync(
            $"api/scenarios/rename-folder?path={Uri.EscapeDataString(relativePath)}&newName={Uri.EscapeDataString(newName)}",
            null).Result;
        response.EnsureSuccessStatusCode();
    }
}
