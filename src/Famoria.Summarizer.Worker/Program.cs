using Famoria.Summarizer.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<SummarizerWorker>();

var host = builder.Build();
host.Run();
