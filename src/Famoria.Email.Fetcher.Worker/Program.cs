using Famoria.Application;
using Famoria.Email.Fetcher.Worker;
using Famoria.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder
    .AddServiceDefaults()
    .AddInfrastructure()
    .AddApplication()
    .AddEmailFetcherServices();

builder.Services.AddHostedService<EmailFetcherWorker>();

var host = builder.Build();

host.Run();
