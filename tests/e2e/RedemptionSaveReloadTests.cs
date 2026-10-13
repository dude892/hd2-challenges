using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
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

    public string BaseUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
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
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(BaseUrl);

        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the Blazor app.");
        _process.OutputDataReceived += (_, args) => AppendOutput(args.Data);
        _process.ErrorDataReceived += (_, args) => AppendOutput(args.Data);
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        using HttpClient httpClient = new();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(90));
        while (!timeout.IsCancellationRequested)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException($"The Blazor app exited before becoming ready.{Environment.NewLine}{_processOutput}");
            }

            try
            {
                using HttpResponseMessage response = await httpClient.GetAsync(BaseUrl, timeout.Token);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
            {
            }

            await Task.Delay(250, timeout.Token);
        }

        throw new TimeoutException($"The Blazor app did not become ready.{Environment.NewLine}{_processOutput}");
    }

    public Task DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit();
        }

        _process?.Dispose();
        return Task.CompletedTask;
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

[Collection(WarpathWebAppCollection.Name)]
public sealed class RedemptionSaveReloadTests(WarpathWebAppFixture webApp) : PageTest
{
    [Fact]
    public async Task AdvancingMissionPersistsAndReloadsTheActiveRun()
    {
        await Page.GotoAsync($"{webApp.BaseUrl}/warpath-redemption");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Begin Run" }).WaitForAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Begin Run" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Mission Complete" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "3 stars" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Continue to Rewards" }).ClickAsync();
        await Page.Locator(".app-modal .card-body button").First.ClickAsync();

        await Expect(Page.GetByText("Mission 2 of 2")).ToBeVisibleAsync();
        await Page.ReloadAsync();
        await Expect(Page.GetByText("Mission 2 of 2")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Slots" }).ClickAsync();
        await Expect(Page.Locator(".app-modal.save-slot-modal")).ToContainTextAsync("Normal Warpath |");
    }
}
