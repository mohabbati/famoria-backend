using Famoria.Application;
using Famoria.Email.Fetcher.Worker;
using Famoria.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder
    .AddServiceDefaults()
    .AddEmailFetcherInfra()
    .AddEmailFetcherApp();

builder.Services.AddHostedService<EmailFetcherWorker>();

var host = builder.Build();

host.Run();
