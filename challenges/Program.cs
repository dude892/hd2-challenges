using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Hd2Challenges;
using Hd2Challenges.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<RedemptionCatalogService>();
builder.Services.AddScoped<BrowserStorageService>();
builder.Services.AddScoped<SaveLibraryService>();
builder.Services.AddScoped<RedemptionSaveLibraryService>();
builder.Services.AddScoped<FileExportService>();
builder.Services.AddScoped<RedemptionRewardService>();
builder.Services.AddScoped<RedemptionMissionService>();

await builder.Build().RunAsync();
