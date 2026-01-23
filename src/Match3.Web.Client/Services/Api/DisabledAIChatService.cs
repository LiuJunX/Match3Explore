using Match3.Editor.Interfaces;
using Match3.Editor.Models;

namespace Match3.Web.Client.Services.Api;

/// <summary>
/// Placeholder AI service - AI features disabled in WebAssembly for now.
/// </summary>
public class DisabledAIChatService : ILevelAIChatService
{
    public bool IsAvailable => false;

    public Task<AIChatResponse> SendMessageAsync(
        string message,
        LevelContext context,
        IReadOnlyList<ChatMessage> history,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AIChatResponse
        {
            Success = false,
            Message = "AI 功能在 WebAssembly 模式下暂不可用。\nAI features are not available in WebAssembly mode."
        });
    }

    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        string message,
        LevelContext context,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        yield return "AI 功能在 WebAssembly 模式下暂不可用。";
        await Task.CompletedTask;
    }
}
