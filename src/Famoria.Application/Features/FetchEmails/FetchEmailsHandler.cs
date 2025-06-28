using Famoria.Application.Interfaces; // Added for IEmailFetcher, IEmailPersistenceService
using Famoria.Application.Models; // Added for FetchedEmailData
using MediatR; // Added for IRequestHandler
using Microsoft.Extensions.Logging;
using System; // Added for Exception
using System.Threading; // Added for CancellationToken
using System.Threading.Tasks; // Added for Task

namespace Famoria.Application.Features.FetchEmails;

public class FetchEmailsHandler : IRequestHandler<FetchEmailsCommand, int>
{
    private readonly IEmailFetcher _emailFetcher;
    private readonly IEmailPersistenceService _emailPersistenceService;
    private readonly ILogger<FetchEmailsHandler> _logger;

    public FetchEmailsHandler(IEmailFetcher emailFetcher, IEmailPersistenceService emailPersistenceService, ILogger<FetchEmailsHandler> logger)
    {
        _emailFetcher = emailFetcher;
        _emailPersistenceService = emailPersistenceService;
        _logger = logger;
    }

    public async Task<int> Handle(FetchEmailsCommand request, CancellationToken cancellationToken)
    {
        var fetchedEmails = await _emailFetcher.GetNewEmailsAsync(request.UserEmail, request.AccessToken, request.Since, cancellationToken);
        int successCount = 0;
        foreach (var emailData in fetchedEmails)
        {
            try
            {
                await _emailPersistenceService.PersistAsync(
                    emailData.EmlContent,
                    request.FamilyId,
                    emailData.ProviderMessageId,
                    emailData.ProviderConversationId,
                    emailData.ProviderSyncToken,
                    emailData.Labels,
                    cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist email for family {FamilyId}, ProviderMessageId {ProviderMessageId}", request.FamilyId, emailData.ProviderMessageId ?? "N/A");
            }
        }
        return successCount;
    }
}
