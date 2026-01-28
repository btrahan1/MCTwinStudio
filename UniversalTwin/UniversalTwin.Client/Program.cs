using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UniversalTwin.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<UniversalTwin.Client.Services.ISchemaService, UniversalTwin.Client.Services.SchemaService>();
builder.Services.AddScoped<UniversalTwin.Client.Services.IDataService, UniversalTwin.Client.Services.DataService>();
builder.Services.AddScoped<UniversalTwin.Client.Services.StateService>();
builder.Services.AddScoped<MCTwin.Shared.Services.PolygonService>();

await builder.Build().RunAsync();
