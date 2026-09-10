using System.Text.Json;
using Microsoft.JSInterop;

namespace Hd2Challenges.Services;

public sealed class BrowserStorageService(IJSRuntime jsRuntime)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await jsRuntime.InvokeAsync<string?>("hd2App.getLocalStorage", key);
        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return jsRuntime.InvokeVoidAsync("hd2App.setLocalStorage", key, json).AsTask();
    }

    public Task RemoveAsync(string key) =>
        jsRuntime.InvokeVoidAsync("hd2App.removeLocalStorage", key).AsTask();
}
