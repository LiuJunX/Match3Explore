using System.Net.Http.Json;
using System.Text.Json;
using Match3.Core.Analysis;
using Match3.Editor.Interfaces;
using Match3.Editor.ViewModels;

namespace Match3.Web.Client.Services.Api;

public class ApiLevelService : ILevelService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiLevelService(HttpClient http)
    {
        _http = http;
    }

    public ScenarioFolderNode BuildTree()
    {
        var response = _http.GetAsync("api/levels/tree").Result;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadFromJsonAsync<ScenarioFolderNode>(_jsonOptions).Result
            ?? new ScenarioFolderNode("/", "", new List<ScenarioFolderNode>(), new List<ScenarioFileEntry>());
    }

    public string ReadLevelJson(string relativePath)
    {
        var response = _http.GetAsync($"api/levels/read?path={Uri.EscapeDataString(relativePath)}").Result;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().Result;
    }

    public void WriteLevelJson(string relativePath, string json)
    {
        var response = _http.PostAsJsonAsync($"api/levels/write?path={Uri.EscapeDataString(relativePath)}", json).Result;
        response.EnsureSuccessStatusCode();
    }

    public string CreateNewLevel(string folderRelativePath, string levelName, string json)
    {
        var response = _http.PostAsJsonAsync(
            $"api/levels/create?folder={Uri.EscapeDataString(folderRelativePath)}&name={Uri.EscapeDataString(levelName)}",
            json).Result;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().Result;
    }

    public string CreateFolder(string parentFolderRelativePath, string folderName)
    {
        var response = _http.PostAsync(
            $"api/levels/create-folder?parent={Uri.EscapeDataString(parentFolderRelativePath)}&name={Uri.EscapeDataString(folderName)}",
            null).Result;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().Result;
    }

    public string DuplicateLevel(string sourceRelativePath, string newLevelName)
    {
        var response = _http.PostAsync(
            $"api/levels/duplicate?source={Uri.EscapeDataString(sourceRelativePath)}&newName={Uri.EscapeDataString(newLevelName)}",
            null).Result;
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().Result;
    }

    public void DeleteLevel(string relativePath)
    {
        var response = _http.DeleteAsync($"api/levels/delete?path={Uri.EscapeDataString(relativePath)}").Result;
        response.EnsureSuccessStatusCode();
    }

    public void DeleteFolder(string relativePath)
    {
        var response = _http.DeleteAsync($"api/levels/delete-folder?path={Uri.EscapeDataString(relativePath)}").Result;
        response.EnsureSuccessStatusCode();
    }

    public void RenameLevel(string relativePath, string newName)
    {
        var response = _http.PostAsync(
            $"api/levels/rename?path={Uri.EscapeDataString(relativePath)}&newName={Uri.EscapeDataString(newName)}",
            null).Result;
        response.EnsureSuccessStatusCode();
    }

    public void RenameFolder(string relativePath, string newName)
    {
        var response = _http.PostAsync(
            $"api/levels/rename-folder?path={Uri.EscapeDataString(relativePath)}&newName={Uri.EscapeDataString(newName)}",
            null).Result;
        response.EnsureSuccessStatusCode();
    }

    public LevelAnalysisSnapshot? ReadAnalysisSnapshot(string levelRelativePath)
    {
        var response = _http.GetAsync($"api/levels/analysis?path={Uri.EscapeDataString(levelRelativePath)}").Result;
        if (!response.IsSuccessStatusCode) return null;
        return response.Content.ReadFromJsonAsync<LevelAnalysisSnapshot?>(_jsonOptions).Result;
    }

    public void WriteAnalysisSnapshot(string levelRelativePath, LevelAnalysisSnapshot snapshot)
    {
        var response = _http.PostAsJsonAsync(
            $"api/levels/analysis?path={Uri.EscapeDataString(levelRelativePath)}",
            snapshot, _jsonOptions).Result;
        response.EnsureSuccessStatusCode();
    }

    public string GetAnalysisFilePath(string levelRelativePath)
    {
        // Client doesn't need actual file path
        return levelRelativePath.Replace(".json", ".analysis.json");
    }
}
