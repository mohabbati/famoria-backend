using Famoria.Email.Fetcher.Worker;
using Famoria.Application;
using Famoria.Application.Models;
using Famoria.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder
    .AddServiceDefaults()
    .AddInfrastructure(builder.Configuration.Get<AppSettings>()!)
    .AddEmailFetcherServices();

builder.Services.AddHostedService<EmailFetcherWorker>();

var host = builder.Build();

host.Run();
