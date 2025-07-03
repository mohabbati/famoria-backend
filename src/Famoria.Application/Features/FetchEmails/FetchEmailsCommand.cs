namespace Famoria.Application.Features.FetchEmails;

public record FetchEmailsCommand(IntegrationProvider Provider, string FamilyId, string UserEmail, string AccessToken, DateTime Since, bool ForceUpdateLastFetched = false) : IRequest<int>;
