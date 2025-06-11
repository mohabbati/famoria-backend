using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

var apiBase = new Uri("https://localhost:5001/");
builder.Services.AddScoped(sp => new HttpClient(new WebAssemblyHttpHandler
{
    DefaultBrowserRequestCredentials = BrowserRequestCredentials.Include
}) { BaseAddress = apiBase });

await builder.Build().RunAsync();
