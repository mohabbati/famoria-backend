using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.Extensions.DependencyInjection;
using Famoria.TestClient;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

var apiBase = new Uri("https://localhost:5001/");
builder.Services.AddTransient<CredentialsHandler>();
builder.Services.AddScoped(sp => new HttpClient(sp.GetRequiredService<CredentialsHandler>())
{
    BaseAddress = apiBase
});

await builder.Build().RunAsync();
