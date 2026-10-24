using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Hd2Challenges.Services;
using Xunit;

namespace Hd2Challenges.EndToEnd.Tests;

[CollectionDefinition(Name)]
public sealed class WarpathWebAppCollection : ICollectionFixture<WarpathWebAppFixture>
{
    public const string Name = "Warpath web app";
}

public sealed class WarpathWebAppFixture : IAsyncLifetime
{
    private readonly StringBuilder _processOutput = new();
    private Process? _process;
    private readonly HttpClient _httpClient = new();

    public string BaseUrl { get; private set; } = string.Empty;
    public CatalogService Catalog { get; private set; } = null!;
    public RedemptionCatalogService RedemptionCatalog { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        int port = GetAvailablePort();
        BaseUrl = $"http://127.0.0.1:{port}";
        string repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(repositoryRoot, "challenges", "hd2-challenges.csproj");
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(typeof(WarpathWebAppFixture).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
            .Cast<System.Reflection.AssemblyConfigurationAttribute>().Single().Configuration);
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(BaseUrl);

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the Blazor app.");
        _process.OutputDataReceived += (_, args) => AppendOutput(args.Data);
        _process.ErrorDataReceived += (_, args) => AppendOutput(args.Data);
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _httpClient.BaseAddress = new Uri(BaseUrl);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(90));
        while (!timeout.IsCancellationRequested)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException($"The Blazor app exited before becoming ready.{Environment.NewLine}{_processOutput}");
            }

            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync(BaseUrl, timeout.Token);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    break;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (OperationCanceledException)
            {
            }

            await Task.Delay(250);
        }

        if (timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"The Blazor app did not become ready.{Environment.NewLine}{_processOutput}");
        }

        Catalog = new(_httpClient);
        RedemptionCatalog = new(_httpClient, Catalog);
        try
        {
            await RedemptionCatalog.EnsureLoadedAsync();
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit();
        }

        _process?.Dispose();
        _process = null;
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }

    private void AppendOutput(string? line)
    {
        if (line is not null)
        {
            lock (_processOutput)
            {
                _processOutput.AppendLine(line);
            }
        }
    }

    private static int GetAvailablePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "hd2-challenges.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
