using Cyber_Cord.App;
using Cyber_Cord.App.Options;
using Cyber_Cord.App.Services;
using Cyber_Cord.App.Shared;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = builder.HostEnvironment.BaseAddress;

builder.Services.Configure<RouteOptions>(builder.Configuration);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });
builder.Services.AddSingleton<WebSocketService>();
builder.Services.AddSingleton<SessionState>();
builder.Services.AddSingleton<UserSettingsService>();

builder.Services.AddTransient<CookieDelegatingHandler>();
builder.Services.AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName, client => client.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler<CookieDelegatingHandler>();

builder.Services.AddScoped<ApiService>();

builder.Services.AddRadzenComponents();

builder.Services.AddScoped<BorrowService>();
builder.Services.AddScoped<SpinnerService>();
builder.Services.AddScoped<ErrorProviderService>();
builder.Services.AddScoped<SoundNotificationService>();
builder.Services.AddScoped<CallHandlerService>();

builder.Services.AddBlazorContextMenu();

await builder.Build().RunAsync();
