using Famoria.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

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
        // For demo, fetch emails since 7 days ago. Adjust as needed.
        var since = DateTime.UtcNow.AddDays(-7);
        var emails = await _emailFetcher.GetNewEmailsAsync(request.UserEmail, request.AccessToken, since, cancellationToken);
        int successCount = 0;
        foreach (var eml in emails)
        {
            try
            {
                await _emailPersistenceService.PersistAsync(eml, request.FamilyId, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist email for family {FamilyId}", request.FamilyId);
            }
        }
        return successCount;
    }
}
