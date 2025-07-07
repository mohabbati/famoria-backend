using Famoria.Application;
using Famoria.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .AddEmailFetcherInfra()
    .AddEmailFetcherApp()
    .AddApiServices();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCustomSwagger(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddBackgroundQueue();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseCustomSwagger(builder.Configuration);
}

app.UseHttpsRedirection();

// Use CORS before authentication
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();
