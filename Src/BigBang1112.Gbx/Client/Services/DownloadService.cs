using Microsoft.JSInterop;

namespace BigBang1112.Gbx.Client.Services;

public interface IDownloadService
{
    Task DownloadAsync(string fileName, object content, string mimeType);
}

public class DownloadService : IDownloadService
{
    private readonly IJSRuntime _js;

    public DownloadService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task DownloadAsync(string fileName, object content, string mimeType)
    {
        await _js.InvokeVoidAsync("download_file", fileName, content, mimeType);
    }
}
