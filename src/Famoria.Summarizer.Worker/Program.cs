using Famoria.Application;
using Famoria.Application.Interfaces;
using Famoria.Application.Services;
using Famoria.Infrastructure;
using Famoria.Summarizer.Worker;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = Host.CreateApplicationBuilder(args);

builder
    .AddServiceDefaults()
    .AddSummarizerInfra()
    .AddSummarizerApp();

// Suppress SKEXP0010 diagnostic warning for AddAzureOpenAIChatClient
#pragma warning disable SKEXP0010
// (1) register chat‑completion service – **already in your code**
builder.Services
    .AddAzureOpenAIChatCompletion(
        deploymentName: builder.Configuration["Ai:DeploymentId"]!,
        endpoint:        builder.Configuration["Ai:Endpoint"]!,
        apiKey:          builder.Configuration["Ai:ApiKey"]!,
        serviceId: "azure");
builder.Services.AddOllamaChatCompletion(
        modelId: "llama3",
        endpoint: new Uri(builder.Configuration["Ai:Endpoint"]!),
        serviceId: "ollama");

builder.Services.AddSingleton<SummarizerPromptBuilder>();

builder.Services.AddScoped<IFamoriaAiClient>(sp =>
{
    #if DEBUG
    var chatService = sp.GetKeyedService<IChatCompletionService>("ollama");
    #else
    var chatService = sp.GetKeyedService<IChatCompletionService>("azure");
    #endif
    var promptBuilder = sp.GetRequiredService<SummarizerPromptBuilder>();
    return new FamoriaAiClient(chatService!, promptBuilder);
});
#pragma warning restore SKEXP0010

builder.Services.AddHostedService<SummarizerWorker>();

var host = builder.Build();
host.Run();

