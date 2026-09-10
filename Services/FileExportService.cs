using System.Text.Json;
using Microsoft.JSInterop;

namespace Hd2Challenges.Services;

public sealed class FileExportService(IJSRuntime jsRuntime)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public Task DownloadTextAsync(string fileName, string content, string contentType = "text/plain;charset=utf-8") =>
        jsRuntime.InvokeVoidAsync("hd2App.downloadFile", fileName, contentType, content).AsTask();

    public Task DownloadJsonAsync<T>(string fileName, T content) =>
        DownloadTextAsync(fileName, JsonSerializer.Serialize(content, JsonOptions), "application/json;charset=utf-8");
}
