using Famoria.Application.Interfaces;
using Famoria.Application.Models;
using Famoria.Application.Models.Summarizer;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums;
using Famoria.Infrastructure.Persistence;

using Microsoft.Azure.Cosmos;

using System.Text.Json;

using Container = Microsoft.Azure.Cosmos.Container;
using PriorityLevel = Famoria.Domain.Enums.PriorityLevel;

// TODO: Implement prompt-level caching for successful AI responses during retries or development mode.
// Use a hash (e.g., SHA256) of the prompt JSON as the cache key.
// Store results in memory or file to avoid redundant LLM calls for repeated inputs.

namespace Famoria.Summarizer.Worker;

public class SummarizerWorker : BackgroundService
{
    private const int AuditTtl = 604800; // 7 days (7 × 24 × 60 × 60 = 604,800 seconds)
    private const int MaxAiReties = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SummarizerWorker> _logger;

    private CosmosClient _cosmosClient = default!;
    private ContainerResolver _containerResolver = default!;
    private ICosmosRepository<FamilyItem> _itemRepository = default!;
    private ICosmosRepository<FamilyItemAudit> _auditRepository = default!;
    private ICosmosRepository<Family> _familyRepository = default!;
    private IFamoriaAiClient _aiClient = default!;
    private IEmailService _emailService = default!;

    public SummarizerWorker(IServiceScopeFactory scopeFactory, ILogger<SummarizerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        _cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
        _containerResolver = scope.ServiceProvider.GetRequiredService<ContainerResolver>();
        _itemRepository = scope.ServiceProvider.GetRequiredService<ICosmosRepository<FamilyItem>>();
        _auditRepository = scope.ServiceProvider.GetRequiredService<ICosmosRepository<FamilyItemAudit>>();
        _familyRepository = scope.ServiceProvider.GetRequiredService<ICosmosRepository<Family>>();
        _aiClient = scope.ServiceProvider.GetRequiredService<IFamoriaAiClient>();
        _emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        Container leaseContainer = _cosmosClient.GetContainer(
            RepositoryHelper.DatabaseId, _containerResolver.ResolveLease(typeof(FamilyItem)));

        var processor = _itemRepository.Container
                .GetChangeFeedProcessorBuilder<FamilyItem>(
                    processorName: "famoria-summarizer",
                    onChangesDelegate: HandleChangesAsync)
                .WithInstanceName(Environment.MachineName)
                .WithLeaseContainer(leaseContainer)
                .Build();

        await processor.StartAsync();
    }

    // this delegate will get batches of new/updated items
    private async Task HandleChangesAsync(IReadOnlyCollection<FamilyItem> changes, CancellationToken ct)
    {
        foreach (var item in changes)
            await ProcessItemAsync(item, ct);
    }

    private async Task ProcessItemAsync(FamilyItem item, CancellationToken ct)
    {
        // Skip if already processed or in error
        if (item.Status == FamilyItemStatus.Processed || item.AiErrorReason == AiErrorReason.FailedPermanent)
        {
            _logger.LogInformation("Skipping already processed item {ItemId}", item.Id);
            return;
        }

        _logger.LogInformation("Processing FamilyItem {ItemId}", item.Id);

        // 1. Extracting family and item info and creating the prompt
        ProcessingPrompt processingPrompt = await CreateProcessingPrompt(item, ct);

        // 2. Sending the prompt to a LLM and handling its response
        SummaryResult summaryResult;
        try
        {
            summaryResult = await HandleAiResponse(item, processingPrompt, ct);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "AI error on processing item {ItemId}", item.Id);
            return;
        }

        // 3. Persist enriched item
        item.Summary = summaryResult;
        item.Status = FamilyItemStatus.Processed;
        await _itemRepository.UpdateAsync(item, ct);

        // 4. Write an audit record (7-day TTL)
        await CreateAuditRecordAsync(item, ct);

