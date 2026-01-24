using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MCTwin.Web;
using MCTwinStudio.Services;
using MCTwinStudio.Core.Interfaces;
using MCTwinStudio.Web.Renderers;
using MCTwinStudio.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// MCTwin Core Services
builder.Services.AddScoped<IAssetService, WebAssetService>();
builder.Services.AddScoped<IMCTwinRenderer, WebRenderer>();


await builder.Build().RunAsync();
