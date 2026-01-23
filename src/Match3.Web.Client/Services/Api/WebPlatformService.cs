using Match3.Editor.Interfaces;
using Microsoft.JSInterop;

namespace Match3.Web.Client.Services.Api;

public class WebPlatformService : IPlatformService
{
    private readonly IJSRuntime _js;

    public WebPlatformService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task ShowAlertAsync(string message)
    {
        await _js.InvokeVoidAsync("alert", message);
    }

    public async Task ShowAlertAsync(string title, string message)
    {
        await _js.InvokeVoidAsync("alert", $"{title}\n\n{message}");
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        return await _js.InvokeAsync<bool>("confirm", $"{title}\n\n{message}");
    }

    public async Task<string> PromptAsync(string title, string defaultValue = "")
    {
        var result = await _js.InvokeAsync<string?>("prompt", title, defaultValue);
        return result ?? "";
    }

    public async Task CopyToClipboardAsync(string text)
    {
        await _js.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }
}
