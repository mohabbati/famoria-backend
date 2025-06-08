using Famoria.Api.Extensions;
using Famoria.Application;
using Famoria.Infrastructure;
using System.Reflection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .AddInfrastructure()
    .AddApiServices();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Famoria API", Version = "v1" });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:19759")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddAuth(builder.Configuration);
builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(Program))!));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Famoria API V1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

// Use CORS before authentication
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
