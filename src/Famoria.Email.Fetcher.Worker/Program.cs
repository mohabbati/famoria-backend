using Famoria.Application;
using Famoria.Email.Fetcher.Worker;
using Famoria.Infrastructure;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

builder
    .AddServiceDefaults()
    .AddInfrastructure()
    .AddEmailFetcherServices();

builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(EmailFetcherWorker))!));

builder.Services.AddHostedService<EmailFetcherWorker>();

var host = builder.Build();

host.Run();
