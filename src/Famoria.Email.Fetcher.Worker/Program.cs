using System.Reflection;

using Famoria.Application;
using Famoria.Application.Features.ProcessLinkedAccounts;
using Famoria.Email.Fetcher.Worker;
using Famoria.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder
    .AddServiceDefaults()
    .AddInfrastructure()
    .AddApplication()
    .AddEmailFetcherServices();

// Register MediatR from the assembly containing ProcessLinkedAccountsHandler (Famoria.Application)
//builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(Program))!));

builder.Services.AddHostedService<EmailFetcherWorker>();

var host = builder.Build();

host.Run();