        _logger.LogInformation("Processed {ItemId}", item.Id);
    }

    private async Task<SummaryResult> HandleAiResponse(FamilyItem item, ProcessingPrompt processingPrompt, CancellationToken ct)
    {
        AiResponse aiResponse;
        try
        {
            aiResponse = await _aiClient.GenerateSummaryAsync(processingPrompt, ct);
        }
        catch (TimeoutException)
        {
            await MarkErrorAsync(item, FamilyItemStatus.Error, AiErrorReason.PromptTimeout, ct);
            throw;
        }
        catch (JsonException)
        {
            await MarkErrorAsync(item, FamilyItemStatus.Error, AiErrorReason.AiInvalidJson, ct);
            throw;
        }
        catch (Exception)
        {
            await MarkErrorAsync(item, FamilyItemStatus.Error, AiErrorReason.Unknown, ct);
            throw;
        }

        var priorityParsed = Enum.TryParse<PriorityLevel>(aiResponse.Priority, out var priority);
        var detectionStatusParsed = Enum.TryParse<DetectionStatusType>(aiResponse.DetectionStatus, out var detectionStatus);

        if (!priorityParsed)
        {
            await MarkErrorAsync(item, FamilyItemStatus.Error, AiErrorReason.AiInvalidJson, ct);
            throw new Exception($"LLM response invalid priority level {aiResponse.Priority}. Item {item.Id}");
        }

        if (!detectionStatusParsed)
        {
            await MarkErrorAsync(item, FamilyItemStatus.Error, AiErrorReason.AiInvalidJson, ct);
            throw new Exception($"LLM response invalid detection status {aiResponse.DetectionStatus}. Item {item.Id}");
        }

        SummaryResult summaryResult = new(
            aiResponse.Summary,
            aiResponse.ActionItems,
            aiResponse.Keywords,
            priority,
            aiResponse.Label,
            aiResponse.MatchedMembers,
            detectionStatus);

        return summaryResult;
    }

    private async Task<ProcessingPrompt> CreateProcessingPrompt(FamilyItem item, CancellationToken ct)
    {
        // 1. Download the .eml blob and extract text
        var rawContent = await _emailService.DownloadBlobAsync(((EmailPayload)item.Payload).EmlBlobPath, ct);
        var fullText = await _emailService.ExtractTextAsync(rawContent, ct);

        // 2. Context Gathering
        var family = await _familyRepository.GetByAsync(item.FamilyId, item.FamilyId, ct);
        var memberTagsByName = family!.Members
            .Where(m => m.Role == FamilyMemberRole.Child)
            .ToDictionary(
                m => m.Name,
                m => m.Tags ?? []);
        var allMemberIdentifiers = memberTagsByName
            .Select(kvp => new { Child = kvp.Key, Tags = string.Join(", ", kvp.Value) })
            .Select(x => $"{{\"{x.Child}: {x.Tags}\"}}")
            .ToList();

        // 3. Build & serialize prompt
        var processingPrompt = new ProcessingPrompt(
            ItemId: item.Id,
            SourceType: item.Source.ToString(),
            ReceivedAt: item.Payload.ReceivedAt,
            Language: family.Language,
            ContentText: fullText,
            MemberTagsByName: memberTagsByName,
            Metadata: new Dictionary<string, object>
            {
                ["Subject"] = ((EmailPayload)item.Payload).Subject,
                ["SenderName"] = ((EmailPayload)item.Payload).SenderName,
                ["SenderEmail"] = ((EmailPayload)item.Payload).SenderEmail,
                ["EmailLabels"] = ((EmailPayload)item.Payload).Labels ?? [],
                ["AllMemberIdentifiers"] = allMemberIdentifiers
            }
        );
        return processingPrompt;
    }

    private async Task CreateAuditRecordAsync(FamilyItem item, CancellationToken ct)
    {
        var prompt = await _aiClient.ExtractPromptsAsync();
        var rawResponse = await _aiClient.ExtractRawResponseAsync();

        FamilyItemAudit audit = new()
        {
            Id = Guid.NewGuid().ToString(),
            FamilyId = item.FamilyId,
            FamilyItemId = item.Id,
            Timestamp = DateTimeOffset.UtcNow,
            Prompt = prompt ?? "no-prompt-found",
            Response = rawResponse ?? "no-response-found",
            Ttl = AuditTtl
        };

        await _auditRepository.AddAsync(audit, ct);
    }

    // Helper to centralize error marking and retry count
    private async Task MarkErrorAsync(
        FamilyItem item,
        FamilyItemStatus status,
        AiErrorReason reason,
        CancellationToken ct)
    {
        item.AiRetryCount++;

        if (item.AiRetryCount > MaxAiReties)
        {
            item.AiErrorReason = AiErrorReason.FailedPermanent;
            _logger.LogWarning("Item {ItemId} moved to FailedPermanent after {Retries} retries", item.Id, item.AiRetryCount);
        }
        else
        {
            item.Status = status;
        }

        item.Status = status;
        item.AiErrorReason = reason;
        await _itemRepository.UpdateAsync(item, ct);
    }
}
