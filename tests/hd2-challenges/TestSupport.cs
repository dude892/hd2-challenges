using System.Net;
using Hd2Challenges.Services;

namespace Hd2Challenges.Tests;

internal sealed class CatalogTestContext : IDisposable
{
    private readonly HttpClient _httpClient;

    private CatalogTestContext(HttpClient httpClient, CatalogService catalog, RedemptionCatalogService redemptionCatalog)
    {
        _httpClient = httpClient;
        Catalog = catalog;
        RedemptionCatalog = redemptionCatalog;
    }

    public CatalogService Catalog { get; }
    public RedemptionCatalogService RedemptionCatalog { get; }

    public static async Task<CatalogTestContext> CreateAsync(bool loadCatalog = true)
    {
        HttpClient httpClient = new(new CatalogDataHandler())
        {
            BaseAddress = new Uri("https://catalog.test/")
        };
        CatalogService catalog = new(httpClient);
        RedemptionCatalogService redemptionCatalog = new(httpClient, catalog);
        if (loadCatalog)
        {
            await redemptionCatalog.EnsureLoadedAsync();
        }
        return new CatalogTestContext(httpClient, catalog, redemptionCatalog);
    }

    public void Dispose() => _httpClient.Dispose();

    private sealed class CatalogDataHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string relativePath = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath.TrimStart('/'));
            string path = Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            string content = File.ReadAllText(path);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}

internal sealed class RedemptionSaveTestContext : IDisposable
{
    private readonly CatalogTestContext _catalogContext;

    private RedemptionSaveTestContext(
        CatalogTestContext catalogContext,
        SaveLibraryService genericSaveLibrary,
        RedemptionSaveLibraryService saveLibrary)
    {
        _catalogContext = catalogContext;
        Catalog = catalogContext;
        GenericSaveLibrary = genericSaveLibrary;
        SaveLibrary = saveLibrary;
    }

    public CatalogTestContext Catalog { get; }
    public SaveLibraryService GenericSaveLibrary { get; }
    public RedemptionSaveLibraryService SaveLibrary { get; }

    public static async Task<RedemptionSaveTestContext> CreateAsync()
    {
        CatalogTestContext catalogContext = await CatalogTestContext.CreateAsync();
        SaveLibraryService genericSaveLibrary = new(new BrowserStorageService(new InMemoryJsRuntime()));
        RedemptionSaveLibraryService saveLibrary = new(
            genericSaveLibrary, catalogContext.RedemptionCatalog, catalogContext.Catalog);
        return new RedemptionSaveTestContext(catalogContext, genericSaveLibrary, saveLibrary);
    }

    public void Dispose() => _catalogContext.Dispose();
}

internal sealed class InMemoryJsRuntime : Microsoft.JSInterop.IJSRuntime
{
    private readonly Dictionary<string, string> _storage = new(StringComparer.Ordinal);

    public List<(string FileName, string ContentType, string Content)> Downloads { get; } = [];

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        args ??= [];
        switch (identifier)
        {
            case "hd2App.getLocalStorage":
                return ValueTask.FromResult(_storage.TryGetValue((string)args[0]!, out string? value)
                    ? (TValue)(object)value
                    : default!);
            case "hd2App.setLocalStorage":
                _storage[(string)args[0]!] = (string)args[1]!;
                break;
            case "hd2App.removeLocalStorage":
                _storage.Remove((string)args[0]!);
                break;
            case "hd2App.downloadFile":
                Downloads.Add(((string)args[0]!, (string)args[1]!, (string)args[2]!));
                break;
            default:
                throw new InvalidOperationException($"Unexpected JavaScript interop call '{identifier}'.");
        }

        return ValueTask.FromResult(default(TValue)!);
    }
}