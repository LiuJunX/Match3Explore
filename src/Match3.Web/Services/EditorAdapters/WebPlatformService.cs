using Match3.Editor.Interfaces;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Match3.Web.Services.EditorAdapters
{
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
            await _js.InvokeVoidAsync("alert", $"{title}\n{message}");
        }

        public async Task<bool> ConfirmAsync(string title, string message)
        {
            // Browser confirm only takes message
            return await _js.InvokeAsync<bool>("confirm", message);
        }

        public async Task<string> PromptAsync(string title, string defaultValue = "")
        {
            return await _js.InvokeAsync<string>("prompt", title, defaultValue);
        }

        public async Task CopyToClipboardAsync(string text)
        {
            await _js.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
    }
}
